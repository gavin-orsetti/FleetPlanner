using SQLite;

namespace FleetPlanner.Models;

/// <summary>
/// A controlled tag definition for the group-level tag taxonomy. This is a completely
/// separate SQLite table from the ship-level <see cref="TagDefinition"/> table. Group
/// tags describe formations and task groups rather than individual ships.
///
/// <para><strong>Key format:</strong> <see cref="Key"/> uses the pattern
/// <c>"category:slug"</c> (e.g. <c>"role:strike"</c>, <c>"doctrine:weight:primary-arm"</c>).
/// Keys are the primary key and <strong>must never change once shipped</strong> — they
/// serve as stable identifiers across schema migrations, are human-readable in
/// diagnostics, and avoid auto-increment collision issues.</para>
///
/// <para><strong>Dimensions (14 + custom):</strong> <c>role:economy</c>,
/// <c>role:activity</c>, <c>role:domain</c>, <c>role:scope</c>,
/// <c>role:posture</c>, <c>ctx</c>, <c>doctrine:weight</c>,
/// <c>doctrine:frequency</c>, <c>doctrine:purpose</c>, <c>doctrine:autonomy</c>,
/// <c>doctrine:flexibility</c>, <c>doctrine:lifecycle</c>, <c>status</c>,
/// <c>custom</c>. The category is stored separately in
/// <see cref="Category"/> for efficient filtering.</para>
///
/// <para><strong>Scoping:</strong> <see cref="AllowedScopes"/> is always
/// <c>"UserFleetGroup"</c> for seeded group tags. This distinguishes them from
/// ship tags whose scopes include <c>"OwnedShip"</c>.</para>
///
/// <para><strong>Persistence:</strong> stored via sqlite-net-pcl in the
/// <c>GroupTagDefinition</c> table and managed through
/// <c>IGroupTagRepository</c>.</para>
/// </summary>
[Table("GroupTagDefinition")]
public class GroupTagDefinition
{
    /// <summary>
    /// Stable unique key. NEVER changes once shipped.
    /// Format: "category:slug" e.g. "role:strike", "doctrine:weight:primary-arm".
    /// </summary>
    [PrimaryKey]
    public string Key { get; set; } = string.Empty;

    /// <summary>Human-readable display name for the tag.</summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>
    /// Category: role:economy, role:activity, role:domain, role:scope, role:posture,
    /// ctx, doctrine:weight, doctrine:frequency, doctrine:purpose, doctrine:autonomy,
    /// doctrine:flexibility, doctrine:lifecycle, status, custom.
    /// </summary>
    public string Category { get; set; } = string.Empty;

    /// <summary>Explanation of what this tag means.</summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>Hex color for UI chip display e.g. "#C4706A".</summary>
    public string ColorHex { get; set; } = "#7A8499";

    /// <summary>Sort order within category for UI display.</summary>
    public int SortOrder { get; set; }

    /// <summary>System tags are seeded and cannot be deleted, only archived.</summary>
    public bool IsSystemDefined { get; set; }

    /// <summary>Whether users can edit the display name of this tag.</summary>
    public bool IsUserEditable { get; set; } = true;

    /// <summary>Archived tags are hidden from pickers but still valid on existing records.</summary>
    public bool IsArchived { get; set; }

    /// <summary>
    /// Scope value: always <c>"UserFleetGroup"</c> for seeded group tags.
    /// User-created group tags also default to this value.
    /// </summary>
    public string AllowedScopes { get; set; } = "UserFleetGroup";
}
