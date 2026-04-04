using SQLite;

namespace FleetPlanner.Models;

/// <summary>
/// Tracks when ship data was last fetched from the API.
/// </summary>
[Table("ShipCacheMetadata")]
public class ShipCacheMetadata
{
    [PrimaryKey]
    public string Key { get; set; } = "ship_cache";

    public DateTime LastFetched { get; set; }
}
