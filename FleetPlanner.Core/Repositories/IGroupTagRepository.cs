using FleetPlanner.Models;

namespace FleetPlanner.Repositories;

/// <summary>
/// Data access interface for <see cref="GroupTagDefinition"/> records.
/// Group tag definitions are the system-wide catalogue of tags that can be assigned
/// to fleet groups. They are keyed by a unique string <c>Key</c> (the primary key)
/// and scoped exclusively to <c>"UserFleetGroup"</c>. This is a completely separate
/// table from <see cref="ITagRepository"/> which manages ship-level tags.
/// Implementations use upsert (insert-or-replace) semantics in <see cref="SaveTagAsync"/>.
/// </summary>
public interface IGroupTagRepository
{
    /// <summary>
    /// Returns all group tag definitions, ordered by <c>Category</c> then <c>SortOrder</c>.
    /// </summary>
    /// <param name="includeArchived">
    /// When <see langword="false"/> (default), rows where <c>IsArchived == true</c>
    /// are excluded. Pass <see langword="true"/> to include retired tags.
    /// </param>
    Task<List<GroupTagDefinition>> GetAllTagsAsync(bool includeArchived = false);

    /// <summary>
    /// Returns a single group tag definition by its string primary key, or
    /// <see langword="null"/> if no row matches. Archived tags are still returned.
    /// </summary>
    Task<GroupTagDefinition?> GetTagByKeyAsync(string key);

    /// <summary>
    /// Upserts a group tag definition using insert-or-replace semantics. If a row with
    /// the same <c>Key</c> already exists it is fully overwritten; otherwise a new
    /// row is inserted.
    /// </summary>
    Task SaveTagAsync(GroupTagDefinition tag);

    /// <summary>
    /// Soft-deletes a group tag definition by setting <c>IsArchived = true</c>. The row
    /// remains in the database so that existing junction records that reference this key
    /// stay valid. No-ops silently if the <paramref name="key"/> does not exist.
    /// </summary>
    Task ArchiveTagAsync(string key);

    /// <summary>
    /// Hard-deletes a group tag definition. Only intended for user-defined tags
    /// (<c>IsSystemDefined == false</c>). System tags should be archived, not deleted.
    /// No-ops silently if the <paramref name="key"/> does not exist.
    /// </summary>
    Task DeleteTagAsync(string key);
}
