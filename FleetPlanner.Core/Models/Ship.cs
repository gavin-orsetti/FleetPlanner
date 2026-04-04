using SQLite;

namespace FleetPlanner.Models;

/// <summary>
/// Represents a Star Citizen ship with stats and pricing from the external API.
/// Cached locally in SQLite for offline use.
/// </summary>
[Table("Ship")]
public class Ship
{
    [PrimaryKey]
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Manufacturer { get; set; } = string.Empty;

    public string Role { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public int Size { get; set; }

    public int CrewMin { get; set; }

    public int CrewMax { get; set; }

    public int CargoCapacity { get; set; }

    public decimal PriceUsd { get; set; }

    public long PriceAuec { get; set; }

    public string ImageUrl { get; set; } = string.Empty;

    /// <summary>
    /// Timestamp of when this ship data was last fetched from the API.
    /// </summary>
    public DateTime LastUpdated { get; set; }
}
