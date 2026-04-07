using CommunityToolkit.Mvvm.ComponentModel;

namespace FleetPlanner.Models;

/// <summary>
/// Display DTO representing a single owned ship's membership status within a specific group.
/// Used in the <c>GroupDetailPage</c> to let the user toggle which ships belong to the group.
/// <see cref="IsMember"/> is observable so the UI Switch can bind two-way.
/// </summary>
public partial class GroupShipCandidateItem : ObservableObject
{
    /// <summary>The <see cref="FleetPlanner.Models.OwnedShip.Id"/>.</summary>
    public int OwnedShipId { get; set; }

    /// <summary>User-assigned callsign for this ship.</summary>
    public string Callsign { get; set; } = string.Empty;

    /// <summary>Name from the ship catalogue.</summary>
    public string ShipName { get; set; } = string.Empty;

    /// <summary>Whether the ship is currently a member of this group.</summary>
    [ObservableProperty]
    private bool _isMember;
}
