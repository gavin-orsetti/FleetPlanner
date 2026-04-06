using SQLite;

namespace FleetPlanner.Models;

/// <summary>
/// A user-owned ship instance and the domain root of the tag-centric architecture.
///
/// Each record represents one physical ship the player owns, distinct from the
/// read-only catalogue <see cref="Ship"/> reference. A user may own multiple
/// <see cref="OwnedShip"/> records that reference the same <see cref="Ship.Id"/>
/// (e.g. two Prospectors bought at different times).
///
/// All descriptive metadata (role, doctrine, capability, status, etc.) is expressed
/// through <see cref="OwnedShipTag"/> records rather than columns on this entity.
/// Group membership is likewise represented by contextual <see cref="OwnedShipTag"/>
/// records (ContextType = "group", ContextId = group Id) rather than a direct FK.
///
/// <para><strong>Soft-delete semantics:</strong> when <see cref="IsArchived"/> is
/// <see langword="true"/> the ship is hidden from active views but remains in the
/// database for historical reference and can be restored.</para>
///
/// <para><strong>Persistence:</strong> stored via sqlite-net-pcl in the
/// <c>OwnedShips</c> table and managed through
/// <c>IOwnedShipRepository</c>. <see cref="CreatedUtc"/> and
/// <see cref="UpdatedUtc"/> are set by the repository on insert/update and must
/// not be set by the caller.</para>
/// </summary>
[Table("OwnedShips")]
public class OwnedShip
{
    /// <summary>Auto-incremented primary key.</summary>
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    /// <summary>FK to the ship catalogue (Ship.Id from starcitizen.tools).</summary>
    public int ShipId { get; set; }

    /// <summary>User-assigned name for this specific ship instance.</summary>
    public string Callsign { get; set; } = string.Empty;

    /// <summary>How the ship was acquired.</summary>
    public AcquisitionType AcquisitionType { get; set; } = AcquisitionType.AUEC;

    /// <summary>USD price paid, if real money acquisition.</summary>
    public decimal? AcquiredPriceUsd { get; set; }

    /// <summary>aUEC price paid, if in-game acquisition.</summary>
    public long? AcquiredPriceAuec { get; set; }

    /// <summary>Free-form user notes about this ship.</summary>
    public string Notes { get; set; } = string.Empty;

    /// <summary>UTC timestamp of when this record was created.</summary>
    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;

    /// <summary>UTC timestamp of the last modification.</summary>
    public DateTime UpdatedUtc { get; set; } = DateTime.UtcNow;

    /// <summary>Archived ships are hidden from active views but not deleted.</summary>
    public bool IsArchived { get; set; } = false;
}
