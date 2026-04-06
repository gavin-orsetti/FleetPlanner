using FleetPlanner.Models;

namespace FleetPlanner.Repositories;

/// <summary>
/// Data access interface for <see cref="OwnedShipTag"/> junction records.
/// This is the many-to-many junction table linking <see cref="OwnedShip"/>
/// rows to <see cref="TagDefinition"/> keys. Each assignment can be either
/// <em>global</em> (<c>ContextType == null</c>) or <em>contextual</em>
/// (e.g. <c>ContextType == "group"</c> with a <c>ContextId</c>).
/// <see cref="ReplaceTagsAsync"/> only touches global assignments; contextual
/// tags scoped to a specific group are left untouched. All removal operations
/// are hard-deletes -- junction rows have no archive flag.
/// </summary>
public interface IOwnedShipTagRepository
{
    /// <summary>
    /// Returns tag assignments for an owned ship. When both
    /// <paramref name="contextType"/> and <paramref name="contextId"/> are
    /// <see langword="null"/> (the default), all assignments for the ship are
    /// returned (global and contextual). Supply a context pair such as
    /// <c>("group", groupId)</c> to filter to that context only.
    /// </summary>
    Task<List<OwnedShipTag>> GetTagsForOwnedShipAsync(int ownedShipId, string? contextType = null, int? contextId = null);

    /// <summary>
    /// Returns every <see cref="OwnedShipTag"/> row that references
    /// <paramref name="tagKey"/>, across all ships and contexts.
    /// Useful for impact analysis before retiring a tag.
    /// </summary>
    Task<List<OwnedShipTag>> GetOwnedShipsForTagAsync(string tagKey);

    /// <summary>
    /// Inserts a new tag assignment. Always inserts; does not upsert or check for
    /// duplicates. <c>CreatedUtc</c> is stamped automatically.
    /// </summary>
    /// <returns>The number of rows inserted (always 1 on success).</returns>
    Task<int> ApplyTagAsync(OwnedShipTag assignment);

    /// <summary>
    /// Hard-deletes the junction row(s) matching the given owned ship, tag key,
    /// and context triple. This is a permanent removal; there is no soft-delete
    /// on junction records.
    /// </summary>
    Task RemoveTagAsync(int ownedShipId, string tagKey, string? contextType, int? contextId);

    /// <summary>
    /// Replaces all <em>global</em> tag assignments on a ship. Existing rows
    /// where <c>ContextType == null</c> are hard-deleted, then the supplied
    /// <paramref name="tags"/> are inserted with <c>CreatedUtc</c> stamped.
    /// Contextual tags (those with a non-null <c>ContextType</c>) are not
    /// affected by this operation.
    /// </summary>
    Task ReplaceTagsAsync(int ownedShipId, IEnumerable<OwnedShipTag> tags);
}
