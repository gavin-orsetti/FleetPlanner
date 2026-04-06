using FleetPlanner.Models;

namespace FleetPlanner.Repositories;

/// <summary>
/// Data access interface for <see cref="UserFleetGroup"/> records.
/// </summary>
public interface IUserFleetGroupRepository
{
    /// <summary>Returns all groups, optionally including archived ones.</summary>
    Task<List<UserFleetGroup>> GetAllGroupsAsync(bool includeArchived = false);

    /// <summary>Returns a single group by primary key, or null.</summary>
    Task<UserFleetGroup?> GetGroupAsync(int id);

    /// <summary>Upserts a group (insert or replace).</summary>
    Task<int> SaveGroupAsync(UserFleetGroup group);

    /// <summary>Soft-deletes by setting IsArchived = true.</summary>
    Task ArchiveGroupAsync(int id);

    /// <summary>Hard-deletes a group record.</summary>
    Task<int> DeleteGroupAsync(int id);
}
