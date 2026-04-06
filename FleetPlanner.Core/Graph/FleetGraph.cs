namespace FleetPlanner.Graph;

/// <summary>
/// The complete in-memory graph of the user's fleet. Built from SQLite data,
/// cached in memory, and invalidated after mutations.
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
