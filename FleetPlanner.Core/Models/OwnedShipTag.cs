using SQLite;

namespace FleetPlanner.Models;

/// <summary>
/// A tag assignment linking an <see cref="OwnedShip"/> to a <see cref="TagDefinition"/>.
/// Tags can be global (applied to the ship everywhere) or contextual (scoped to a specific group).
/// </summary>
[Table("OwnedShipTags")]
public class OwnedShipTag
{
    /// <summary>Auto-incremented primary key.</summary>
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    /// <summary>FK to <see cref="OwnedShip.Id"/>.</summary>
    public int OwnedShipId { get; set; }

    /// <summary>FK to <see cref="TagDefinition.Key"/>.</summary>
    public string TagKey { get; set; } = string.Empty;

    /// <summary>
    /// Optional context type for scoped tags. Use "group" when the tag
    /// applies to this ship only within a specific group context.
    /// Null = global tag applied to the ship across all contexts.
    /// </summary>
    public string? ContextType { get; set; }

    /// <summary>
    /// Optional context ID. When ContextType = "group", this is the UserFleetGroup.Id.
    /// </summary>
    public int? ContextId { get; set; }

    /// <summary>
    /// Whether this tag was applied by the system (e.g. auto-tagged)
    /// or by the user manually.
    /// </summary>
    public bool AppliedBySystem { get; set; } = false;

    /// <summary>
    /// Tag priority within its category in this context.
    /// 1 = primary, 2 = secondary, 3 = tertiary, etc.
    /// Used for intent hierarchy: a ship can have intent:activity:fight (weight 1) and
    /// intent:activity:escort (weight 2) — primary intent takes precedence in recommendations.
    /// Conflicting tags at the same weight lower recommendation confidence.
    /// </summary>
    public int Weight { get; set; } = 1;

    /// <summary>UTC timestamp of when this tag was applied.</summary>
    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
}
