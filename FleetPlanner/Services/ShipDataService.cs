using System.Text.Json;
using System.Text.Json.Serialization;

using FleetPlanner.Models;

namespace FleetPlanner.Services;

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

public class ShipDataService : IShipDataService
{
    private const string BaseUrl = "https://uexcorp.space/api/2.0";

    private readonly IHttpClientFactory _httpClientFactory;

    public ShipDataService(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    public async Task<List<Ship>> GetAllShipsAsync(bool forceRefresh = false)
    {
        var client = _httpClientFactory.CreateClient("ShipData");
        var response = await client.GetAsync($"{BaseUrl}/vehicles");
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync();
        var apiResponse = JsonSerializer.Deserialize<UexApiResponse>(json, JsonOptions);

        if (apiResponse?.Data is null)
            return [];

        var now = DateTime.UtcNow;
        return apiResponse.Data.Select(v => MapToShip(v, now)).ToList();
    }

    public Task<Ship?> GetShipAsync(int id)
    {
        // Single-ship lookup is handled by CachedShipDataService from local cache.
        // This method is not typically called directly on the live service.
        throw new NotSupportedException("Use CachedShipDataService for individual ship lookups.");
    }

    public Task<DateTime?> GetLastUpdatedAsync()
    {
        // The live service always fetches fresh data; caching layer tracks timestamps.
        return Task.FromResult<DateTime?>(DateTime.UtcNow);
    }

    private static Ship MapToShip(UexVehicle v, DateTime fetchedAt) => new()
    {
        Id = v.Id,
        Name = v.Name ?? string.Empty,
        Manufacturer = v.Manufacturer ?? string.Empty,
        Role = v.Role ?? string.Empty,
        Description = v.Description ?? string.Empty,
        Size = v.Size,
        CrewMin = v.CrewMin,
        CrewMax = v.CrewMax,
        CargoCapacity = v.Scu,
        PriceUsd = v.PledgePrice,
        PriceAuec = v.GamePrice,
        ImageUrl = v.ImageUrl ?? string.Empty,
        LastUpdated = fetchedAt
    };

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    // UEX Corp API response models (internal to the service)
    private sealed class UexApiResponse
    {
        [JsonPropertyName("data")]
        public List<UexVehicle>? Data { get; set; }
    }

    private sealed class UexVehicle
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("manufacturer")]
        public string? Manufacturer { get; set; }

        [JsonPropertyName("role")]
        public string? Role { get; set; }

        [JsonPropertyName("description")]
        public string? Description { get; set; }

        [JsonPropertyName("size")]
        public int Size { get; set; }

        [JsonPropertyName("crew_min")]
        public int CrewMin { get; set; }

        [JsonPropertyName("crew_max")]
        public int CrewMax { get; set; }

        [JsonPropertyName("scu")]
        public int Scu { get; set; }

        [JsonPropertyName("pledge_price")]
        public decimal PledgePrice { get; set; }

        [JsonPropertyName("game_price")]
        public long GamePrice { get; set; }

        [JsonPropertyName("image_url")]
        public string? ImageUrl { get; set; }
    }
}
