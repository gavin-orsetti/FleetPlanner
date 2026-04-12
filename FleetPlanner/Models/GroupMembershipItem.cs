using CommunityToolkit.Mvvm.ComponentModel;

namespace FleetPlanner.Models;

/// <summary>
/// Display DTO representing a single group's membership status for a specific owned ship.
/// Used in the <c>OwnedShipEditorPage</c> to let the user toggle which groups a ship belongs to.
/// <see cref="IsMember"/> is observable so the UI Switch can bind two-way.
/// </summary>
public partial class GroupMembershipItem : ObservableObject
{
    /// <summary>The <see cref="FleetPlanner.Models.UserFleetGroup.Id"/>.</summary>
    public int GroupId { get; set; }

    /// <summary>The group's display name.</summary>
    public string GroupName { get; set; } = string.Empty;

    /// <summary>Whether the ship is currently a member of this group.</summary>
    [ObservableProperty]
    private bool _isMember;
}
