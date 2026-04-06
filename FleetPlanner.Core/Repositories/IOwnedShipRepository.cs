using FleetPlanner.Models;

namespace FleetPlanner.Repositories;

/// <summary>
/// Data access interface for <see cref="OwnedShip"/> records.
/// This is the user-data layer contract for ships the player has added to their
/// personal fleet. Implementations persist to the local SQLite database and
/// support both soft-delete (archive) and hard-delete semantics.
/// </summary>
public interface IOwnedShipRepository
{
    /// <summary>
    /// Returns all owned ships, ordered by <c>CreatedUtc</c> descending.
    /// </summary>
    /// <param name="includeArchived">
    /// When <see langword="false"/> (default), rows where <c>IsArchived == true</c>
    /// are filtered out. Pass <see langword="true"/> to return every row regardless
    /// of archive status -- useful for admin views and data exports.
    /// </param>
    Task<List<OwnedShip>> GetAllOwnedShipsAsync(bool includeArchived = false);

    /// <summary>
    /// Returns a single owned ship by primary key, or <see langword="null"/> if no
    /// row exists for <paramref name="id"/>. Archived ships are still returned.
    /// </summary>
    Task<OwnedShip?> GetOwnedShipAsync(int id);

    /// <summary>
    /// Upserts an owned ship. If <c>ship.Id</c> is <c>0</c> the record is inserted
    /// and <c>CreatedUtc</c> is stamped; otherwise the existing row is updated.
    /// <c>UpdatedUtc</c> is always set to <see cref="DateTime.UtcNow"/>. Does not
    /// throw on conflict -- the <c>Id == 0</c> check is the sole insert-vs-update
    /// discriminator.
    /// </summary>
    /// <returns>The number of rows affected (always 1 on success).</returns>
    Task<int> SaveOwnedShipAsync(OwnedShip ship);

    /// <summary>
    /// Soft-deletes an owned ship by setting <c>IsArchived = true</c> and stamping
    /// <c>UpdatedUtc</c>. The row remains in the database and can be restored or
    /// included via <see cref="GetAllOwnedShipsAsync"/> with
    /// <c>includeArchived = true</c>. No-ops silently if the <paramref name="id"/>
    /// does not exist.
    /// </summary>
    Task ArchiveOwnedShipAsync(int id);

    /// <summary>
    /// Hard-deletes an owned ship record, permanently removing the row from the
    /// database. Prefer <see cref="ArchiveOwnedShipAsync"/> for user-facing
    /// removal; use this only for data-cleanup or test teardown scenarios.
    /// </summary>
    /// <returns>The number of rows deleted (0 if the row was not found).</returns>
    Task<int> DeleteOwnedShipAsync(int id);
}
