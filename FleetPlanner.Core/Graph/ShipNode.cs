using FleetPlanner.Models;

namespace FleetPlanner.Graph;

/// <summary>
/// In-memory graph node representing a single owned ship, with resolved catalogue
/// data and attached tag nodes split by context (global vs. group-scoped).
///
/// <para><b>Tag partitioning:</b> Tags are split into two buckets:
/// <list type="bullet">
///   <item><see cref="GlobalTags"/> — tags with <c>ContextType == null</c>, applied to the ship
///     everywhere. Used by account-level patterns (AccountRoleDistribution, UnderDescribedShip).</item>
///   <item><see cref="ContextualTags"/> — tags scoped to a specific group (<c>ContextType == "group"</c>).
///     Keyed by <c>UserFleetGroup.Id</c>. The presence of ANY contextual tags for a group
///     makes this ship a member of that group.</item>
/// </list></para>
///
/// <para><b>Invariant:</b> <see cref="CatalogueShip"/> is always non-null after graph build —
/// ships with no catalogue match are excluded during construction.</para>
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
