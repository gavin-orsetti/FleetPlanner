using FleetPlanner.Models;

namespace FleetPlanner.Graph;

/// <summary>
/// In-memory graph node representing a tag assignment with its definition,
/// weight, and context information.
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
