using FleetPlanner.Models;

namespace FleetPlanner.Services;

public interface IRecommendationService
{
    List<Recommendation> GetRecommendations(List<Ship> ownedShips, List<Ship> allShips);
}
