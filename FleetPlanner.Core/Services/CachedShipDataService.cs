using FleetPlanner.Models;
using SQLite;

namespace FleetPlanner.Services;

/// <summary>
/// Caching decorator for <see cref="IShipDataService"/>. Stores ship data in SQLite
/// and serves from cache when available. Only hits the network on first launch or manual refresh.
/// <para>
/// <b>Decorator Pattern:</b> This class implements <see cref="IShipDataService"/> and wraps
/// another <c>IShipDataService</c> (the "live" <see cref="ShipDataService"/>). Consumers
/// depend on the interface and don't know about the caching layer — it's transparent.
/// This is a classic Gang-of-Four Decorator: same interface, added behaviour (caching).
/// </para>
/// <para>
/// <b>Offline-first strategy:</b>
/// <list type="number">
///   <item>If cache has data and <c>forceRefresh</c> is false → return cached data immediately (no network).</item>
///   <item>If cache is empty or <c>forceRefresh</c> is true → try fetching from the API.</item>
///   <item>If the API call fails (no network, server error) → fall back to whatever is in the cache.</item>
/// </list>
/// This means the app always works, even without internet — the user just sees potentially stale data.
/// </para>
/// <para>
/// <b>Two constructors (DI vs. testing):</b>
/// <list type="bullet">
///   <item><c>CachedShipDataService(ShipDataService)</c> — used by the DI container at runtime.
///     Takes the concrete <see cref="ShipDataService"/> type (not the interface) so the DI
///     container can resolve it without circular confusion. See <c>MauiProgram.cs</c> for the
///     factory lambda that wires this up.</item>
///   <item><c>CachedShipDataService(IShipDataService, string)</c> — used by unit tests.
///     Accepts the interface (for mocking with NSubstitute) and a custom DB path (for isolation).</item>
/// </list>
/// </para>
/// </summary>
/// <see href="https://github.com/praeclarum/sqlite-net"/>
/// <see href="https://learn.microsoft.com/en-us/dotnet/maui/fundamentals/dependency-injection"/>
public class CachedShipDataService : IShipDataService
{
    /// <summary>
    /// The wrapped "live" service that actually talks to the starcitizen.tools wiki API.
    /// Only called when the cache is empty or the user explicitly refreshes.
    /// </summary>
    private readonly IShipDataService _liveService;

    /// <summary>
    /// Lazily-initialised SQLite connection for the ship cache.
    /// Shares the same database file as all other repositories
    /// — all use "FleetPlanner.db3" — so ships, tags, groups, and cache metadata live side by side.
    /// </summary>
    private SQLiteAsyncConnection? _db;

    /// <summary>Cached database file path. Set once and reused.</summary>
    private string? _dbPath;

    /// <summary>
    /// Resolves the database path using the same platform-conditional pattern as
    /// all other repositories. Every repository points to "FleetPlanner.db3"
    /// so all data lives in a single SQLite file.
    /// </summary>
    private string DbPath => _dbPath ??=
#if ANDROID || IOS || MACCATALYST || WINDOWS
        Path.Combine(FileSystem.AppDataDirectory, "FleetPlanner.db3");
#else
        Path.Combine(Path.GetTempPath(), "FleetPlanner.db3");
#endif

    /// <summary>
    /// Runtime constructor — takes the concrete <see cref="ShipDataService"/> type.
    /// <para>
    /// <b>Why concrete type instead of interface?</b> If both this class and <c>ShipDataService</c>
    /// were registered as <c>IShipDataService</c>, the DI container would hit a circular resolution:
    /// resolving <c>IShipDataService</c> → <c>CachedShipDataService</c> → needs <c>IShipDataService</c> → loop.
    /// By depending on the concrete <c>ShipDataService</c>, the DI container resolves it directly,
    /// then passes it into this decorator. See the factory lambda in <c>MauiProgram.cs</c>.
    /// </para>
    /// </summary>
    /// <param name="liveService">The live API service to wrap with caching.</param>
    public CachedShipDataService(ShipDataService liveService)
    {
        _liveService = liveService;
    }

    /// <summary>
    /// Constructor for testing — accepts a mock service and custom database path.
    /// <para>
    /// Using <c>IShipDataService</c> (interface) here lets tests pass in an NSubstitute mock.
    /// The <c>dbPath</c> parameter points to a temp file so tests don't pollute the real database.
    /// </para>
    /// </summary>
    /// <param name="liveService">A mock or stub <c>IShipDataService</c> for testing.</param>
    /// <param name="dbPath">Path to a temporary SQLite database file.</param>
    public CachedShipDataService(IShipDataService liveService, string dbPath)
    {
        _liveService = liveService;
        _dbPath = dbPath;
    }

    /// <summary>
    /// Returns the shared database connection, creating the Ship and ShipCacheMetadata
    /// tables on first call. Same lazy-init pattern used by all repository implementations.
    /// </summary>
    private async Task<SQLiteAsyncConnection> GetConnectionAsync()
    {
        if (_db is not null)
            return _db;

        _db = new SQLiteAsyncConnection(DbPath, SQLiteOpenFlags.ReadWrite | SQLiteOpenFlags.Create | SQLiteOpenFlags.SharedCache);
        await _db.CreateTableAsync<Ship>();
        await _db.CreateTableAsync<ShipCacheMetadata>();
        return _db;
    }

    /// <summary>
    /// Returns all ships, using a cache-first strategy with offline fallback.
    /// <para>
    /// <b>Algorithm:</b>
    /// <list type="number">
    ///   <item>If not forcing refresh, check the local SQLite cache. If it has data, return it immediately.</item>
    ///   <item>Otherwise, call the live service to fetch fresh data from the API.</item>
    ///   <item>On success: clear the old cache → insert new data → update the metadata timestamp.</item>
    ///   <item>On failure (network error, etc.): catch the exception and return stale cached data.</item>
    /// </list>
    /// </para>
    /// </summary>
    /// <param name="forceRefresh">When <see langword="true"/>, skips the cache and fetches from the API.</param>
    /// <returns>Ships ordered alphabetically by name. May be empty on first launch with no network.</returns>
    public async Task<List<Ship>> GetAllShipsAsync(bool forceRefresh = false)
    {
        var db = await GetConnectionAsync();

        // Step 1: Try to serve from cache (skip if user explicitly requested a refresh).
        if (!forceRefresh)
        {
            var cached = await db.Table<Ship>().ToListAsync();
            if (cached.Count > 0)
                return cached.OrderBy(s => s.Name).ToList();
        }

        // Step 2: Cache miss or forced refresh — fetch from the live API.
        try
        {
            var ships = await _liveService.GetAllShipsAsync();
            if (ships.Count > 0)
            {
                // Replace the entire cache atomically: delete all → insert all.
                // This is simpler than diffing and handles removed/renamed ships correctly.
                await db.DeleteAllAsync<Ship>();
                await db.InsertAllAsync(ships);

                // Record when we last successfully fetched, so the UI can show "Last updated: ..."
                var meta = new ShipCacheMetadata
                {
                    Key = "ship_cache",
                    LastFetched = DateTime.UtcNow
                };
                // InsertOrReplace: if the "ship_cache" row exists, update it; otherwise insert.
                await db.InsertOrReplaceAsync(meta);
            }
            return ships.OrderBy(s => s.Name).ToList();
        }
        catch (Exception)
        {
            // Step 3: Offline fallback — return whatever stale data we have cached.
            // This is the key benefit of the caching decorator: the app never crashes
            // due to network issues; it gracefully degrades to last-known-good data.
            var cached = await db.Table<Ship>().ToListAsync();
            return cached.OrderBy(s => s.Name).ToList();
        }
    }

    /// <summary>
    /// Looks up a single ship by ID from the local SQLite cache.
    /// This is a fast O(1) primary-key lookup — no network call needed.
    /// </summary>
    /// <param name="id">The ship's <see cref="Ship.Id"/>.</param>
    /// <returns>The cached ship, or <see langword="null"/> if not in the cache.</returns>
    public async Task<Ship?> GetShipAsync(int id)
    {
        var db = await GetConnectionAsync();
        return await db.FindAsync<Ship>(id);
    }

    /// <summary>
    /// Returns the timestamp of the last successful API fetch from the
    /// <see cref="ShipCacheMetadata"/> table, or <see langword="null"/> if never fetched.
    /// Displayed in the UI as "Ship data last updated: {date}".
    /// </summary>
    public async Task<DateTime?> GetLastUpdatedAsync()
    {
        var db = await GetConnectionAsync();
        // FindAsync by primary key ("ship_cache") — single-row lookup.
        var meta = await db.FindAsync<ShipCacheMetadata>("ship_cache");
        return meta?.LastFetched;
    }
}
