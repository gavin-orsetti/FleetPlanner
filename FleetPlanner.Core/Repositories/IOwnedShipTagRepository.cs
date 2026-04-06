using FleetPlanner.Models;

namespace FleetPlanner.Repositories;

/// <summary>
/// Data access interface for <see cref="OwnedShipTag"/> junction records.
/// </summary>
public interface IOwnedShipTagRepository
{
    /// <summary>
    /// Returns tags for a specific owned ship, optionally filtered by context.
    /// Pass null contextType for global-only tags, or specify "group" + contextId for group-scoped.
    /// </summary>
    Task<List<OwnedShipTag>> GetTagsForOwnedShipAsync(int ownedShipId, string? contextType = null, int? contextId = null);

    /// <summary>Returns all OwnedShipTag records that reference the given tag key.</summary>
    Task<List<OwnedShipTag>> GetOwnedShipsForTagAsync(string tagKey);

    /// <summary>Inserts a new tag assignment.</summary>
    Task<int> ApplyTagAsync(OwnedShipTag assignment);

    /// <summary>Removes a specific tag assignment by owned ship, tag key, and context.</summary>
    Task RemoveTagAsync(int ownedShipId, string tagKey, string? contextType, int? contextId);

    /// <summary>Replaces all global tags on a ship (deletes existing globals, inserts new set).</summary>
    Task ReplaceTagsAsync(int ownedShipId, IEnumerable<OwnedShipTag> tags);
}
