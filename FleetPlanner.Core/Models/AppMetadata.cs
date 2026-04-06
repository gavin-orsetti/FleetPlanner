using SQLite;

namespace FleetPlanner.Models;

/// <summary>
/// Key-value metadata store for app-level settings such as schema version.
/// </summary>
[Table("AppMetadata")]
public class AppMetadata
{
    /// <summary>Unique key (e.g. "schema_version").</summary>
    [PrimaryKey]
    public string Key { get; set; } = string.Empty;

    /// <summary>The value stored for this key.</summary>
    public string Value { get; set; } = string.Empty;

    /// <summary>UTC timestamp of the last update.</summary>
    public DateTime UpdatedUtc { get; set; } = DateTime.UtcNow;
}
