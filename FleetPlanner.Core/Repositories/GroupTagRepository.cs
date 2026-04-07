using FleetPlanner.Models;

using SQLite;

namespace FleetPlanner.Repositories;

/// <summary>
/// SQLite-backed implementation of <see cref="IGroupTagRepository"/>.
/// Uses the sqlite-net-pcl async API with lazy connection initialisation: the
/// <see cref="SQLiteAsyncConnection"/> is created on first use and the
/// <c>GroupTagDefinition</c> table is auto-created via <c>CreateTableAsync</c>.
/// All data lives in the shared <c>FleetPlanner.db3</c> file. On MAUI platforms
/// (Android, iOS, Mac Catalyst, Windows) the database path resolves to
/// <c>FileSystem.AppDataDirectory</c>; on other targets (unit-test hosts) it
/// falls back to <c>Path.GetTempPath()</c>. A second constructor accepting a
/// <c>dbPath</c> string enables test isolation.
/// </summary>
public class GroupTagRepository : IGroupTagRepository
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
    public GroupTagRepository() { }

    /// <summary>Constructor for testing with a custom database path.</summary>
    public GroupTagRepository(string dbPath) { _dbPath = dbPath; }

    private async Task<SQLiteAsyncConnection> GetConnectionAsync()
    {
        if (_db is not null) return _db;
        _db = new SQLiteAsyncConnection(DbPath, SQLiteOpenFlags.ReadWrite | SQLiteOpenFlags.Create | SQLiteOpenFlags.SharedCache);
        await _db.CreateTableAsync<GroupTagDefinition>();
        return _db;
    }

    /// <inheritdoc/>
    public async Task<List<GroupTagDefinition>> GetAllTagsAsync(bool includeArchived = false)
    {
        var db = await GetConnectionAsync();
        if (includeArchived)
            return await db.Table<GroupTagDefinition>().OrderBy(t => t.Category).ThenBy(t => t.SortOrder).ToListAsync();
        return await db.Table<GroupTagDefinition>().Where(t => !t.IsArchived).OrderBy(t => t.Category).ThenBy(t => t.SortOrder).ToListAsync();
    }

    /// <inheritdoc/>
    public async Task<GroupTagDefinition?> GetTagByKeyAsync(string key)
    {
        var db = await GetConnectionAsync();
        return await db.FindAsync<GroupTagDefinition>(key);
    }

    /// <inheritdoc/>
    public async Task SaveTagAsync(GroupTagDefinition tag)
    {
        var db = await GetConnectionAsync();
        await db.InsertOrReplaceAsync(tag);
    }

    /// <inheritdoc/>
    public async Task ArchiveTagAsync(string key)
    {
        var db = await GetConnectionAsync();
        var tag = await db.FindAsync<GroupTagDefinition>(key);
        if (tag is null) return;
        tag.IsArchived = true;
        await db.UpdateAsync(tag);
    }

    /// <inheritdoc/>
    public async Task DeleteTagAsync(string key)
    {
        var db = await GetConnectionAsync();
        var tag = await db.FindAsync<GroupTagDefinition>(key);
        if (tag is null) return;
        await db.DeleteAsync(tag);
    }
}
