using SQLite;

namespace FleetPlanner.Models;

/// <summary>
/// A tag assignment linking a <see cref="UserFleetGroup"/> to a <see cref="GroupTagDefinition"/>.
/// Used for doctrine, intent, potency, status, and tradeoff tags that describe
/// the group's intended operational focus and composition.
/// </summary>
[Table("UserFleetGroupTags")]
public class UserFleetGroupTag
{
    /// <summary>Auto-incremented primary key.</summary>
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    /// <summary>FK to <see cref="UserFleetGroup.Id"/>.</summary>
    public int UserFleetGroupId { get; set; }

    /// <summary>FK to <see cref="GroupTagDefinition.Key"/>.</summary>
    public string TagKey { get; set; } = string.Empty;

    /// <summary>
    /// Priority weight for this tag on the group.
    /// 1 = primary, 2 = secondary, etc.
    /// </summary>
    public int Weight { get; set; } = 1;
}
