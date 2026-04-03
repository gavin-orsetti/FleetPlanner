using FleetPlanner.Models;

namespace FleetPlanner.Repositories;

public interface IFleetRepository
{
    Task InitializeAsync();
    Task<List<Fleet>> GetAllFleetsAsync();
    Task<Fleet?> GetFleetAsync(int id);
    Task<int> SaveFleetAsync(Fleet fleet);
    Task<int> DeleteFleetAsync(int id);

    Task<List<FleetShip>> GetFleetShipsAsync(int fleetId);
    Task<FleetShip?> GetFleetShipAsync(int id);
    Task<int> SaveFleetShipAsync(FleetShip fleetShip);
    Task<int> DeleteFleetShipAsync(int id);
    Task DeleteFleetShipsByFleetAsync(int fleetId);
}
