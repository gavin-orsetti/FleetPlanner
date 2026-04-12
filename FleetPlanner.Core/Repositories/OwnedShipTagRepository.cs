using FleetPlanner.Models;

using SQLite;

namespace FleetPlanner.Repositories;

/// <summary>
/// SQLite-backed implementation of <see cref="IOwnedShipTagRepository"/>.
/// Uses the sqlite-net-pcl async API with lazy connection initialisation: the
/// <see cref="SQLiteAsyncConnection"/> is created on first use and the
/// <c>OwnedShipTag</c> table is auto-created via <c>CreateTableAsync</c>.
/// All data lives in the shared <c>FleetPlanner.db3</c> file. On MAUI
/// platforms (Android, iOS, Mac Catalyst, Windows) the database path resolves
/// to <c>FileSystem.AppDataDirectory</c>; on other targets (unit-test hosts)
/// it falls back to <c>Path.GetTempPath()</c>. A second constructor accepting
/// a <c>dbPath</c> string enables test isolation by pointing each test run at
/// a unique temporary database. All removal operations are hard-deletes;
/// junction rows carry no archive flag.
/// </summary>
public class OwnedShipTagRepository : IOwnedShipTagRepository
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
    public OwnedShipTagRepository() { }

    /// <summary>Constructor for testing with a custom database path.</summary>
    public OwnedShipTagRepository(string dbPath) { _dbPath = dbPath; }

    private async Task<SQLiteAsyncConnection> GetConnectionAsync()
    {
        if (_db is not null) return _db;
        _db = new SQLiteAsyncConnection(DbPath, SQLiteOpenFlags.ReadWrite | SQLiteOpenFlags.Create | SQLiteOpenFlags.SharedCache);
        await _db.CreateTableAsync<OwnedShipTag>();
        return _db;
    }

    /// <inheritdoc/>
    public async Task<List<OwnedShipTag>> GetTagsForOwnedShipAsync(int ownedShipId, string? contextType = null, int? contextId = null)
    {
        var db = await GetConnectionAsync();
        var all = await db.Table<OwnedShipTag>().Where(t => t.OwnedShipId == ownedShipId).ToListAsync();

        if (contextType is null && contextId is null)
            return all;

        return all.Where(t => t.ContextType == contextType && t.ContextId == contextId).ToList();
    }

    /// <inheritdoc/>
    public async Task<List<OwnedShipTag>> GetOwnedShipsForTagAsync(string tagKey)
    {
        var db = await GetConnectionAsync();
        return await db.Table<OwnedShipTag>().Where(t => t.TagKey == tagKey).ToListAsync();
    }

    /// <inheritdoc/>
    public async Task<int> ApplyTagAsync(OwnedShipTag assignment)
    {
        var db = await GetConnectionAsync();
        assignment.CreatedUtc = DateTime.UtcNow;
        return await db.InsertAsync(assignment);
    }

    /// <inheritdoc/>
    public async Task RemoveTagAsync(int ownedShipId, string tagKey, string? contextType, int? contextId)
    {
        var db = await GetConnectionAsync();
        var matches = await db.Table<OwnedShipTag>()
            .Where(t => t.OwnedShipId == ownedShipId && t.TagKey == tagKey)
            .ToListAsync();

        var toDelete = matches.Where(t => t.ContextType == contextType && t.ContextId == contextId).ToList();
        foreach (var tag in toDelete)
            await db.DeleteAsync(tag);
    }

    /// <inheritdoc/>
    public async Task ReplaceTagsAsync(int ownedShipId, IEnumerable<OwnedShipTag> tags)
    {
        var db = await GetConnectionAsync();

        // Delete existing global tags (ContextType == null)
        var existing = await db.Table<OwnedShipTag>()
            .Where(t => t.OwnedShipId == ownedShipId)
            .ToListAsync();
        var globals = existing.Where(t => t.ContextType == null).ToList();
        foreach (var tag in globals)
            await db.DeleteAsync(tag);

        // Insert new tags
        foreach (var tag in tags)
        {
            tag.OwnedShipId = ownedShipId;
            tag.CreatedUtc = DateTime.UtcNow;
            await db.InsertAsync(tag);
        }
    }

    /// <inheritdoc/>
    public async Task DeleteContextualTagsForGroupAsync(int groupId)
    {
        var db = await GetConnectionAsync();
        var all = await db.Table<OwnedShipTag>().ToListAsync();
        var toDelete = all.Where(t => t.ContextType == "group" && t.ContextId == groupId).ToList();
        foreach (var tag in toDelete)
            await db.DeleteAsync(tag);
    }
}
