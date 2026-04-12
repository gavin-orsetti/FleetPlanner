using FleetPlanner.Models;

namespace FleetPlanner.Repositories;

/// <summary>
/// Data access interface for <see cref="UserFleetGroupTag"/> junction records.
/// This is the many-to-many junction table linking <see cref="UserFleetGroup"/>
/// rows to <see cref="GroupTagDefinition"/> keys. Unlike the ship-tag junction,
/// group tags have no context dimension -- every assignment is global to the
/// group. All removal operations are hard-deletes; junction rows carry no
/// archive flag.
/// </summary>
public interface IUserFleetGroupTagRepository
{
    /// <summary>
    /// Returns every tag assignment for the specified group.
    /// </summary>
    Task<List<UserFleetGroupTag>> GetTagsForGroupAsync(int groupId);

    /// <summary>
    /// Inserts a new tag assignment for a group. Always inserts; does not upsert
    /// or check for duplicates.
    /// </summary>
    /// <returns>The number of rows inserted (always 1 on success).</returns>
    Task<int> ApplyTagAsync(UserFleetGroupTag tag);

    /// <summary>
    /// Hard-deletes the junction row(s) matching the given group and tag key.
    /// This is a permanent removal; there is no soft-delete on junction records.
    /// </summary>
    Task RemoveTagAsync(int groupId, string tagKey);

    /// <summary>
    /// Replaces all tag assignments on a group. Every existing row for the group
    /// is hard-deleted, then the supplied <paramref name="tags"/> are inserted.
    /// This is an atomic swap of the full tag set.
    /// </summary>
    Task ReplaceTagsAsync(int groupId, IEnumerable<UserFleetGroupTag> tags);

    /// <summary>
    /// Hard-deletes all tag assignments for the specified group.
    /// Used to clean up when a group is deleted.
    /// </summary>
    Task DeleteAllTagsForGroupAsync(int groupId);
}
