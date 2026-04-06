using FleetPlanner.Models;

using SQLite;

namespace FleetPlanner.Repositories;

/// <summary>
/// SQLite-backed implementation of <see cref="IUserFleetGroupTagRepository"/>.
/// </summary>
public class UserFleetGroupTagRepository : IUserFleetGroupTagRepository
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
    public UserFleetGroupTagRepository() { }

    /// <summary>Constructor for testing with a custom database path.</summary>
    public UserFleetGroupTagRepository(string dbPath) { _dbPath = dbPath; }

    private async Task<SQLiteAsyncConnection> GetConnectionAsync()
    {
        if (_db is not null) return _db;
        _db = new SQLiteAsyncConnection(DbPath, SQLiteOpenFlags.ReadWrite | SQLiteOpenFlags.Create | SQLiteOpenFlags.SharedCache);
        await _db.CreateTableAsync<UserFleetGroupTag>();
        return _db;
    }

    /// <inheritdoc/>
    public async Task<List<UserFleetGroupTag>> GetTagsForGroupAsync(int groupId)
    {
        var db = await GetConnectionAsync();
        return await db.Table<UserFleetGroupTag>().Where(t => t.UserFleetGroupId == groupId).ToListAsync();
    }

    /// <inheritdoc/>
    public async Task<int> ApplyTagAsync(UserFleetGroupTag tag)
    {
        var db = await GetConnectionAsync();
        return await db.InsertAsync(tag);
    }

    /// <inheritdoc/>
    public async Task RemoveTagAsync(int groupId, string tagKey)
    {
        var db = await GetConnectionAsync();
        var matches = await db.Table<UserFleetGroupTag>()
            .Where(t => t.UserFleetGroupId == groupId && t.TagKey == tagKey)
            .ToListAsync();
        foreach (var tag in matches)
            await db.DeleteAsync(tag);
    }

    /// <inheritdoc/>
    public async Task ReplaceTagsAsync(int groupId, IEnumerable<UserFleetGroupTag> tags)
    {
        var db = await GetConnectionAsync();

        // Delete existing tags for this group
        var existing = await db.Table<UserFleetGroupTag>()
            .Where(t => t.UserFleetGroupId == groupId)
            .ToListAsync();
        foreach (var tag in existing)
            await db.DeleteAsync(tag);

        // Insert new tags
        foreach (var tag in tags)
        {
            tag.UserFleetGroupId = groupId;
            await db.InsertAsync(tag);
        }
    }
}
