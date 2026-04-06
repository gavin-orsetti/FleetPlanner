using FleetPlanner.Models;

namespace FleetPlanner.Graph;

/// <summary>
/// In-memory graph node representing a resolved tag assignment — combines the
/// <see cref="TagDefinition"/> with the assignment-specific weight and context.
///
/// <para><b>Weight semantics:</b> <see cref="Weight"/> comes from <see cref="FleetPlanner.Models.OwnedShipTag.Weight"/>
/// or <see cref="FleetPlanner.Models.UserFleetGroupTag.Weight"/>: 1 = primary, 2 = secondary,
/// 3 = tertiary. The recommendation engine uses weight to identify a ship's primary role or
/// doctrine — only weight-1 tags drive mismatch and redundancy analysis.</para>
///
/// <para><b>Context:</b> <see cref="IsContextual"/> is true if this tag was scoped to a specific
/// group (from an <see cref="FleetPlanner.Models.OwnedShipTag"/> with ContextType == "group").
/// <see cref="ContextGroupId"/> holds the group Id in that case.</para>
/// </summary>
public class TagNode
{
    /// <summary>The resolved tag definition.</summary>
    public TagDefinition Definition { get; set; } = null!;

    /// <summary>Priority weight of this tag assignment (1 = primary).</summary>
    public int Weight { get; set; }

    /// <summary>True if this tag is scoped to a specific group context.</summary>
    public bool IsContextual { get; set; }

    /// <summary>The group Id if contextual, null if global.</summary>
    public int? ContextGroupId { get; set; }
}
