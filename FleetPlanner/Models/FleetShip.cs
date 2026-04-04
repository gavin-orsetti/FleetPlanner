using SQLite;

namespace FleetPlanner.Models;

/// <summary>
/// A ship instance within a fleet. Links a Ship reference to a Fleet with
/// user-specific metadata like callsign and purchase info.
/// Evolved from V1's ShipDetail model.
/// </summary>
[Table("FleetShip")]
public class FleetShip
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    [Indexed]
    public int FleetId { get; set; }

    public int ShipId { get; set; }

    public string Callsign { get; set; } = string.Empty;

    public string Notes { get; set; } = string.Empty;

    public bool Purchased { get; set; }

    public decimal PurchasePrice { get; set; }

    public string PurchaseCurrency { get; set; } = "USD";

    public int AcquisitionType { get; set; } // Maps to AcquisitionType enum (0=AUEC, 1=RealMoney)

    public decimal? PledgeStorePriceUsd { get; set; }
}
