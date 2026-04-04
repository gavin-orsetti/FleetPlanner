using System.Text.Json;
using System.Text.Json.Serialization;

using FleetPlanner.Models;

namespace FleetPlanner.Services;

// ──────────────────────────────────────────────────────────────────────────────
// API choice: UEX Corp API 2.0 (https://uexcorp.space/api/2.0/)
//
// Rationale:
// - UEX Corp is purpose-built for Star Citizen economy data (ships, vehicles, pricing).
// - Provides both USD pledge pricing and in-game aUEC pricing.
// - Free tier with no API key required for read-only ship data endpoints.
// - Returns comprehensive ship stats: name, manufacturer, role, size, crew, cargo, prices.
// - The Star Citizen API (starcitizen-api.com) was considered but its GameData
//   feature is deprecated ("This feature is no longer supported"), and its ship
//   endpoint requires an API key with undocumented rate limits.
// - UEX Corp is the de-facto standard used by the Star Citizen community tools.
//
// Endpoints used:
// - GET /vehicles       — list of all ships/vehicles with stats
// - GET /vehicles/{id}  — single vehicle detail
// ──────────────────────────────────────────────────────────────────────────────

/// <summary>
/// The "live" implementation of <see cref="IShipDataService"/> that fetches ship data
/// directly from the UEX Corp REST API over HTTP.
/// <para>
/// This service is NOT registered directly in DI — instead, <see cref="CachedShipDataService"/>
/// wraps it (Decorator Pattern) and is what the rest of the app consumes. The live service
/// is only called when the cache is empty or the user explicitly refreshes.
/// </para>
/// <para>
/// <b>IHttpClientFactory pattern:</b> Instead of creating <c>HttpClient</c> instances directly
/// (which can cause socket exhaustion), we use <c>IHttpClientFactory</c> — a .NET best practice
/// that manages <c>HttpClient</c> lifetimes and connection pooling for us.
/// </para>
/// </summary>
/// <see href="https://uexcorp.space/api/2.0/"/>
/// <see href="https://learn.microsoft.com/en-us/dotnet/fundamentals/networking/http/httpclient-guidelines"/>
public class ShipDataService : IShipDataService
{
    /// <summary>Base URL for the UEX Corp API v2.0.</summary>
    private const string BaseUrl = "https://uexcorp.space/api/2.0";

    /// <summary>
    /// Factory for creating named <c>HttpClient</c> instances. Registered in
    /// <c>MauiProgram.cs</c> via <c>builder.Services.AddHttpClient()</c>.
    /// </summary>
    private readonly IHttpClientFactory _httpClientFactory;

    /// <summary>
    /// Constructor — receives <c>IHttpClientFactory</c> from the DI container.
    /// </summary>
    /// <param name="httpClientFactory">
    /// The factory that creates pre-configured <c>HttpClient</c> instances.
    /// We request a named client called "ShipData" — this name can be used in
    /// <c>MauiProgram.cs</c> to configure base addresses, default headers, etc.
    /// </param>
    public ShipDataService(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    /// <summary>
    /// Fetches all ships from the UEX Corp <c>/vehicles</c> endpoint.
    /// <para>
    /// <b>Flow:</b> Create HttpClient → GET /vehicles → deserialise JSON → map each API
    /// vehicle DTO to our <see cref="Ship"/> domain model → return the list.
    /// </para>
    /// </summary>
    /// <param name="forceRefresh">Ignored by the live service (always fetches fresh). The
    /// caching layer uses this parameter to decide whether to bypass its cache.</param>
    /// <returns>A list of all ships, or an empty list if the API returns no data.</returns>
    /// <exception cref="HttpRequestException">Thrown if the API call fails (non-2xx status).</exception>
    public async Task<List<Ship>> GetAllShipsAsync(bool forceRefresh = false)
    {
        // CreateClient("ShipData") returns a pooled HttpClient instance. The name "ShipData"
        // allows platform-specific configuration (e.g., custom User-Agent) if needed.
        var client = _httpClientFactory.CreateClient("ShipData");
        var response = await client.GetAsync($"{BaseUrl}/vehicles");

        // EnsureSuccessStatusCode throws HttpRequestException for 4xx/5xx responses,
        // which the CachedShipDataService catches to fall back to offline data.
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync();

        // Deserialise the UEX Corp JSON envelope: { "data": [ {...}, {...}, ... ] }
        var apiResponse = JsonSerializer.Deserialize<UexApiResponse>(json, JsonOptions);

        if (apiResponse?.Data is null)
            return [];

        // Map each API DTO to our domain model, stamping the fetch time.
        var now = DateTime.UtcNow;
        return apiResponse.Data.Select(v => MapToShip(v, now)).ToList();
    }

    /// <summary>
    /// Not supported on the live service — individual ship lookups are served from the
    /// local SQLite cache by <see cref="CachedShipDataService"/>.
    /// </summary>
    /// <exception cref="NotSupportedException">Always thrown.</exception>
    public Task<Ship?> GetShipAsync(int id)
    {
        // Single-ship lookup is handled by CachedShipDataService from local cache.
        // This method is not typically called directly on the live service.
        throw new NotSupportedException("Use CachedShipDataService for individual ship lookups.");
    }

    /// <summary>
    /// The live service always returns "now" because every call fetches fresh data.
    /// The meaningful timestamp tracking happens in <see cref="CachedShipDataService"/>.
    /// </summary>
    public Task<DateTime?> GetLastUpdatedAsync()
    {
        // The live service always fetches fresh data; caching layer tracks timestamps.
        return Task.FromResult<DateTime?>(DateTime.UtcNow);
    }

    /// <summary>
    /// Maps a UEX Corp API vehicle DTO to our <see cref="Ship"/> domain model.
    /// <para>
    /// Field mappings:
    /// <list type="bullet">
    ///   <item><c>v.Scu</c> → <see cref="Ship.CargoCapacity"/> (SCU = Standard Cargo Units)</item>
    ///   <item><c>v.PledgePrice</c> → <see cref="Ship.PriceUsd"/> (RSI store price in USD)</item>
    ///   <item><c>v.GamePrice</c> → <see cref="Ship.PriceAuec"/> (in-game aUEC price)</item>
    /// </list>
    /// </para>
    /// </summary>
    private static Ship MapToShip(UexVehicle v, DateTime fetchedAt)
    {
        // Derive a human-readable primary role from the boolean flags.
        // Priority order: more specific roles first.
        var role = v.IsMining == 1 ? "Mining"
                 : v.IsSalvage == 1 ? "Industrial"
                 : v.IsMedical == 1 ? "Medical"
                 : v.IsBomber == 1 ? "Combat"
                 : v.IsMilitary == 1 ? "Combat"
                 : v.IsExploration == 1 ? "Exploration"
                 : v.IsCargo == 1 ? "Cargo"
                 : v.IsRacing == 1 ? "Racing"
                 : v.IsPassenger == 1 ? "Passenger"
                 : "Multipurpose";

        // crew is a single string from the API (e.g. "1", "2")
        int.TryParse(v.Crew, out int crew);

        return new Ship
        {
            Id = v.Id,
            Name = v.Name ?? string.Empty,
            Manufacturer = v.CompanyName ?? string.Empty,
            Role = role,
            Description = string.Empty,   // not in /vehicles endpoint
            Size = v.PadType ?? "S",
            CrewMin = crew,
            CrewMax = crew,
            CargoCapacity = v.Scu,
            PriceUsd = 0,              // not in /vehicles endpoint
            PriceAuec = 0,              // not in /vehicles endpoint
            ImageUrl = v.UrlPhoto ?? string.Empty,
            LastUpdated = fetchedAt
        };
    }

    /// <summary>
    /// Shared JSON deserialisation options. <c>PropertyNameCaseInsensitive = true</c>
    /// allows the deserialiser to match JSON property names regardless of casing,
    /// providing resilience if the API changes casing conventions.
    /// </summary>
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    // ── UEX Corp API response DTOs ────────────────────────────────────
    // These are internal to the service — no other code needs to know the API's JSON shape.
    // We use [JsonPropertyName] to map snake_case JSON keys to PascalCase C# properties.
    // See: https://learn.microsoft.com/en-us/dotnet/standard/serialization/system-text-json/

    /// <summary>
    /// Top-level JSON envelope returned by the UEX Corp API.
    /// Shape: <c>{ "data": [ ... ] }</c>
    /// </summary>
    private sealed class UexApiResponse
    {
        [JsonPropertyName("data")]
        public List<UexVehicle>? Data { get; set; }
    }

    /// <summary>
    /// A single vehicle record from the UEX Corp API's <c>/vehicles</c> endpoint.
    /// Property names use <c>[JsonPropertyName]</c> to map from snake_case JSON keys.
    /// </summary>
    private sealed class UexVehicle
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("company_name")]
        public string? CompanyName { get; set; }

        [JsonPropertyName("pad_type")]
        public string? PadType { get; set; }  // "XS","S","M","L","XL"

        [JsonPropertyName("crew")]
        public string? Crew { get; set; }     // single value e.g. "1" or "2"

        [JsonPropertyName("scu")]
        public int Scu { get; set; }

        [JsonPropertyName("url_photo")]
        public string? UrlPhoto { get; set; }

        // Role boolean flags (API returns 0/1 integers)
        [JsonPropertyName("is_cargo")] public int IsCargo { get; set; }
        [JsonPropertyName("is_mining")] public int IsMining { get; set; }
        [JsonPropertyName("is_military")] public int IsMilitary { get; set; }
        [JsonPropertyName("is_exploration")] public int IsExploration { get; set; }
        [JsonPropertyName("is_medical")] public int IsMedical { get; set; }
        [JsonPropertyName("is_salvage")] public int IsSalvage { get; set; }
        [JsonPropertyName("is_refinery")] public int IsRefinery { get; set; }
        [JsonPropertyName("is_repair")] public int IsRepair { get; set; }
        [JsonPropertyName("is_refuel")] public int IsRefuel { get; set; }
        [JsonPropertyName("is_passenger")] public int IsPassenger { get; set; }
        [JsonPropertyName("is_bomber")] public int IsBomber { get; set; }
        [JsonPropertyName("is_stealth")] public int IsStealth { get; set; }
        [JsonPropertyName("is_racing")] public int IsRacing { get; set; }
        [JsonPropertyName("is_scanning")] public int IsScanning { get; set; }
        [JsonPropertyName("is_interdiction")] public int IsInterdiction { get; set; }
        [JsonPropertyName("is_concept")] public int IsConcept { get; set; }
        [JsonPropertyName("is_starter")] public int IsStarter { get; set; }
    }
}
