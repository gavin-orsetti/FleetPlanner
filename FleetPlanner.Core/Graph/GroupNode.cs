using FleetPlanner.Models;

namespace FleetPlanner.Graph;

/// <summary>
/// In-memory graph node representing a fleet group with its doctrine/focus tags
/// and the ships assigned to it via contextual <see cref="FleetPlanner.Models.OwnedShipTag"/> records.
///
/// <para><b>Membership model:</b> <see cref="MemberShips"/> is populated during graph build by
/// finding all <see cref="ShipNode"/> instances that have contextual tags for this group's Id.
/// A ship is a member if it has ANY contextual tag for this group — there is no separate
/// membership table.</para>
///
/// <para><b>Doctrine tags:</b> <see cref="DoctrineAndFocusTags"/> holds the group-level tags
/// (typically doctrine and constraint categories) that describe the group's intended purpose.
/// These drive capability gap analysis and coherence scoring in the recommendation engine.</para>
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
