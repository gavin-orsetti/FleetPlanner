using FleetPlanner.Graph;
using FleetPlanner.Models;

namespace FleetPlanner.Services;

/// <summary>
/// Graph-driven recommendation engine that analyses the fleet graph
/// and produces actionable <see cref="Recommendation"/> objects.
/// </summary>
public interface IRecommendationService
{
    /// <summary>
    /// Generates recommendations from a fully-built fleet graph.
    /// Runs all 10 analytical patterns and returns results sorted by score descending.
    /// </summary>
    /// <param name="graph">The in-memory fleet graph built by <see cref="IGraphBuildService"/>.</param>
    /// <returns>Sorted list of recommendations.</returns>
    List<Recommendation> GetRecommendations(FleetGraph graph);
}
