using System.Collections.ObjectModel;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using FleetPlanner.Models;
using FleetPlanner.Repositories;
using FleetPlanner.Services;

namespace FleetPlanner.ViewModels;

/// <summary>
/// ViewModel for the Group Detail page — shows a group's doctrine tags and member ships.
/// </summary>
[QueryProperty(nameof(GroupId), "groupId")]
public partial class GroupDetailViewModel : ObservableObject
{
    private readonly IUserFleetGroupRepository _groupRepository;
    private readonly IUserFleetGroupTagRepository _groupTagRepository;
    private readonly IOwnedShipTagRepository _ownedShipTagRepository;
    private readonly IOwnedShipRepository _ownedShipRepository;
    private readonly IShipDataService _shipDataService;

    /// <summary>The group Id received via query parameter.</summary>
    [ObservableProperty]
    private int _groupId;

    /// <summary>The group being displayed.</summary>
    [ObservableProperty]
    private UserFleetGroup? _group;

    /// <summary>Doctrine/focus tags applied to this group.</summary>
    [ObservableProperty]
    private ObservableCollection<UserFleetGroupTag> _groupTags = [];

    /// <summary>Ships assigned to this group via contextual tags.</summary>
    [ObservableProperty]
    private ObservableCollection<OwnedShipDisplay> _memberShips = [];

    /// <summary>True while loading.</summary>
    [ObservableProperty]
    private bool _isLoading;

    /// <summary>
    /// Constructor — receives dependencies from the DI container.
    /// </summary>
    public GroupDetailViewModel(
        IUserFleetGroupRepository groupRepository,
        IUserFleetGroupTagRepository groupTagRepository,
        IOwnedShipTagRepository ownedShipTagRepository,
        IOwnedShipRepository ownedShipRepository,
        IShipDataService shipDataService)
    {
        _groupRepository = groupRepository;
        _groupTagRepository = groupTagRepository;
        _ownedShipTagRepository = ownedShipTagRepository;
        _ownedShipRepository = ownedShipRepository;
        _shipDataService = shipDataService;
    }

    /// <summary>Loads the group, its tags, and member ships.</summary>
    [RelayCommand]
    private async Task LoadGroupAsync()
    {
        if (GroupId <= 0) return;
        IsLoading = true;
        try
        {
            Group = await _groupRepository.GetGroupAsync(GroupId);
            if (Group is null) return;

            // Load group doctrine tags
            var tags = await _groupTagRepository.GetTagsForGroupAsync(GroupId);
            GroupTags = new ObservableCollection<UserFleetGroupTag>(tags);

            // Find ships assigned to this group via contextual OwnedShipTags
            var allOwned = await _ownedShipRepository.GetAllOwnedShipsAsync();
            var allShips = await _shipDataService.GetAllShipsAsync();
            var shipLookup = allShips.ToDictionary(s => s.Id);

            var memberDisplays = new List<OwnedShipDisplay>();
            foreach (var owned in allOwned)
            {
                var shipTags = await _ownedShipTagRepository.GetTagsForOwnedShipAsync(owned.Id, "group", GroupId);
                if (shipTags.Count > 0)
                {
                    shipLookup.TryGetValue(owned.ShipId, out var catalogueShip);
                    memberDisplays.Add(new OwnedShipDisplay
                    {
                        OwnedShipId = owned.Id,
                        ShipId = owned.ShipId,
                        Callsign = owned.Callsign,
                        ShipName = catalogueShip?.Name ?? "Unknown Ship",
                        Manufacturer = catalogueShip?.Manufacturer ?? "Unknown",
                        Role = catalogueShip?.Role ?? "Unknown",
                        AcquisitionType = owned.AcquisitionType,
                        IsArchived = owned.IsArchived
                    });
                }
            }

            MemberShips = new ObservableCollection<OwnedShipDisplay>(memberDisplays);
        }
        finally
        {
            IsLoading = false;
        }
    }

    /// <summary>Navigates back to the group overview.</summary>
    [RelayCommand]
    private async Task GoBackAsync()
    {
        await Shell.Current.GoToAsync("..");
    }
}
