using FleetPlanner.Models;

namespace FleetPlanner.Services;

public interface IRecommendationService
{
    List<Recommendation> GetRecommendations(Fleet fleet, List<Ship> ownedShips, List<Ship> allShips);
}
