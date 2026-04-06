using FleetPlanner.Graph;
using FleetPlanner.Models;

namespace FleetPlanner.Services;

/// <summary>
/// Graph-driven recommendation engine that analyses the in-memory <see cref="FleetGraph"/>
/// and produces actionable <see cref="Recommendation"/> objects.
///
/// <para><b>Architecture:</b> Consumes a pre-built <see cref="FleetGraph"/> (no I/O of its own).
/// The graph must be built and optionally cached by <see cref="IGraphBuildService"/> before
/// calling <see cref="GetRecommendations"/>. The implementation (<see cref="RecommendationService"/>)
/// runs 10 analytical patterns that examine tag coverage, role distribution, doctrine alignment,
/// and crew efficiency.</para>
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
