namespace FleetPlanner.Graph;

/// <summary>
/// The complete in-memory graph of the user's fleet, built by <see cref="FleetPlanner.Services.GraphBuildService"/>
/// from SQLite data. This is the primary data structure consumed by
/// <see cref="FleetPlanner.Services.IRecommendationService"/> for fleet analysis.
///
/// <para><b>Lifecycle:</b> Built on demand via <see cref="FleetPlanner.Services.IGraphBuildService.GetOrRebuildAsync"/>,
/// cached in memory, and invalidated when the underlying data changes. The graph is ephemeral
/// — it is never persisted, only rebuilt from the authoritative SQLite data.</para>
///
/// <para><b>Invariants:</b> After a successful build, <see cref="Ships"/> contains only ships
/// whose <c>ShipId</c> matches a catalogue entry (orphaned OwnedShips are silently skipped).
/// <see cref="Groups"/> contains only non-archived groups. <see cref="AllTags"/> is a flat
/// list of every tag node referenced anywhere in the graph (may contain duplicates if the same
/// tag definition appears on multiple ships/groups with different weights or contexts).</para>
/// </summary>
public class FleetGraph
{
    /// <summary>All owned ship nodes with their resolved tags.</summary>
    public List<ShipNode> Ships { get; set; } = new();

    /// <summary>All fleet group nodes with their doctrine tags and member ships.</summary>
    public List<GroupNode> Groups { get; set; } = new();

    /// <summary>All unique tag nodes referenced in the graph.</summary>
    public List<TagNode> AllTags { get; set; } = new();

    /// <summary>UTC timestamp of when this graph was built.</summary>
    public DateTime BuiltAt { get; set; }
}
