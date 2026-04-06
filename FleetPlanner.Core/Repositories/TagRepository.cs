using FleetPlanner.Models;

using SQLite;

namespace FleetPlanner.Repositories;

/// <summary>
/// SQLite-backed implementation of <see cref="ITagRepository"/>.
/// Uses the sqlite-net-pcl async API with lazy connection initialisation: the
/// <see cref="SQLiteAsyncConnection"/> is created on first use and the
/// <c>TagDefinition</c> table is auto-created via <c>CreateTableAsync</c>.
/// All data lives in the shared <c>FleetPlanner.db3</c> file. On MAUI
/// platforms (Android, iOS, Mac Catalyst, Windows) the database path resolves
/// to <c>FileSystem.AppDataDirectory</c>; on other targets (unit-test hosts)
/// it falls back to <c>Path.GetTempPath()</c>. A second constructor accepting
/// a <c>dbPath</c> string enables test isolation by pointing each test run at
/// a unique temporary database. <see cref="SaveTagAsync"/> uses
/// <c>InsertOrReplaceAsync</c>, so saving a tag with an existing key silently
/// overwrites the row rather than throwing.
/// </summary>
public class TagRepository : ITagRepository
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
    public TagRepository() { }

    /// <summary>Constructor for testing with a custom database path.</summary>
    public TagRepository(string dbPath) { _dbPath = dbPath; }

    private async Task<SQLiteAsyncConnection> GetConnectionAsync()
    {
        if (_db is not null) return _db;
        _db = new SQLiteAsyncConnection(DbPath, SQLiteOpenFlags.ReadWrite | SQLiteOpenFlags.Create | SQLiteOpenFlags.SharedCache);
        await _db.CreateTableAsync<TagDefinition>();
        return _db;
    }

    /// <inheritdoc/>
    public async Task<List<TagDefinition>> GetAllTagsAsync(bool includeArchived = false)
    {
        var db = await GetConnectionAsync();
        if (includeArchived)
            return await db.Table<TagDefinition>().OrderBy(t => t.Category).ThenBy(t => t.SortOrder).ToListAsync();
        return await db.Table<TagDefinition>().Where(t => !t.IsArchived).OrderBy(t => t.Category).ThenBy(t => t.SortOrder).ToListAsync();
    }

    /// <inheritdoc/>
    public async Task<List<TagDefinition>> GetTagsByCategoryAsync(string category)
    {
        var db = await GetConnectionAsync();
        return await db.Table<TagDefinition>().Where(t => t.Category == category && !t.IsArchived).OrderBy(t => t.SortOrder).ToListAsync();
    }

    /// <inheritdoc/>
    public async Task<List<TagDefinition>> GetAssignableTagsForScopeAsync(string scope)
    {
        var db = await GetConnectionAsync();
        // AllowedScopes is a comma-separated string; we use LIKE for contains check.
        var all = await db.Table<TagDefinition>().Where(t => !t.IsArchived).ToListAsync();
        return all.Where(t => t.AllowedScopes.Contains(scope)).OrderBy(t => t.Category).ThenBy(t => t.SortOrder).ToList();
    }

    /// <inheritdoc/>
    public async Task<TagDefinition?> GetTagAsync(string key)
    {
        var db = await GetConnectionAsync();
        return await db.FindAsync<TagDefinition>(key);
    }

    /// <inheritdoc/>
    public async Task<int> SaveTagAsync(TagDefinition tag)
    {
        var db = await GetConnectionAsync();
        return await db.InsertOrReplaceAsync(tag);
    }

    /// <inheritdoc/>
    public async Task ArchiveTagAsync(string key)
    {
        var db = await GetConnectionAsync();
        var tag = await db.FindAsync<TagDefinition>(key);
        if (tag is null) return;
        tag.IsArchived = true;
        await db.UpdateAsync(tag);
    }
}
