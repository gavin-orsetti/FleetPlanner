using FleetPlanner.Models;

namespace FleetPlanner.Repositories;

/// <summary>
/// Abstraction over the local SQLite database for fleet and fleet-ship CRUD operations.
/// <para>
/// <b>Why an interface?</b> The Repository Pattern decouples business logic (ViewModels, Services)
/// from the data-access implementation. This means:
/// <list type="bullet">
///   <item>ViewModels never touch SQLite directly — they depend on <c>IFleetRepository</c>.</item>
///   <item>Unit tests can substitute a mock/fake implementation (via NSubstitute) without a real database.</item>
///   <item>The storage backend could be swapped (e.g., to a cloud API) without changing any ViewModel code.</item>
/// </list>
/// </para>
/// </summary>
/// <see href="https://learn.microsoft.com/en-us/dotnet/architecture/microservices/microservice-ddd-cqrs-patterns/infrastructure-persistence-layer-design"/>
/// <see href="https://learn.microsoft.com/en-us/dotnet/maui/fundamentals/dependency-injection"/>
public interface IFleetRepository
{
    /// <summary>
    /// Creates tables if they don't exist and seeds default data.
    /// Must be called once at app startup before any other repository method.
    /// </summary>
    Task InitializeAsync();

    /// <summary>Returns all fleets ordered by name.</summary>
    Task<List<Fleet>> GetAllFleetsAsync();

    /// <summary>Returns a single fleet by primary key, or <see langword="null"/> if not found.</summary>
    /// <param name="id">The <see cref="Fleet.Id"/> to look up.</param>
    Task<Fleet?> GetFleetAsync(int id);

    /// <summary>
    /// Inserts or updates a fleet. If <see cref="Fleet.Id"/> is 0 (new), inserts; otherwise updates.
    /// </summary>
    /// <param name="fleet">The fleet entity to persist.</param>
    /// <returns>The number of rows affected (1 on success).</returns>
    Task<int> SaveFleetAsync(Fleet fleet);

    /// <summary>
    /// Deletes a fleet AND all of its associated <see cref="FleetShip"/> rows (cascade).
    /// </summary>
    /// <param name="id">The <see cref="Fleet.Id"/> to delete.</param>
    /// <returns>The number of rows affected.</returns>
    Task<int> DeleteFleetAsync(int id);

    /// <summary>Returns all ships belonging to the specified fleet.</summary>
    /// <param name="fleetId">The <see cref="Fleet.Id"/> to filter by.</param>
    Task<List<FleetShip>> GetFleetShipsAsync(int fleetId);

    /// <summary>Returns a single fleet-ship by primary key, or <see langword="null"/> if not found.</summary>
    /// <param name="id">The <see cref="FleetShip.Id"/> to look up.</param>
    Task<FleetShip?> GetFleetShipAsync(int id);

    /// <summary>
    /// Inserts or updates a fleet-ship. If <see cref="FleetShip.Id"/> is 0, inserts; otherwise updates.
    /// </summary>
    /// <param name="fleetShip">The fleet-ship entity to persist.</param>
    /// <returns>The number of rows affected.</returns>
    Task<int> SaveFleetShipAsync(FleetShip fleetShip);

    /// <summary>Deletes a single fleet-ship by primary key.</summary>
    /// <param name="id">The <see cref="FleetShip.Id"/> to delete.</param>
    /// <returns>The number of rows affected.</returns>
    Task<int> DeleteFleetShipAsync(int id);

    /// <summary>
    /// Deletes ALL fleet-ships belonging to a fleet. Called as part of fleet deletion cascade.
    /// </summary>
    /// <param name="fleetId">The <see cref="Fleet.Id"/> whose ships should be removed.</param>
    Task DeleteFleetShipsByFleetAsync(int fleetId);

    /// <summary>
    /// Ensures at least one fleet exists. If the database is empty, creates a default
    /// "My Fleet" and reassigns any orphaned <see cref="FleetShip"/> rows to it.
    /// </summary>
    Task EnsureDefaultFleetAsync();
}
