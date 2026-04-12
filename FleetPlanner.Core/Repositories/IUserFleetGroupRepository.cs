using FleetPlanner.Models;

namespace FleetPlanner.Repositories;

/// <summary>
/// Data access interface for <see cref="UserFleetGroup"/> records.
/// User fleet groups are named collections that let players organise ships
/// into logical sets (e.g. "Mining Fleet", "PvP Wing"). Implementations
/// persist to the local SQLite database and support both soft-delete
/// (archive) and hard-delete semantics.
/// </summary>
public interface IUserFleetGroupRepository
{
    /// <summary>
    /// Returns all groups, ordered by <c>SortOrder</c> then <c>Name</c>.
    /// </summary>
    /// <param name="includeArchived">
    /// When <see langword="false"/> (default), rows where <c>IsArchived == true</c>
    /// are excluded. Pass <see langword="true"/> to return every row regardless
    /// of archive status -- useful for admin views and data exports.
    /// </param>
    Task<List<UserFleetGroup>> GetAllGroupsAsync(bool includeArchived = false);

    /// <summary>
    /// Returns a single group by primary key, or <see langword="null"/> if no
    /// row exists for <paramref name="id"/>. Archived groups are still returned.
    /// </summary>
    Task<UserFleetGroup?> GetGroupAsync(int id);

    /// <summary>
    /// Upserts a group. If <c>group.Id</c> is <c>0</c> the record is inserted
    /// and <c>CreatedUtc</c> is stamped; otherwise the existing row is updated.
    /// <c>UpdatedUtc</c> is always set to <see cref="DateTime.UtcNow"/>. Does not
    /// throw on conflict -- the <c>Id == 0</c> check is the sole insert-vs-update
    /// discriminator.
    /// </summary>
    /// <returns>The number of rows affected (always 1 on success).</returns>
    Task<int> SaveGroupAsync(UserFleetGroup group);

    /// <summary>
    /// Soft-deletes a group by setting <c>IsArchived = true</c> and stamping
    /// <c>UpdatedUtc</c>. The row remains in the database and can be restored or
    /// included via <see cref="GetAllGroupsAsync"/> with
    /// <c>includeArchived = true</c>. No-ops silently if the <paramref name="id"/>
    /// does not exist.
    /// </summary>
    Task ArchiveGroupAsync(int id);

    /// <summary>
    /// Hard-deletes a group record, permanently removing the row from the
    /// database. Prefer <see cref="ArchiveGroupAsync"/> for user-facing removal;
    /// use this only for data-cleanup or test teardown scenarios.
    /// </summary>
    /// <returns>The number of rows deleted (0 if the row was not found).</returns>
    Task<int> DeleteGroupAsync(int id);
}
