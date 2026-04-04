using SQLite;

namespace FleetPlanner.Models;

/// <summary>
/// Represents a Star Citizen ship with stats and pricing from the external UEX Corp API.
/// <para>
/// This is the global ship-reference table — every known ship in the game has one row here.
/// The data is fetched from the UEX Corp community API (<see href="https://uexcorp.space/api"/>)
/// and cached locally in SQLite so the app works offline. The <see cref="CachedShipDataService"/>
/// handles the fetch-and-cache lifecycle.
/// </para>
/// <para>
/// <b>Important distinction:</b> <see cref="Ship"/> is a read-only reference record (game data).
/// <see cref="FleetShip"/> is the user-owned instance that links a Ship to a Fleet with
/// player-specific metadata (callsign, purchase info).
/// </para>
/// </summary>
/// <see href="https://github.com/praeclarum/sqlite-net"/>
[Table("Ship")]
public class Ship
{
    /// <summary>
    /// Primary key — matches the ship ID from the UEX Corp API.
    /// Not auto-incremented because the ID comes from the external data source.
    /// </summary>
    [PrimaryKey]
    public int Id { get; set; }

    /// <summary>The ship's full name (e.g., "Aegis Avenger Titan", "RSI Constellation Andromeda").</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>The in-game manufacturer (e.g., "Aegis Dynamics", "Roberts Space Industries").</summary>
    public string Manufacturer { get; set; } = string.Empty;

    /// <summary>
    /// The ship's primary role as classified by the game (e.g., "Combat", "Mining", "Exploration").
    /// Used by the recommendation engine to match ships to fleet roles.
    /// </summary>
    public string Role { get; set; } = string.Empty;

    /// <summary>A short description of the ship from the game's lore or marketing material.</summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Size class (1 = snub/small, up to ~6 = capital). Larger ships generally require more crew
    /// and can carry more cargo. The recommendation engine uses this for scale-appropriate filtering.
    /// </summary>
    public string Size { get; set; } = string.Empty;

    /// <summary>
    /// Minimum crew needed to fly the ship. A solo player (1 crew) can't effectively operate
    /// a ship with CrewMin > 1 without NPC crew or AI blades (future game feature).
    /// </summary>
    public int CrewMin { get; set; }

    /// <summary>
    /// Maximum crew the ship can accommodate. Ships with high CrewMax are designed for
    /// multi-crew gameplay — turrets, engineering stations, medical bays, etc.
    /// </summary>
    public int CrewMax { get; set; }

    /// <summary>
    /// Cargo capacity in SCU (Standard Cargo Units). Zero for pure combat ships;
    /// hundreds or thousands for dedicated haulers like the Hull series.
    /// </summary>
    public int CargoCapacity { get; set; }

    /// <summary>Price in real-world USD on the RSI pledge store (may be 0 if not currently available).</summary>
    public decimal PriceUsd { get; set; }

    /// <summary>
    /// Price in Alpha UEC (aUEC), the in-game currency. Used for value analysis in recommendations.
    /// Zero if the ship can't be bought in-game yet.
    /// </summary>
    public long PriceAuec { get; set; }

    /// <summary>URL to the ship's image/thumbnail, used for display in the Ship Browser and detail views.</summary>
    public string ImageUrl { get; set; } = string.Empty;

    /// <summary>
    /// UTC timestamp of when this ship record was last fetched from the UEX Corp API.
    /// Used by <see cref="CachedShipDataService"/> to decide whether the cache is stale.
    /// </summary>
    public DateTime LastUpdated { get; set; }
}
