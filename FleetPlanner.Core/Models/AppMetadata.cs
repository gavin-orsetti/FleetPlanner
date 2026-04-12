using SQLite;

namespace FleetPlanner.Models;

/// <summary>
/// Key-value metadata store for app-level settings persisted in SQLite.
/// Currently holds a single key (<c>"schema_version"</c>) used by
/// <see cref="FleetPlanner.Services.DatabaseBootstrapService"/> to gate idempotent seeding.
/// Future keys can be added without schema changes.
///
/// <para><b>Architecture:</b> Sits in the user data layer. Created and managed by
/// <see cref="FleetPlanner.Services.DatabaseBootstrapService.InitialiseAsync"/>. Not accessed
/// through a dedicated repository — the bootstrap service writes directly via sqlite-net-pcl.</para>
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
