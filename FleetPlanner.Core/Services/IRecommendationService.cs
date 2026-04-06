using FleetPlanner.Models;

namespace FleetPlanner.Services;

/// <summary>
/// Abstraction for the on-device recommendation engine.
/// Will be fully rewritten in Phase 3 with graph-driven analysis.
/// </summary>
public interface IRecommendationService
{
    /// <summary>
    /// Generates recommendations based on the fleet graph.
    /// Phase 3 stub — currently returns empty list.
    /// </summary>
    List<Recommendation> GetRecommendations();
}
