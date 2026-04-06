using FleetPlanner.Models;

namespace FleetPlanner.Repositories;

/// <summary>
/// Data access interface for <see cref="OwnedShip"/> records.
/// </summary>
public interface IOwnedShipRepository
{
    /// <summary>Returns all owned ships, optionally including archived ones.</summary>
    Task<List<OwnedShip>> GetAllOwnedShipsAsync(bool includeArchived = false);

    /// <summary>Returns a single owned ship by primary key, or null.</summary>
    Task<OwnedShip?> GetOwnedShipAsync(int id);

    /// <summary>Upserts an owned ship (insert or replace).</summary>
    Task<int> SaveOwnedShipAsync(OwnedShip ship);

    /// <summary>Soft-deletes by setting IsArchived = true.</summary>
    Task ArchiveOwnedShipAsync(int id);

    /// <summary>Hard-deletes an owned ship record.</summary>
    Task<int> DeleteOwnedShipAsync(int id);
}
