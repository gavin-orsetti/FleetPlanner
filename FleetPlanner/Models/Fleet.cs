using SQLite;

namespace FleetPlanner.Models;

/// <summary>
/// A user-created fleet that contains ships. Persisted locally in SQLite.
/// Preserved from V1 with minor field adjustments.
/// </summary>
[Table("Fleet")]
public class Fleet
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    [NotNull]
    public string Name { get; set; } = string.Empty;

    public string Affiliation { get; set; } = string.Empty;

    public string AreaOfOperation { get; set; } = string.Empty;

    public string Manifesto { get; set; } = string.Empty;

    public string Notes { get; set; } = string.Empty;
}
