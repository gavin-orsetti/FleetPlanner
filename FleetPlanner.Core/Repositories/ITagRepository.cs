using FleetPlanner.Models;

namespace FleetPlanner.Repositories;

/// <summary>
/// Data access interface for <see cref="TagDefinition"/> records.
/// Tag definitions are the system-wide catalogue of tags that can be assigned
/// to ships and groups. They are keyed by a unique string <c>Key</c> (the
/// primary key) and scoped by <c>AllowedScopes</c> to control which entity
/// types may reference them. Implementations use upsert (insert-or-replace)
/// semantics in <see cref="SaveTagAsync"/>, so saving a tag whose key already
/// exists overwrites the row rather than throwing. To retire a system tag,
/// call <see cref="ArchiveTagAsync"/> (soft-delete) rather than hard-deleting
/// it, so that historical junction records remain valid.
/// </summary>
public interface ITagRepository
{
    /// <summary>
    /// Returns all tag definitions, ordered by <c>Category</c> then <c>SortOrder</c>.
    /// </summary>
    /// <param name="includeArchived">
    /// When <see langword="false"/> (default), rows where <c>IsArchived == true</c>
    /// are excluded. Pass <see langword="true"/> to include retired tags -- useful
    /// for admin screens or audit exports.
    /// </param>
    Task<List<TagDefinition>> GetAllTagsAsync(bool includeArchived = false);

    /// <summary>
    /// Returns all non-archived tag definitions in the given <paramref name="category"/>,
    /// ordered by <c>SortOrder</c>.
    /// </summary>
    Task<List<TagDefinition>> GetTagsByCategoryAsync(string category);

    /// <summary>
    /// Returns non-archived tags whose <c>AllowedScopes</c> comma-separated list
    /// contains <paramref name="scope"/> (e.g. <c>"OwnedShip"</c> or
    /// <c>"UserFleetGroup"</c>), ordered by <c>Category</c> then <c>SortOrder</c>.
    /// The contains check is performed in-memory after fetching all non-archived rows.
    /// </summary>
    Task<List<TagDefinition>> GetAssignableTagsForScopeAsync(string scope);

    /// <summary>
    /// Returns a single tag definition by its string primary key, or
    /// <see langword="null"/> if no row matches. Archived tags are still returned.
    /// </summary>
    Task<TagDefinition?> GetTagAsync(string key);

    /// <summary>
    /// Upserts a tag definition using insert-or-replace semantics. If a row with
    /// the same <c>Key</c> already exists it is fully overwritten; otherwise a new
    /// row is inserted. Does not throw on conflict.
    /// </summary>
    /// <returns>The number of rows affected (always 1 on success).</returns>
    Task<int> SaveTagAsync(TagDefinition tag);

    /// <summary>
    /// Soft-deletes a tag definition by setting <c>IsArchived = true</c>. The row
    /// remains in the database so that existing junction records
    /// (<see cref="OwnedShipTag"/>, <see cref="UserFleetGroupTag"/>) that
    /// reference this key stay valid. This is the correct removal path for
    /// system tags; prefer this over hard-deleting. No-ops silently if the
    /// <paramref name="key"/> does not exist.
    /// </summary>
    Task ArchiveTagAsync(string key);
}
