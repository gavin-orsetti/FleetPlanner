using SQLite;

namespace FleetPlanner.Models;

/// <summary>
/// A controlled tag definition in the taxonomy. Tags are the primary mechanism by
/// which users describe their ships (<see cref="OwnedShipTag"/>) and groups
/// (<see cref="UserFleetGroupTag"/>), replacing traditional relational columns
/// with a flexible, extensible labelling system.
///
/// <para><strong>Key format:</strong> <see cref="Key"/> uses the pattern
/// <c>"category:slug"</c> (e.g. <c>"doctrine:value:backbone"</c>,
/// <c>"intent:activity:fight"</c>). Keys are the primary key and
/// <strong>must never change once shipped</strong> -- they serve as stable
/// identifiers across schema migrations, are human-readable in diagnostics, and
/// avoid auto-increment collision issues that integer IDs would introduce when
/// merging seed data with user-created tags.</para>
///
/// <para><strong>5-pillar taxonomy (86 ship tags across 18 sub-dimensions + custom):</strong>
/// <c>doctrine:value</c>, <c>doctrine:frequency</c>, <c>doctrine:investment</c>,
/// <c>doctrine:identity</c>, <c>doctrine:structural</c>,
/// <c>intent:activity</c>, <c>intent:economy</c>, <c>intent:crew</c>,
/// <c>intent:legal</c>, <c>intent:org</c>,
/// <c>potency:capacity</c>, <c>potency:reach</c>, <c>potency:resilience</c>,
/// <c>potency:footprint</c>,
/// <c>status:lifecycle</c>, <c>status:modifier</c>,
/// <c>tradeoff</c>, <c>custom</c>. The category is stored separately in
/// <see cref="Category"/> for efficient filtering.</para>
///
/// <para><strong>Scoping:</strong> <see cref="AllowedScopes"/> is a
/// comma-separated string of entity type names (e.g. <c>"OwnedShip"</c>,
/// <c>"UserFleetGroup"</c>, <c>"OwnedShip,UserFleetGroup"</c>) that controls
/// which entity types a tag may be applied to. The tag-assignment layer validates
/// against this before persisting.</para>
///
/// <para><strong>Weight convention:</strong> when a tag is assigned via
/// <see cref="OwnedShipTag.Weight"/>, the values <c>1</c> = primary,
/// <c>2</c> = secondary, <c>3</c> = tertiary. The recommendation engine uses
/// weight to rank tag relevance.</para>
///
/// <para><strong>System tags:</strong> when <see cref="IsSystemDefined"/> is
/// <see langword="true"/> the tag was seeded at first launch. System tags can be
/// archived (<see cref="IsArchived"/> = <see langword="true"/>) to hide them from
/// pickers, but they must never be hard-deleted because existing records may still
/// reference them.</para>
///
/// <para><strong>Persistence:</strong> stored via sqlite-net-pcl in the
/// <c>TagDefinitions</c> table and managed through
/// <c>ITagRepository</c>.</para>
/// </summary>
[Table("TagDefinitions")]
public class TagDefinition
{
    /// <summary>
    /// Stable unique key. NEVER changes once shipped.
    /// Format: "category:slug" e.g. "doctrine:value:backbone", "intent:activity:fight".
    /// </summary>
    [PrimaryKey]
    public string Key { get; set; } = string.Empty;

    /// <summary>Human-readable display name for the tag.</summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>
    /// Category: doctrine:value, doctrine:frequency, doctrine:investment, doctrine:identity,
    /// doctrine:structural, intent:activity, intent:economy, intent:crew, intent:legal,
    /// intent:org, potency:capacity, potency:reach, potency:resilience, potency:footprint,
    /// status:lifecycle, status:modifier, tradeoff, custom.
    /// </summary>
    public string Category { get; set; } = string.Empty;

    /// <summary>Explanation of what this tag means.</summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>Hex color for UI chip display e.g. "#00d4ff".</summary>
    public string ColorHex { get; set; } = "#7A8499";

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
