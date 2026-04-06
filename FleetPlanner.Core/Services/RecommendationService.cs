using FleetPlanner.Models;

namespace FleetPlanner.Services;

/// <summary>
/// Stub recommendation engine. Will be fully rewritten in Phase 3 with graph-driven analysis
/// implementing all 10 analytical patterns.
/// </summary>
public class RecommendationService : IRecommendationService
{
    /// <inheritdoc/>
    public List<Recommendation> GetRecommendations()
    {
        // Phase 3: This will accept a FleetGraph and run 10 analytical patterns.
        return [];
    }
}
