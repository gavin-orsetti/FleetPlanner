using FleetPlanner.Models;

using SQLite;

namespace FleetPlanner.Repositories;

public class FleetRepository : IFleetRepository
{
    private SQLiteAsyncConnection? _db;

    // FIX: Lazy-resolve the database path. FileSystem.AppDataDirectory is not
    // available when the DI container builds singletons during
    // MauiProgram.CreateMauiApp() on Android. Deferring to first use
    // ensures the platform is fully initialised.
    private string? _dbPath;
    private string DbPath => _dbPath ??= Path.Combine(FileSystem.AppDataDirectory, "FleetPlanner.db3");

    public FleetRepository()
    {
    }

    private async Task<SQLiteAsyncConnection> GetConnectionAsync()
    {
        if (_db is not null)
            return _db;

        _db = new SQLiteAsyncConnection(DbPath, SQLiteOpenFlags.ReadWrite | SQLiteOpenFlags.Create | SQLiteOpenFlags.SharedCache);
        await _db.CreateTableAsync<Fleet>();
        await _db.CreateTableAsync<FleetShip>();
        return _db;
    }

    public async Task InitializeAsync()
    {
        await GetConnectionAsync();
    }

    public async Task<List<Fleet>> GetAllFleetsAsync()
    {
        var db = await GetConnectionAsync();
        return await db.Table<Fleet>().OrderBy(f => f.Name).ToListAsync();
    }

    public async Task<Fleet?> GetFleetAsync(int id)
    {
        var db = await GetConnectionAsync();
        return await db.FindAsync<Fleet>(id);
    }

    public async Task<int> SaveFleetAsync(Fleet fleet)
    {
        var db = await GetConnectionAsync();
        if (fleet.Id != 0)
            return await db.UpdateAsync(fleet);
        return await db.InsertAsync(fleet);
    }

    public async Task<int> DeleteFleetAsync(int id)
    {
        var db = await GetConnectionAsync();
        await DeleteFleetShipsByFleetAsync(id);
        return await db.DeleteAsync<Fleet>(id);
    }

    public async Task<List<FleetShip>> GetFleetShipsAsync(int fleetId)
    {
        var db = await GetConnectionAsync();
        return await db.Table<FleetShip>().Where(fs => fs.FleetId == fleetId).ToListAsync();
    }

    public async Task<FleetShip?> GetFleetShipAsync(int id)
    {
        var db = await GetConnectionAsync();
        return await db.FindAsync<FleetShip>(id);
    }

    public async Task<int> SaveFleetShipAsync(FleetShip fleetShip)
    {
        var db = await GetConnectionAsync();
        if (fleetShip.Id != 0)
            return await db.UpdateAsync(fleetShip);
        return await db.InsertAsync(fleetShip);
    }

    public async Task<int> DeleteFleetShipAsync(int id)
    {
        var db = await GetConnectionAsync();
        return await db.DeleteAsync<FleetShip>(id);
    }

    public async Task DeleteFleetShipsByFleetAsync(int fleetId)
    {
        var db = await GetConnectionAsync();
        var ships = await GetFleetShipsAsync(fleetId);
        foreach (var ship in ships)
            await db.DeleteAsync<FleetShip>(ship.Id);
    }
}
