using FleetPlanner.Models;

namespace FleetPlanner.Services;

public interface IShipDataService
{
    Task<List<Ship>> GetAllShipsAsync(bool forceRefresh = false);
    Task<Ship?> GetShipAsync(int id);
    Task<DateTime?> GetLastUpdatedAsync();
}
