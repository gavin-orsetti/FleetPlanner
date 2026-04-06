using FleetPlanner.Models;

namespace FleetPlanner.Graph;

/// <summary>
/// In-memory graph node representing a fleet group with its doctrine/focus tags
/// and the ships assigned to it.
/// </summary>
public class GroupNode
{
    /// <summary>The user-created fleet group record.</summary>
    public UserFleetGroup Group { get; set; } = null!;

    /// <summary>Doctrine and focus tags applied to this group.</summary>
    public List<TagNode> DoctrineAndFocusTags { get; set; } = new();

    /// <summary>Ships that are members of this group (via contextual OwnedShipTags).</summary>
    public List<ShipNode> MemberShips { get; set; } = new();
}
