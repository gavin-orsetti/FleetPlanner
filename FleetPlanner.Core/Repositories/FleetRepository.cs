using FleetPlanner.Models;

using SQLite;

namespace FleetPlanner.Repositories;

/// <summary>
/// SQLite-backed implementation of <see cref="IFleetRepository"/>.
/// <para>
/// Uses the <b>sqlite-net-pcl</b> library's async API (<see cref="SQLiteAsyncConnection"/>)
/// for all database operations. The connection is lazily initialised on first use and reused
/// for the lifetime of the app — this is safe because <c>SQLiteAsyncConnection</c> serialises
/// all operations onto a single background thread internally.
/// </para>
/// <para>
/// <b>Two constructors:</b>
/// <list type="bullet">
///   <item>Parameterless — used by the DI container at runtime. Derives the database path
///     from <c>FileSystem.AppDataDirectory</c> (a MAUI API that returns the platform-specific
///     app-private storage directory).</item>
///   <item><c>FleetRepository(string dbPath)</c> — used by unit tests to point at an
///     in-memory or temp-file database.</item>
/// </list>
/// </para>
/// </summary>
/// <see href="https://github.com/praeclarum/sqlite-net"/>
/// <see href="https://learn.microsoft.com/en-us/dotnet/maui/fundamentals/dependency-injection"/>
public class FleetRepository : IFleetRepository
{
    /// <summary>
    /// Lazily-initialised database connection. Null until <see cref="GetConnectionAsync"/> is called.
    /// </summary>
    private SQLiteAsyncConnection? _db;

    /// <summary>Cached database file path. Set once and reused.</summary>
    private string? _dbPath;

    /// <summary>
    /// Resolves the database file path using a platform-conditional compilation directive.
    /// <para>
    /// On mobile/desktop platforms, <c>FileSystem.AppDataDirectory</c> gives us the
    /// platform-appropriate private storage folder (e.g., Android's internal storage,
    /// iOS's Documents, Windows AppData). On other platforms (e.g., unit tests running
    /// on plain .NET), we fall back to the system temp directory.
    /// </para>
    /// </summary>
    private string DbPath => _dbPath ??=
#if ANDROID || IOS || MACCATALYST || WINDOWS
        Path.Combine(FileSystem.AppDataDirectory, "FleetPlanner.db3");
#else
        Path.Combine(Path.GetTempPath(), "FleetPlanner.db3");
#endif

    /// <summary>
    /// Parameterless constructor for DI registration. The database path is resolved
    /// lazily from <see cref="DbPath"/> on first access.
    /// </summary>
    public FleetRepository()
    {
    }

    /// <summary>
    /// Constructor for testing — accepts a custom database path so tests can use
    /// isolated temp files instead of the app's real database.
    /// </summary>
    /// <param name="dbPath">Full path to the SQLite database file.</param>
    public FleetRepository(string dbPath)
    {
        _dbPath = dbPath;
    }

    /// <summary>
    /// Returns the shared database connection, creating it (and the tables) on first call.
    /// <para>
    /// <b>Lazy initialisation pattern:</b> We don't open the database in the constructor
    /// because (a) constructors can't be <see langword="async"/>, and (b) the DI container
    /// calls the constructor at registration time — we don't want to hit the filesystem then.
    /// </para>
    /// <para>
    /// <c>CreateTableAsync&lt;T&gt;</c> is idempotent — it creates the table only if it
    /// doesn't already exist, and adds any new columns found in the model class (schema migration).
    /// </para>
    /// </summary>
    private async Task<SQLiteAsyncConnection> GetConnectionAsync()
    {
        if (_db is not null)
            return _db;

        // SQLiteOpenFlags: ReadWrite + Create = create file if missing; SharedCache = allow
        // multiple connections (not strictly needed here, but safe for future use).
        _db = new SQLiteAsyncConnection(DbPath, SQLiteOpenFlags.ReadWrite | SQLiteOpenFlags.Create | SQLiteOpenFlags.SharedCache);

        // CreateTableAsync is the sqlite-net migration mechanism: it CREATEs the table if missing,
        // and ALTERs it to add any new columns that exist in the C# model but not in the schema.
        await _db.CreateTableAsync<Fleet>();
        await _db.CreateTableAsync<FleetShip>();
        return _db;
    }

    /// <inheritdoc/>
    public async Task InitializeAsync()
    {
        // Ensure tables exist, then seed default data if the database is fresh.
        await GetConnectionAsync();
        await EnsureDefaultFleetAsync();
    }

    /// <inheritdoc/>
    /// <remarks>
    /// <b>Migration logic:</b> This method handles the V1 → V2 migration scenario where
    /// V1 stored FleetShip rows with <c>FleetId = 0</c> (because V1 only had a single
    /// implicit fleet). After creating the default fleet, we reassign those orphans so
    /// existing user data isn't lost on upgrade.
    /// </remarks>
    public async Task EnsureDefaultFleetAsync()
    {
        var db = await GetConnectionAsync();
        var fleets = await db.Table<Fleet>().ToListAsync();

        // Only seed if the database has zero fleets (fresh install or cleared data).
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

            // Reassign orphaned FleetShip rows from V1 (FleetId == 0) to the new default fleet.
            // This preserves user data across the V1 → V2 schema migration.
            var orphanedShips = await db.Table<FleetShip>().Where(fs => fs.FleetId == 0).ToListAsync();
            foreach (var ship in orphanedShips)
            {
                ship.FleetId = defaultFleet.Id;
                await db.UpdateAsync(ship);
            }
        }
    }

    /// <inheritdoc/>
    public async Task<List<Fleet>> GetAllFleetsAsync()
    {
        var db = await GetConnectionAsync();
        // OrderBy(Name) for consistent, alphabetical display in the fleet list UI.
        return await db.Table<Fleet>().OrderBy(f => f.Name).ToListAsync();
    }

    /// <inheritdoc/>
    public async Task<Fleet?> GetFleetAsync(int id)
    {
        var db = await GetConnectionAsync();
        // FindAsync looks up by primary key — O(1) via SQLite's rowid index.
        return await db.FindAsync<Fleet>(id);
    }

    /// <inheritdoc/>
    /// <remarks>
    /// Uses the "Id == 0 means new" convention: SQLite auto-increment starts at 1,
    /// so a freshly-constructed entity with <c>Id = 0</c> has never been persisted.
    /// The <c>DateModified</c> timestamp is always refreshed; <c>DateCreated</c> is
    /// set only on insert.
    /// </remarks>
    public async Task<int> SaveFleetAsync(Fleet fleet)
    {
        var db = await GetConnectionAsync();
        fleet.DateModified = DateTime.UtcNow;

        if (fleet.Id != 0)
            return await db.UpdateAsync(fleet);

        // New fleet — set creation timestamp and insert.
        fleet.DateCreated = DateTime.UtcNow;
        return await db.InsertAsync(fleet);
    }

    /// <inheritdoc/>
    /// <remarks>
    /// Cascading delete: removes all child FleetShip rows first, then the Fleet itself.
    /// SQLite-net doesn't support ON DELETE CASCADE in its ORM layer, so we do it manually.
    /// </remarks>
    public async Task<int> DeleteFleetAsync(int id)
    {
        var db = await GetConnectionAsync();
        // Delete children first to maintain referential integrity.
        await DeleteFleetShipsByFleetAsync(id);
        return await db.DeleteAsync<Fleet>(id);
    }

    /// <inheritdoc/>
    public async Task<List<FleetShip>> GetFleetShipsAsync(int fleetId)
    {
        var db = await GetConnectionAsync();
        // WHERE FleetId = ? — uses the [Indexed] attribute on FleetShip.FleetId for fast lookup.
        return await db.Table<FleetShip>().Where(fs => fs.FleetId == fleetId).ToListAsync();
    }

    /// <inheritdoc/>
    public async Task<FleetShip?> GetFleetShipAsync(int id)
    {
        var db = await GetConnectionAsync();
        return await db.FindAsync<FleetShip>(id);
    }

    /// <inheritdoc/>
    public async Task<int> SaveFleetShipAsync(FleetShip fleetShip)
    {
        var db = await GetConnectionAsync();
        // Same "Id == 0 means new" convention as SaveFleetAsync.
        if (fleetShip.Id != 0)
            return await db.UpdateAsync(fleetShip);
        return await db.InsertAsync(fleetShip);
    }

    /// <inheritdoc/>
    public async Task<int> DeleteFleetShipAsync(int id)
    {
        var db = await GetConnectionAsync();
        return await db.DeleteAsync<FleetShip>(id);
    }

    /// <inheritdoc/>
    /// <remarks>
    /// Iterates and deletes one-by-one because sqlite-net's async API doesn't support
    /// bulk deletes with a WHERE clause. For the expected fleet sizes (tens of ships),
    /// this is fast enough — no need for raw SQL.
    /// </remarks>
    public async Task DeleteFleetShipsByFleetAsync(int fleetId)
    {
        var db = await GetConnectionAsync();
        var ships = await GetFleetShipsAsync(fleetId);
        foreach (var ship in ships)
            await db.DeleteAsync<FleetShip>(ship.Id);
    }
}
