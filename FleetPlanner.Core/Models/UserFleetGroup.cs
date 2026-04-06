using SQLite;

namespace FleetPlanner.Models;

/// <summary>
/// An optional organisational group for ships. Groups are tag-driven:
/// a ship's membership in a group is represented by contextual
/// <see cref="OwnedShipTag"/> records (ContextType = "group", ContextId = group Id).
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
