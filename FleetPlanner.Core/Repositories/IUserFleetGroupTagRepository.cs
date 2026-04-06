using FleetPlanner.Models;

namespace FleetPlanner.Repositories;

/// <summary>
/// Data access interface for <see cref="UserFleetGroupTag"/> junction records.
/// </summary>
public interface IUserFleetGroupTagRepository
{
    /// <summary>Returns all tags for a specific group.</summary>
    Task<List<UserFleetGroupTag>> GetTagsForGroupAsync(int groupId);

    /// <summary>Inserts a new tag assignment for a group.</summary>
    Task<int> ApplyTagAsync(UserFleetGroupTag tag);

    /// <summary>Removes a specific tag from a group.</summary>
    Task RemoveTagAsync(int groupId, string tagKey);

    /// <summary>Replaces all tags on a group (deletes existing, inserts new set).</summary>
    Task ReplaceTagsAsync(int groupId, IEnumerable<UserFleetGroupTag> tags);
}
