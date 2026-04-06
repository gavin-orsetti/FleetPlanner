using SQLite;

namespace FleetPlanner.Models;

/// <summary>
/// A controlled tag definition in the taxonomy. Tags are the primary way users describe
/// their ships and groups. System-defined tags are seeded at first launch and cannot be deleted.
/// </summary>
[Table("TagDefinitions")]
public class TagDefinition
{
    /// <summary>
    /// Stable unique key. NEVER changes once shipped.
    /// Format: "category:slug" e.g. "role:escort", "doctrine:industrial".
    /// </summary>
    [PrimaryKey]
    public string Key { get; set; } = string.Empty;

    /// <summary>Human-readable display name for the tag.</summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>
    /// Category prefix: role, doctrine, status, crew, preference, capability, constraint, custom.
    /// </summary>
    public string Category { get; set; } = string.Empty;

    /// <summary>Explanation of what this tag means.</summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>Hex color for UI chip display e.g. "#00d4ff".</summary>
    public string ColorHex { get; set; } = "#8890a8";

    /// <summary>Sort order within category for UI display.</summary>
    public int SortOrder { get; set; } = 0;

    /// <summary>System tags are seeded and cannot be deleted, only archived.</summary>
    public bool IsSystemDefined { get; set; } = false;

    /// <summary>Whether users can edit the display name of this tag.</summary>
    public bool IsUserEditable { get; set; } = true;

    /// <summary>Archived tags are hidden from pickers but still valid on existing records.</summary>
    public bool IsArchived { get; set; } = false;

    /// <summary>
    /// Comma-separated scope values: "OwnedShip", "UserFleetGroup", "OwnedShip,UserFleetGroup".
    /// </summary>
    public string AllowedScopes { get; set; } = "OwnedShip";

    /// <summary>Optional parent tag key for hierarchical browsing.</summary>
    public string? ParentKey { get; set; }
}
