using FleetPlanner.Models;

using SQLite;

namespace FleetPlanner.Services;

/// <summary>
/// Caching decorator for IShipDataService. Stores ship data in SQLite
/// and serves from cache when available. Only hits the network on
/// first launch or manual refresh.
/// </summary>
public class CachedShipDataService : IShipDataService
{
    private readonly ShipDataService _liveService;
    private SQLiteAsyncConnection? _db;

    // FIX: Lazy-resolve the database path. FileSystem.AppDataDirectory is not
    // available when the DI container builds singletons during
    // MauiProgram.CreateMauiApp() on Android. Deferring to first use
    // ensures the platform is fully initialised.
    private string? _dbPath;
    private string DbPath => _dbPath ??= Path.Combine(FileSystem.AppDataDirectory, "FleetPlanner.db3");

    public CachedShipDataService(ShipDataService liveService)
    {
        _liveService = liveService;
    }

    private async Task<SQLiteAsyncConnection> GetConnectionAsync()
    {
        if (_db is not null)
            return _db;

        _db = new SQLiteAsyncConnection(DbPath, SQLiteOpenFlags.ReadWrite | SQLiteOpenFlags.Create | SQLiteOpenFlags.SharedCache);
        await _db.CreateTableAsync<Ship>();
        await _db.CreateTableAsync<ShipCacheMetadata>();
        return _db;
    }

    public async Task<List<Ship>> GetAllShipsAsync(bool forceRefresh = false)
    {
        var db = await GetConnectionAsync();

        if (!forceRefresh)
        {
            var cached = await db.Table<Ship>().ToListAsync();
            if (cached.Count > 0)
                return cached.OrderBy(s => s.Name).ToList();
        }

        try
        {
            var ships = await _liveService.GetAllShipsAsync();
            if (ships.Count > 0)
            {
                await db.DeleteAllAsync<Ship>();
                await db.InsertAllAsync(ships);

                var meta = new ShipCacheMetadata
                {
                    Key = "ship_cache",
                    LastFetched = DateTime.UtcNow
                };
                await db.InsertOrReplaceAsync(meta);
            }
            return ships.OrderBy(s => s.Name).ToList();
        }
        catch (Exception)
        {
            // Offline fallback: return whatever we have cached
            var cached = await db.Table<Ship>().ToListAsync();
            return cached.OrderBy(s => s.Name).ToList();
        }
    }

    public async Task<Ship?> GetShipAsync(int id)
    {
        var db = await GetConnectionAsync();
        return await db.FindAsync<Ship>(id);
    }

    public async Task<DateTime?> GetLastUpdatedAsync()
    {
        var db = await GetConnectionAsync();
        var meta = await db.FindAsync<ShipCacheMetadata>("ship_cache");
        return meta?.LastFetched;
    }
}
