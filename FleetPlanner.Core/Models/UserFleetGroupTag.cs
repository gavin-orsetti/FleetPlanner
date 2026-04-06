using SQLite;

namespace FleetPlanner.Models;

/// <summary>
/// A tag assignment linking a <see cref="UserFleetGroup"/> to a <see cref="TagDefinition"/>.
/// Used primarily for doctrine and focus tags that describe what the group is intended to do.
/// </summary>
[Table("UserFleetGroupTags")]
public class UserFleetGroupTag
{
    /// <summary>Auto-incremented primary key.</summary>
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    /// <summary>FK to <see cref="UserFleetGroup.Id"/>.</summary>
    public int UserFleetGroupId { get; set; }

    /// <summary>FK to <see cref="TagDefinition.Key"/>.</summary>
    public string TagKey { get; set; } = string.Empty;

    /// <summary>
    /// Priority weight for this tag on the group.
    /// 1 = primary doctrine/focus, 2 = secondary, etc.
    /// </summary>
    public int Weight { get; set; } = 1;
}
