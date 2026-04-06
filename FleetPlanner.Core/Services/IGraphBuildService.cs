using FleetPlanner.Graph;

namespace FleetPlanner.Services;

/// <summary>
/// Builds and caches the in-memory <see cref="FleetGraph"/> from SQLite data.
/// </summary>
public interface IGraphBuildService
{
    /// <summary>Builds a fresh graph from all current data.</summary>
    Task<FleetGraph> BuildGraphAsync();

    /// <summary>Returns the cached graph, rebuilding if stale or invalidated.</summary>
    Task<FleetGraph> GetOrRebuildAsync();

    /// <summary>Invalidates the cached graph, forcing a rebuild on next access.</summary>
    void InvalidateCache();
}
