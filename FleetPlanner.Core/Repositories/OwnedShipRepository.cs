using FleetPlanner.Models;

using SQLite;

namespace FleetPlanner.Repositories;

/// <summary>
/// SQLite-backed implementation of <see cref="IOwnedShipRepository"/>.
/// Uses the sqlite-net-pcl async API with lazy connection initialisation.
/// </summary>
public class OwnedShipRepository : IOwnedShipRepository
{
    private SQLiteAsyncConnection? _db;
    private string? _dbPath;

    private string DbPath => _dbPath ??=
#if ANDROID || IOS || MACCATALYST || WINDOWS
        Path.Combine(FileSystem.AppDataDirectory, "FleetPlanner.db3");
#else
        Path.Combine(Path.GetTempPath(), "FleetPlanner.db3");
#endif

    /// <summary>Parameterless constructor for DI registration.</summary>
    public OwnedShipRepository() { }

    /// <summary>Constructor for testing with a custom database path.</summary>
    public OwnedShipRepository(string dbPath) { _dbPath = dbPath; }

    private async Task<SQLiteAsyncConnection> GetConnectionAsync()
    {
        if (_db is not null) return _db;
        _db = new SQLiteAsyncConnection(DbPath, SQLiteOpenFlags.ReadWrite | SQLiteOpenFlags.Create | SQLiteOpenFlags.SharedCache);
        await _db.CreateTableAsync<OwnedShip>();
        return _db;
    }

    /// <inheritdoc/>
    public async Task<List<OwnedShip>> GetAllOwnedShipsAsync(bool includeArchived = false)
    {
        var db = await GetConnectionAsync();
        if (includeArchived)
            return await db.Table<OwnedShip>().OrderByDescending(s => s.CreatedUtc).ToListAsync();
        return await db.Table<OwnedShip>().Where(s => !s.IsArchived).OrderByDescending(s => s.CreatedUtc).ToListAsync();
    }

    /// <inheritdoc/>
    public async Task<OwnedShip?> GetOwnedShipAsync(int id)
    {
        var db = await GetConnectionAsync();
        return await db.FindAsync<OwnedShip>(id);
    }

    /// <inheritdoc/>
    public async Task<int> SaveOwnedShipAsync(OwnedShip ship)
    {
        var db = await GetConnectionAsync();
        ship.UpdatedUtc = DateTime.UtcNow;
        if (ship.Id != 0)
            return await db.UpdateAsync(ship);
        ship.CreatedUtc = DateTime.UtcNow;
        return await db.InsertAsync(ship);
    }

    /// <inheritdoc/>
    public async Task ArchiveOwnedShipAsync(int id)
    {
        var db = await GetConnectionAsync();
        var ship = await db.FindAsync<OwnedShip>(id);
        if (ship is null) return;
        ship.IsArchived = true;
        ship.UpdatedUtc = DateTime.UtcNow;
        await db.UpdateAsync(ship);
    }

    /// <inheritdoc/>
    public async Task<int> DeleteOwnedShipAsync(int id)
    {
        var db = await GetConnectionAsync();
        return await db.DeleteAsync<OwnedShip>(id);
    }
}
