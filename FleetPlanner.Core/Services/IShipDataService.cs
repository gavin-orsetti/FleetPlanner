using FleetPlanner.Models;

namespace FleetPlanner.Services;

/// <summary>
/// Abstraction for retrieving Star Citizen ship data.
/// <para>
/// Two implementations exist, illustrating the <b>Decorator Pattern</b>:
/// <list type="bullet">
///   <item><see cref="ShipDataService"/> — the "live" implementation that fetches from the UEX Corp API over HTTP.</item>
///   <item><see cref="CachedShipDataService"/> — wraps the live service, adding a SQLite caching layer
///     so the app works offline and avoids redundant network calls.</item>
/// </list>
/// The DI container registers <c>CachedShipDataService</c> as the <c>IShipDataService</c> singleton,
/// so all consumers (ViewModels, RecommendationService) automatically get caching behaviour
/// without knowing about it — that's the power of coding against an interface.
/// </para>
/// </summary>
/// <see href="https://learn.microsoft.com/en-us/dotnet/maui/fundamentals/dependency-injection"/>
public interface IShipDataService
{
    /// <summary>
    /// Returns all known Star Citizen ships.
    /// </summary>
    /// <param name="forceRefresh">
    /// When <see langword="true"/>, bypasses the cache and fetches fresh data from the API.
    /// When <see langword="false"/> (default), returns cached data if available.
    /// </param>
    /// <returns>A list of <see cref="Ship"/> records, ordered alphabetically by name.</returns>
    Task<List<Ship>> GetAllShipsAsync(bool forceRefresh = false);

    /// <summary>
    /// Returns a single ship by its ID, or <see langword="null"/> if not found.
    /// </summary>
    /// <param name="id">The ship's <see cref="Ship.Id"/> (from the UEX Corp API).</param>
    Task<Ship?> GetShipAsync(int id);

    /// <summary>
    /// Returns the UTC timestamp of the last successful data fetch, or <see langword="null"/>
    /// if the cache has never been populated.
    /// </summary>
    Task<DateTime?> GetLastUpdatedAsync();
}
