using SQLite;

namespace FleetPlanner.Models;

/// <summary>
/// A ship instance within a fleet — the bridge/join entity between <see cref="Fleet"/> and <see cref="Ship"/>.
/// <para>
/// Each row represents one ship added to one fleet. A <see cref="Ship"/> is a global reference record
/// (name, stats, role, crew requirements), while a <see cref="FleetShip"/> carries user-specific metadata:
/// the callsign the player gave this particular hull, purchase details, and notes.
/// </para>
/// <para>
/// <b>Relationship:</b> Many FleetShips → one Fleet (via <see cref="FleetId"/>),
/// and many FleetShips → one Ship (via <see cref="ShipId"/>).
/// SQLite-net doesn't enforce foreign keys automatically; these are logical references
/// used in application code and LINQ queries.
/// </para>
/// <para>
/// Evolved from V1's "ShipDetail" model — renamed to clarify that this is a fleet-membership
/// record, not a detail view of a ship.
/// </para>
/// </summary>
/// <see href="https://github.com/praeclarum/sqlite-net"/>
[Table("FleetShip")]
public class FleetShip
{
    /// <summary>Auto-incrementing primary key assigned by SQLite on insert.</summary>
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    /// <summary>
    /// Foreign key to <see cref="Fleet.Id"/>.
    /// Indexed for fast lookups — the app frequently queries "all ships in fleet X".
    /// </summary>
    [Indexed]
    public int FleetId { get; set; }

    /// <summary>
    /// Foreign key to <see cref="Ship.Id"/> (the global ship-reference table).
    /// Used to join against the cached ship catalogue for name, stats, etc.
    /// </summary>
    public int ShipId { get; set; }

    /// <summary>
    /// Player-assigned callsign for this specific hull (e.g., "Rusty Bucket").
    /// Optional — defaults to empty string.
    /// </summary>
    public string Callsign { get; set; } = string.Empty;

    /// <summary>Free-text notes about this particular ship instance.</summary>
    public string Notes { get; set; } = string.Empty;

    /// <summary>Whether the player has actually purchased (owns) this ship, vs. it being a wishlist entry.</summary>
    public bool Purchased { get; set; }

    /// <summary>The price the player paid, in whatever currency <see cref="PurchaseCurrency"/> indicates.</summary>
    public decimal PurchasePrice { get; set; }

    /// <summary>
    /// ISO currency code for the purchase (default "USD").
    /// Kept as a string to support both real currencies and in-game currency if needed.
    /// </summary>
    public string PurchaseCurrency { get; set; } = "USD";

    /// <summary>
    /// How the ship was acquired, stored as an <see langword="int"/>.
    /// Maps to <see cref="Models.AcquisitionType"/>: 0 = aUEC (in-game), 1 = Real Money (pledge store).
    /// </summary>
    public int AcquisitionType { get; set; }

    /// <summary>
    /// The ship's current price on the RSI pledge store in USD, if known.
    /// Nullable because not all ships are currently available for pledge.
    /// </summary>
    public decimal? PledgeStorePriceUsd { get; set; }
}
