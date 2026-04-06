using FleetPlanner.Models;

namespace FleetPlanner.Graph;

/// <summary>
/// In-memory graph node representing a single owned ship, with resolved catalogue
/// data and attached tag nodes split by context (global vs. group-scoped).
/// </summary>
public class ShipNode
{
    /// <summary>The owned ship record Id.</summary>
    public int OwnedShipId { get; set; }

    /// <summary>The user-owned ship record.</summary>
    public OwnedShip OwnedShip { get; set; } = null!;

    /// <summary>The resolved catalogue ship reference.</summary>
    public Ship CatalogueShip { get; set; } = null!;

    /// <summary>Tags applied globally to this ship (not scoped to any group).</summary>
    public List<TagNode> GlobalTags { get; set; } = new();

    /// <summary>Tags scoped to specific groups. Key = UserFleetGroup.Id, Value = tags in that context.</summary>
    public Dictionary<int, List<TagNode>> ContextualTags { get; set; } = new();
}
