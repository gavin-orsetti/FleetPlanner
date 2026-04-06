using SQLite;

namespace FleetPlanner.Models;

/// <summary>
/// An organisational container that lets the user group ships for a specific
/// purpose (e.g. "Mining Fleet", "Combat Patrol Alpha").
///
/// Ship membership is <strong>not</strong> modelled with a direct FK. Instead, an
/// <see cref="OwnedShipTag"/> with <c>ContextType = "group"</c> and
/// <c>ContextId</c> equal to this group's <see cref="Id"/> represents the
/// association. This tag-centric approach lets the same ship appear in multiple
/// groups with different contextual tags (e.g. "role:escort" in one group,
/// "role:scout" in another).
///
/// Groups themselves carry doctrine tags via <see cref="UserFleetGroupTag"/>
/// records that describe the group's intended operational focus (e.g.
/// "doctrine:industrial", "doctrine:combat"). The recommendation engine reads
/// these tags alongside <see cref="IntendedFocus"/> to suggest fleet composition.
///
/// <para><strong>Crew model:</strong> <see cref="CrewTarget"/> represents the
/// number of real players available to crew ships in this group, which the
/// recommendation engine uses to filter out ships that cannot be adequately
/// staffed.</para>
///
/// <para><strong>Soft-delete semantics:</strong> when <see cref="IsArchived"/> is
/// <see langword="true"/> the group is hidden from active views but remains in the
/// database for historical reference and can be restored.</para>
///
/// <para><strong>Persistence:</strong> stored via sqlite-net-pcl in the
/// <c>UserFleetGroups</c> table and managed through
/// <c>IUserFleetGroupRepository</c>. <see cref="CreatedUtc"/> and
/// <see cref="UpdatedUtc"/> are set by the repository on insert/update and must
/// not be set by the caller.</para>
/// </summary>
[Table("UserFleetGroups")]
public class UserFleetGroup
{
    /// <summary>Auto-incremented primary key.</summary>
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    /// <summary>User-assigned name for this group.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>User-assigned description of this group.</summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Free-text description of what this group is intended to do.
    /// e.g. "Deep space mining operation with refinery support".
    /// The recommendation engine reads this alongside group tags for context.
    /// </summary>
    public string IntendedFocus { get; set; } = string.Empty;

    /// <summary>How many real players are available for this group.</summary>
    public int CrewTarget { get; set; } = 1;

    /// <summary>Sort order for UI display.</summary>
    public int SortOrder { get; set; } = 0;

    /// <summary>Archived groups are hidden from active views but not deleted.</summary>
    public bool IsArchived { get; set; } = false;

    /// <summary>UTC timestamp of when this record was created.</summary>
    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;

    /// <summary>UTC timestamp of the last modification.</summary>
    public DateTime UpdatedUtc { get; set; } = DateTime.UtcNow;
}
