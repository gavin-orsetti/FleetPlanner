using System.Text.Json;
using System.Text.Json.Serialization;

using FleetPlanner.Models;

namespace FleetPlanner.Services;

// ──────────────────────────────────────────────────────────────────────────────
// API choice: starcitizen.tools Semantic MediaWiki API
//
// Rationale:
// - starcitizen.tools is the community-maintained wiki for Star Citizen, powered by
//   Semantic MediaWiki (SMW). Unlike the previous UEX Corp integration, the wiki's
//   SMW "action=ask" endpoint returns ship descriptions, USD pledge prices, in-game
//   aUEC average prices, and all other ship stats in a SINGLE API call.
// - The UEX Corp /vehicles endpoint lacked pledge pricing, aUEC pricing, and ship
//   descriptions — three fields that are critical for the recommendation engine's
//   value analysis and upgrade-path passes.
// - No API key is required; the wiki only asks for a descriptive User-Agent header.
// - The SMW query language is powerful: [[Category:Ships]] selects all ship pages,
//   and |?Property syntax requests specific semantic properties as structured JSON.
//
// Endpoint used:
// - GET /api.php?action=ask&query=[[Category:Ships]]|?...|limit=500&format=json
//
// How the SMW "action=ask" query works:
// - [[Category:Ships]]    — selects all pages in the Ships category
// - |?Pledge price        — requests the "Pledge price" semantic property
// - |?Average price       — requests the "Average price" (aUEC) property
// - |?Description         — requests the ship description
// - |?Career              — requests the career/role classification
// - |limit=500            — returns up to 500 results per call
// - &format=json          — returns results as JSON instead of HTML
//
// The response shape is:
//   { "query": { "results": { "Ship Name": { "printouts": { ... } } } } }
// Each ship is keyed by its wiki page title (which is the ship name).
// ──────────────────────────────────────────────────────────────────────────────

/// <summary>
/// The "live" implementation of <see cref="IShipDataService"/> that fetches ship data
/// directly from the starcitizen.tools Semantic MediaWiki API over HTTP.
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
/// <see href="https://starcitizen.tools"/>
/// <see href="https://www.semantic-mediawiki.org/wiki/Help:API:ask"/>
/// <see href="https://learn.microsoft.com/en-us/dotnet/fundamentals/networking/http/httpclient-guidelines"/>
public class ShipDataService : IShipDataService
{
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
    /// We request a named client called "ShipData" — this name is used in
    /// <c>MauiProgram.cs</c> to configure the User-Agent header and timeout.
    /// </param>
    public ShipDataService(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    /// <summary>
    /// Fetches all ships from the starcitizen.tools wiki using a single SMW
    /// <c>action=ask</c> query that returns descriptions, prices, and all stats.
    /// <para>
    /// <b>Flow:</b> Create HttpClient → GET the SMW ask query → deserialise JSON →
    /// map each wiki result entry to our <see cref="Ship"/> domain model → return the list
    /// sorted alphabetically by name.
    /// </para>
    /// </summary>
    /// <param name="forceRefresh">Ignored by the live service (always fetches fresh). The
    /// caching layer uses this parameter to decide whether to bypass its cache.</param>
    /// <returns>A list of all ships, or an empty list if the API returns no data.</returns>
    /// <exception cref="HttpRequestException">Thrown if the API call fails (non-2xx status).</exception>
    public async Task<List<Ship>> GetAllShipsAsync(bool forceRefresh = false)
    {
        var client = _httpClientFactory.CreateClient("ShipData");

        var url = "https://starcitizen.tools/api.php?action=ask" +
                  "&query=[[Category:Ships]]" +
                  "|?Pledge%20price" +
                  "|?Average%20price" +
                  "|?Description" +
                  "|?Career" +
                  "|?Minimum%20crew" +
                  "|?Maximum%20crew" +
                  "|?Manufacturer" +
                  "|?Ship%20matrix%20size" +
                  "|?Cargo%20capacity" +
                  "|?Production%20state" +
                  "|limit=500" +
                  "&format=json";

        var response = await client.GetAsync(url);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync();
        var apiResponse = JsonSerializer.Deserialize<WikiApiResponse>(json, JsonOptions);

        if (apiResponse?.Query?.Results is null)
            return [];

        var now = DateTime.UtcNow;

        return apiResponse.Query.Results
            .Select((kvp, index) => MapToShip(kvp.Key, kvp.Value.Printouts, index + 1, now))
            .OrderBy(s => s.Name)
            .ToList();
    }

    /// <summary>
    /// Not supported on the live service — individual ship lookups are served from the
    /// local SQLite cache by <see cref="CachedShipDataService"/>.
    /// </summary>
    /// <exception cref="NotSupportedException">Always thrown.</exception>
    public Task<Ship?> GetShipAsync(int id)
    {
        throw new NotSupportedException("Use CachedShipDataService for individual ship lookups.");
    }

    /// <summary>
    /// The live service always returns "now" because every call fetches fresh data.
    /// The meaningful timestamp tracking happens in <see cref="CachedShipDataService"/>.
    /// </summary>
    public Task<DateTime?> GetLastUpdatedAsync()
    {
        return Task.FromResult<DateTime?>(DateTime.UtcNow);
    }

    /// <summary>
    /// Maps a wiki result entry to our <see cref="Ship"/> domain model.
    /// <para>
    /// The wiki has no numeric ship IDs — pages are identified by their title string.
    /// We generate sequential IDs (<paramref name="generatedId"/>) so the SQLite cache
    /// can use an integer primary key. These IDs are stable within a single fetch but
    /// may change across fetches if the wiki adds or removes ship pages.
    /// </para>
    /// <para>
    /// Each printout field uses the null-safe <c>FirstOrDefault()</c> pattern because SMW
    /// returns properties as JSON arrays (a property can have multiple values). We take the
    /// first value and fall back to a sensible default if the array is null or empty. This
    /// handles missing data gracefully — not every ship page has every property filled in.
    /// </para>
    /// <para>
    /// Field mappings:
    /// <list type="bullet">
    ///   <item><c>name</c> (dictionary key) → <see cref="Ship.Name"/></item>
    ///   <item><c>Pledge price[0].value</c> → <see cref="Ship.PriceUsd"/> (RSI store price in USD)</item>
    ///   <item><c>Average price[0].value</c> → <see cref="Ship.PriceAuec"/> (in-game aUEC price)</item>
    ///   <item><c>Description[0]</c> → <see cref="Ship.Description"/></item>
    ///   <item><c>Career[0]</c> → <see cref="Ship.Role"/> (e.g., "Combat", "Multi-role")</item>
    ///   <item><c>Minimum crew[0]</c> → <see cref="Ship.CrewMin"/></item>
    ///   <item><c>Maximum crew[0]</c> → <see cref="Ship.CrewMax"/></item>
    ///   <item><c>Manufacturer[0].fulltext</c> → <see cref="Ship.Manufacturer"/></item>
    ///   <item><c>Ship matrix size[0]</c> → <see cref="Ship.Size"/> (e.g., "Small", "Medium", "Large")</item>
    ///   <item><c>Cargo capacity[0].value</c> → <see cref="Ship.CargoCapacity"/> (SCU, truncated to int)</item>
    /// </list>
    /// </para>
    /// </summary>
    /// <param name="name">The ship name (wiki page title used as the dictionary key).</param>
    /// <param name="p">The printout properties from the wiki, or null if the entry has no printouts.</param>
    /// <param name="generatedId">A sequentially assigned ID (wiki has no numeric IDs).</param>
    /// <param name="fetchedAt">UTC timestamp of when this data was fetched.</param>
    /// <returns>A fully populated <see cref="Ship"/> domain model.</returns>
    private static Ship MapToShip(string name, WikiShipPrintouts? p, int generatedId, DateTime fetchedAt)
    {
        var pledgePrice  = p?.PledgePrice?.FirstOrDefault()?.Value ?? 0;
        var averagePrice = p?.AveragePrice?.FirstOrDefault()?.Value ?? 0;
        var description  = p?.Description?.FirstOrDefault() ?? string.Empty;
        var career       = p?.Career?.FirstOrDefault() ?? "Multipurpose";
        var crewMin      = p?.MinimumCrew?.FirstOrDefault() ?? 1;
        var crewMax      = p?.MaximumCrew?.FirstOrDefault() ?? 1;
        var manufacturer = p?.Manufacturer?.FirstOrDefault()?.Fulltext ?? string.Empty;
        var size         = p?.ShipMatrixSize?.FirstOrDefault() ?? "Small";
        var cargo        = (int)(p?.CargoCapacity?.FirstOrDefault()?.Value ?? 0);

        return new Ship
        {
            Id            = generatedId,
            Name          = name,
            Manufacturer  = manufacturer,
            Role          = career,
            Description   = description,
            Size          = size,
            CrewMin       = crewMin,
            CrewMax       = crewMax,
            CargoCapacity = cargo,
            PriceUsd      = pledgePrice,
            PriceAuec     = (long)averagePrice,
            ImageUrl      = string.Empty,
            LastUpdated   = fetchedAt
        };
    }

    /// <summary>
    /// Shared JSON deserialisation options. <c>PropertyNameCaseInsensitive = true</c>
    /// allows the deserialiser to match JSON property names regardless of casing,
    /// providing resilience if the wiki API changes casing conventions.
    /// </summary>
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    // ── starcitizen.tools SMW API response DTOs ──────────────────────────
    // These are internal to the service — no other code needs to know the API's JSON shape.
    // We use [JsonPropertyName] to map the wiki's JSON keys to PascalCase C# properties.
    // See: https://www.semantic-mediawiki.org/wiki/Help:API:ask

    /// <summary>
    /// Top-level JSON envelope returned by the SMW <c>action=ask</c> endpoint.
    /// Shape: <c>{ "query": { "results": { ... } } }</c>
    /// </summary>
    private sealed class WikiApiResponse
    {
        [JsonPropertyName("query")]
        public WikiQuery? Query { get; set; }
    }

    /// <summary>
    /// The "query" object containing the results dictionary.
    /// Each key is a wiki page title (ship name), and each value holds the printout data.
    /// </summary>
    private sealed class WikiQuery
    {
        [JsonPropertyName("results")]
        public Dictionary<string, WikiShipEntry>? Results { get; set; }
    }

    /// <summary>
    /// A single ship entry in the results dictionary. Contains the printouts
    /// (semantic properties) requested in the query string.
    /// </summary>
    private sealed class WikiShipEntry
    {
        [JsonPropertyName("printouts")]
        public WikiShipPrintouts? Printouts { get; set; }
    }

    /// <summary>
    /// The printout properties for a single ship. Each property is returned as a JSON array
    /// because SMW supports multi-valued properties. We take <c>FirstOrDefault()</c> for each.
    /// <para>
    /// <b>Why arrays?</b> Semantic MediaWiki properties can have multiple values (e.g., a ship
    /// could theoretically have multiple careers). In practice, ships have one value per property,
    /// but the API always wraps them in arrays for consistency.
    /// </para>
    /// </summary>
    private sealed class WikiShipPrintouts
    {
        /// <summary>USD pledge price from the RSI store. Returned as a money value with unit.</summary>
        [JsonPropertyName("Pledge price")]
        public List<WikiMoneyValue>? PledgePrice { get; set; }

        /// <summary>Average in-game aUEC price. Returned as a money value with unit.</summary>
        [JsonPropertyName("Average price")]
        public List<WikiMoneyValue>? AveragePrice { get; set; }

        /// <summary>Ship description text from the wiki page.</summary>
        [JsonPropertyName("Description")]
        public List<string>? Description { get; set; }

        /// <summary>
        /// The ship's career/role classification (e.g., "Combat", "Exploration", "Multi-role").
        /// Maps directly to <see cref="Ship.Role"/>.
        /// </summary>
        [JsonPropertyName("Career")]
        public List<string>? Career { get; set; }

        /// <summary>Minimum crew needed to operate the ship.</summary>
        [JsonPropertyName("Minimum crew")]
        public List<int>? MinimumCrew { get; set; }

        /// <summary>Maximum crew the ship can accommodate.</summary>
        [JsonPropertyName("Maximum crew")]
        public List<int>? MaximumCrew { get; set; }

        /// <summary>
        /// The ship's manufacturer. Returned as a page reference (SMW links to the manufacturer's
        /// wiki page), so we use <see cref="WikiPageRef.Fulltext"/> to get the display name.
        /// </summary>
        [JsonPropertyName("Manufacturer")]
        public List<WikiPageRef>? Manufacturer { get; set; }

        /// <summary>
        /// Size classification from the ship matrix (e.g., "Small", "Medium", "Large", "Capital").
        /// The wiki uses human-readable strings, not the UEX-style pad codes.
        /// </summary>
        [JsonPropertyName("Ship matrix size")]
        public List<string>? ShipMatrixSize { get; set; }

        /// <summary>Cargo capacity in SCU (Standard Cargo Units). Returned as a money/quantity value.</summary>
        [JsonPropertyName("Cargo capacity")]
        public List<WikiMoneyValue>? CargoCapacity { get; set; }

        /// <summary>
        /// Production state (e.g., "Flight ready", "In concept").
        /// Not currently mapped to the Ship model but available for future filtering.
        /// </summary>
        [JsonPropertyName("Production state")]
        public List<string>? ProductionState { get; set; }
    }

    /// <summary>
    /// Represents a numeric value with an optional unit, used by SMW for monetary amounts
    /// and quantities (e.g., <c>{ "value": 45.0, "unit": "USD" }</c>).
    /// </summary>
    private sealed class WikiMoneyValue
    {
        /// <summary>The numeric value (price, quantity, etc.).</summary>
        [JsonPropertyName("value")]
        public decimal Value { get; set; }

        /// <summary>The unit string (e.g., "USD", "aUEC", "SCU"), or null if unitless.</summary>
        [JsonPropertyName("unit")]
        public string? Unit { get; set; }
    }

    /// <summary>
    /// Represents a reference to another wiki page, used by SMW for linked properties
    /// like Manufacturer. The <see cref="Fulltext"/> field contains the page title
    /// (e.g., "Aegis Dynamics", "Roberts Space Industries").
    /// </summary>
    private sealed class WikiPageRef
    {
        /// <summary>The full page title of the referenced wiki page.</summary>
        [JsonPropertyName("fulltext")]
        public string? Fulltext { get; set; }
    }
}
