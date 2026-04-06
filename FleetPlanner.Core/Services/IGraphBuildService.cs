using FleetPlanner.Graph;

namespace FleetPlanner.Services;

/// <summary>
/// Builds and caches the in-memory <see cref="FleetGraph"/> from SQLite data.
/// The graph is the central data structure consumed by <see cref="IRecommendationService"/>
/// and any future analytics that need a resolved, relationship-rich view of the fleet.
///
/// <para><b>Cache contract:</b> The graph is cached in memory after the first build.
/// <see cref="GetOrRebuildAsync"/> returns the cached instance if available, avoiding
/// redundant database round-trips. <see cref="InvalidateCache"/> must be called after
/// any mutation to the underlying data (ship CRUD, tag changes, group changes) to ensure
/// the next <c>GetOrRebuildAsync</c> call rebuilds from fresh data.</para>
///
/// <para><b>Thread safety:</b> The current implementation is NOT thread-safe. Concurrent
/// calls to <see cref="BuildGraphAsync"/> could produce inconsistent state. In MAUI this
/// is acceptable because all ViewModel calls happen on the UI thread, but callers moving
/// to background threads must synchronise externally.</para>
/// </summary>
public interface IGraphBuildService
{
    /// <summary>
    /// Builds a fresh <see cref="FleetGraph"/> by loading all data from SQLite and resolving
    /// relationships. Always hits the database — ignores and replaces any cached graph.
    /// </summary>
    /// <returns>A fully resolved graph with ships, groups, and tags.</returns>
    Task<FleetGraph> BuildGraphAsync();

    /// <summary>
    /// Returns the cached graph if one exists, otherwise builds a fresh one.
    /// Prefer this over <see cref="BuildGraphAsync"/> in normal usage — it avoids
    /// redundant database reads when the data hasn't changed.
    /// </summary>
    /// <returns>The cached or freshly-built <see cref="FleetGraph"/>.</returns>
    Task<FleetGraph> GetOrRebuildAsync();

    /// <summary>
    /// Marks the cached graph as stale, forcing the next <see cref="GetOrRebuildAsync"/>
    /// call to rebuild from the database. Must be called after any write operation that
    /// changes owned ships, tags, or groups.
    /// </summary>
    void InvalidateCache();
}
