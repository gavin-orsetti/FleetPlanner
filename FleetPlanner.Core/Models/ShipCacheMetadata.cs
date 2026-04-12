using SQLite;

namespace FleetPlanner.Models;

/// <summary>
/// Tracks when ship data was last fetched from the starcitizen.tools wiki API.
/// <para>
/// This is a single-row table used as a key-value store. The <see cref="Key"/> is always
/// "ship_cache", and <see cref="LastFetched"/> records the UTC timestamp of the most recent
/// successful API call. <see cref="CachedShipDataService"/> reads this to decide whether
/// the local ship cache is stale and needs refreshing.
/// </para>
/// <para>
/// <b>Why a separate table instead of a setting?</b> Keeping cache metadata in SQLite
/// (alongside the cached <see cref="Ship"/> data it describes) ensures atomicity — if the
/// database is deleted or reset, the metadata goes with it, preventing stale-timestamp bugs.
/// </para>
/// </summary>
/// <see href="https://github.com/praeclarum/sqlite-net"/>
[Table("ShipCacheMetadata")]
public class ShipCacheMetadata
{
    /// <summary>
    /// Fixed primary key — always "ship_cache". Using a string PK (instead of an int)
    /// lets this table act as a simple key-value store that could hold other metadata keys
    /// in the future without schema changes.
    /// </summary>
    [PrimaryKey]
    public string Key { get; set; } = "ship_cache";

    /// <summary>
    /// UTC timestamp of the last successful ship data fetch from the API.
    /// Compared against <c>DateTime.UtcNow</c> to determine cache freshness.
    /// </summary>
    public DateTime LastFetched { get; set; }
}
