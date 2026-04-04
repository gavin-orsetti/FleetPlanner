using FleetPlanner.Models;

namespace FleetPlanner.Services;

/// <summary>
/// Abstraction for the on-device fleet recommendation engine.
/// <para>
/// The engine analyses a fleet's declared intent (focus, roles, crew budget, operating scale)
/// against the ships it already contains and the full ship catalogue, then produces actionable
/// <see cref="Recommendation"/> objects that suggest how to improve the fleet.
/// </para>
/// <para>
/// <b>Why an interface?</b> Same reason as every other service interface in this app:
/// <list type="bullet">
///   <item>ViewModels depend on the interface, not the implementation → loose coupling.</item>
///   <item>Unit tests can mock it with NSubstitute to test the ViewModel in isolation.</item>
///   <item>The recommendation algorithm can be swapped or A/B tested without touching ViewModels.</item>
/// </list>
/// </para>
/// </summary>
public interface IRecommendationService
{
    /// <summary>
    /// Generates recommendations for a single fleet.
    /// <para>
    /// This is a pure, synchronous computation — no I/O, no network calls, no database access.
    /// All data is passed in as parameters. This makes it fast, testable, and safe to call
    /// from the UI thread (though in practice it's called from a ViewModel's async command).
    /// </para>
    /// </summary>
    /// <param name="fleet">The fleet to analyse — provides focus, roles, crew budget, scale.</param>
    /// <param name="ownedShips">
    /// The <see cref="Ship"/> records for ships currently in the fleet. These are the global
    /// ship-reference records (with stats), not the <see cref="FleetShip"/> join entities.
    /// </param>
    /// <param name="allShips">The complete cached ship catalogue — candidates for suggestions.</param>
    /// <returns>
    /// A list of <see cref="Recommendation"/> objects sorted by priority (High → Low).
    /// May be empty if the fleet is already well-balanced.
    /// </returns>
    List<Recommendation> GetRecommendations(Fleet fleet, List<Ship> ownedShips, List<Ship> allShips);
}
