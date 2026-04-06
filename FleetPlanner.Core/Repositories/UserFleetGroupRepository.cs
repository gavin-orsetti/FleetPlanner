using FleetPlanner.Models;

using SQLite;

namespace FleetPlanner.Repositories;

/// <summary>
/// SQLite-backed implementation of <see cref="IUserFleetGroupRepository"/>.
/// Uses the sqlite-net-pcl async API with lazy connection initialisation: the
/// <see cref="SQLiteAsyncConnection"/> is created on first use and the
/// <c>UserFleetGroup</c> table is auto-created via <c>CreateTableAsync</c>.
/// All data lives in the shared <c>FleetPlanner.db3</c> file. On MAUI
/// platforms (Android, iOS, Mac Catalyst, Windows) the database path resolves
/// to <c>FileSystem.AppDataDirectory</c>; on other targets (unit-test hosts)
/// it falls back to <c>Path.GetTempPath()</c>. A second constructor accepting
/// a <c>dbPath</c> string enables test isolation by pointing each test run at
/// a unique temporary database.
/// </summary>
public class UserFleetGroupRepository : IUserFleetGroupRepository
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
    public UserFleetGroupRepository() { }

    /// <summary>Constructor for testing with a custom database path.</summary>
    public UserFleetGroupRepository(string dbPath) { _dbPath = dbPath; }

    private async Task<SQLiteAsyncConnection> GetConnectionAsync()
    {
        if (_db is not null) return _db;
        _db = new SQLiteAsyncConnection(DbPath, SQLiteOpenFlags.ReadWrite | SQLiteOpenFlags.Create | SQLiteOpenFlags.SharedCache);
        await _db.CreateTableAsync<UserFleetGroup>();
        return _db;
    }

    /// <inheritdoc/>
    public async Task<List<UserFleetGroup>> GetAllGroupsAsync(bool includeArchived = false)
    {
        var db = await GetConnectionAsync();
        if (includeArchived)
            return await db.Table<UserFleetGroup>().OrderBy(g => g.SortOrder).ThenBy(g => g.Name).ToListAsync();
        return await db.Table<UserFleetGroup>().Where(g => !g.IsArchived).OrderBy(g => g.SortOrder).ThenBy(g => g.Name).ToListAsync();
    }

    /// <inheritdoc/>
    public async Task<UserFleetGroup?> GetGroupAsync(int id)
    {
        var db = await GetConnectionAsync();
        return await db.FindAsync<UserFleetGroup>(id);
    }

    /// <inheritdoc/>
    public async Task<int> SaveGroupAsync(UserFleetGroup group)
    {
        var db = await GetConnectionAsync();
        group.UpdatedUtc = DateTime.UtcNow;
        if (group.Id != 0)
            return await db.UpdateAsync(group);
        group.CreatedUtc = DateTime.UtcNow;
        return await db.InsertAsync(group);
    }

    /// <inheritdoc/>
    public async Task ArchiveGroupAsync(int id)
    {
        var db = await GetConnectionAsync();
        var group = await db.FindAsync<UserFleetGroup>(id);
        if (group is null) return;
        group.IsArchived = true;
        group.UpdatedUtc = DateTime.UtcNow;
        await db.UpdateAsync(group);
    }

    /// <inheritdoc/>
    public async Task<int> DeleteGroupAsync(int id)
    {
        var db = await GetConnectionAsync();
        return await db.DeleteAsync<UserFleetGroup>(id);
    }
}
