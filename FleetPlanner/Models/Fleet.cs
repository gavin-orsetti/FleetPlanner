using SQLite;

namespace FleetPlanner.Models;

/// <summary>
/// A user-created fleet that contains ships. Persisted locally in SQLite.
/// Supports multi-fleet with declared intent/goals.
/// </summary>
[Table("Fleet")]
public class Fleet
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    [NotNull]
    public string Name { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public int PrimaryFocus { get; set; } // Maps to FleetFocus enum

    /// <summary>
    /// Comma-separated list of FleetRole enum int values.
    /// </summary>
    public string DeclaredRolesRaw { get; set; } = string.Empty;

    public int AvailableCrewCount { get; set; }

    public int OperatingScale { get; set; } // Maps to FleetOperatingScale enum

    public DateTime DateCreated { get; set; } = DateTime.UtcNow;

    public DateTime DateModified { get; set; } = DateTime.UtcNow;

    // Legacy fields preserved for backward compatibility with existing data
    public string Affiliation { get; set; } = string.Empty;

    public string AreaOfOperation { get; set; } = string.Empty;

    public string Manifesto { get; set; } = string.Empty;

    public string Notes { get; set; } = string.Empty;

    [Ignore]
    public FleetFocus PrimaryFocusEnum
    {
        get => (FleetFocus)PrimaryFocus;
        set => PrimaryFocus = (int)value;
    }

    [Ignore]
    public FleetOperatingScale OperatingScaleEnum
    {
        get => (FleetOperatingScale)OperatingScale;
        set => OperatingScale = (int)value;
    }

    [Ignore]
    public List<FleetRole> DeclaredRoles
    {
        get
        {
            if (string.IsNullOrWhiteSpace(DeclaredRolesRaw))
                return [];
            return DeclaredRolesRaw
                .Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(s => (FleetRole)int.Parse(s))
                .ToList();
        }
        set
        {
            DeclaredRolesRaw = string.Join(",", value.Select(r => ((int)r).ToString()));
        }
    }
}
