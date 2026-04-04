using FleetPlanner.Models;

using SQLite;

namespace FleetPlanner.Repositories;

public class FleetRepository : IFleetRepository
{
    private SQLiteAsyncConnection? _db;

    private string? _dbPath;
    private string DbPath => _dbPath ??=
#if ANDROID || IOS || MACCATALYST || WINDOWS
        Path.Combine(FileSystem.AppDataDirectory, "FleetPlanner.db3");
#else
        Path.Combine(Path.GetTempPath(), "FleetPlanner.db3");
#endif

    public FleetRepository()
    {
    }

    /// <summary>
    /// Constructor for testing — accepts a custom database path.
    /// </summary>
    public FleetRepository(string dbPath)
    {
        _dbPath = dbPath;
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
        await EnsureDefaultFleetAsync();
    }

    public async Task EnsureDefaultFleetAsync()
    {
        var db = await GetConnectionAsync();
        var fleets = await db.Table<Fleet>().ToListAsync();
        if (fleets.Count == 0)
        {
            var defaultFleet = new Fleet
            {
                Name = "My Fleet",
                PrimaryFocus = (int)FleetFocus.Multipurpose,
                OperatingScale = (int)FleetOperatingScale.Solo,
                AvailableCrewCount = 1,
                DateCreated = DateTime.UtcNow,
                DateModified = DateTime.UtcNow
            };
            await db.InsertAsync(defaultFleet);

            // Assign any orphaned FleetShip rows to the default fleet
            var orphanedShips = await db.Table<FleetShip>().Where(fs => fs.FleetId == 0).ToListAsync();
            foreach (var ship in orphanedShips)
            {
                ship.FleetId = defaultFleet.Id;
                await db.UpdateAsync(ship);
            }
        }
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
        fleet.DateModified = DateTime.UtcNow;
        if (fleet.Id != 0)
            return await db.UpdateAsync(fleet);
        fleet.DateCreated = DateTime.UtcNow;
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
