using SQLite;

namespace FleetPlanner.Models;

/// <summary>
/// A user-owned ship instance. This is the domain root of the tag-centric model.
/// Each record represents one physical ship the player owns, distinct from the
/// read-only catalogue <see cref="Ship"/> reference.
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
