using System.Collections.ObjectModel;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using FleetPlanner.Helpers;
using FleetPlanner.Models;
using FleetPlanner.Repositories;
using FleetPlanner.Services;
using FleetPlanner.Views;

namespace FleetPlanner.ViewModels;

/// <summary>
/// ViewModel for the Group Detail page — displays a group's metadata, doctrine/focus tags,
/// and member ships (determined by contextual tag assignment). Supports editing group tags
/// and per-ship contextual role tags within the group.
///
/// <para><b>QueryProperty:</b> Receives <see cref="GroupId"/> via Shell navigation parameter
/// <c>"groupId"</c> (an integer). The property is set before <c>OnAppearing</c> fires.</para>
///
/// <para><b>Page lifecycle:</b> <see cref="LoadGroupCommand"/> runs on every <c>OnAppearing</c>.
/// It loads the group record, its <see cref="UserFleetGroupTag"/> records, and then iterates
/// all owned ships to find members (ships with any contextual tag for this group). The
/// <see cref="MemberShips"/> collection is rebuilt from scratch on each load.</para>
///
/// <para><b>Tag editing:</b> <see cref="EditGroupTagsCommand"/> navigates to <see cref="TagPickerPage"/>
/// for group-level doctrine/focus tags. <see cref="EditShipRoleInGroupCommand"/> navigates to
/// <see cref="TagPickerPage"/> for a specific ship's contextual tags within this group.</para>
/// </summary>
[QueryProperty(nameof(GroupId), "groupId")]
public partial class GroupDetailViewModel : ObservableObject
{
    private readonly IUserFleetGroupRepository _groupRepository;
    private readonly IUserFleetGroupTagRepository _groupTagRepository;
    private readonly IOwnedShipTagRepository _ownedShipTagRepository;
    private readonly IOwnedShipRepository _ownedShipRepository;
    private readonly IShipDataService _shipDataService;
    private readonly ITagRepository _tagRepository;
    private readonly IGraphBuildService _graphBuildService;

    /// <summary>The group Id received via query parameter.</summary>
    [ObservableProperty]
    private int _groupId;

    /// <summary>The group being displayed.</summary>
    [ObservableProperty]
    private UserFleetGroup? _group;

    /// <summary>Doctrine/focus tags applied to this group (raw model).</summary>
    [ObservableProperty]
    private ObservableCollection<UserFleetGroupTag> _groupTags = [];

    /// <summary>Group tags displayed as styled chips.</summary>
    [ObservableProperty]
    private ObservableCollection<TagDisplayItem> _groupTagDisplayItems = [];

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
        IShipDataService shipDataService,
        ITagRepository tagRepository,
        IGraphBuildService graphBuildService)
    {
        _groupRepository = groupRepository;
        _groupTagRepository = groupTagRepository;
        _ownedShipTagRepository = ownedShipTagRepository;
        _ownedShipRepository = ownedShipRepository;
        _shipDataService = shipDataService;
        _tagRepository = tagRepository;
        _graphBuildService = graphBuildService;
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

            // Load group doctrine tags and build display items
            var tags = await _groupTagRepository.GetTagsForGroupAsync(GroupId);
            GroupTags = new ObservableCollection<UserFleetGroupTag>(tags);

            var groupDisplayItems = new List<TagDisplayItem>();
            foreach (var gt in tags)
            {
                var def = await _tagRepository.GetTagAsync(gt.TagKey);
                groupDisplayItems.Add(new TagDisplayItem
                {
                    TagKey = gt.TagKey,
                    DisplayName = def?.DisplayName ?? gt.TagKey,
                    Category = def?.Category ?? "unknown",
                    ColorHex = def?.ColorHex,
                    Weight = gt.Weight
                });
            }
            GroupTagDisplayItems = new ObservableCollection<TagDisplayItem>(groupDisplayItems);

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

                    // Build contextual tag display items for this ship in this group
                    var contextualDisplayTags = new List<TagDisplayItem>();
                    foreach (var st in shipTags)
                    {
                        var def = await _tagRepository.GetTagAsync(st.TagKey);
                        contextualDisplayTags.Add(new TagDisplayItem
                        {
                            TagKey = st.TagKey,
                            DisplayName = def?.DisplayName ?? st.TagKey,
                            Category = def?.Category ?? "unknown",
                            ColorHex = def?.ColorHex,
                            Weight = st.Weight
                        });
                    }

                    memberDisplays.Add(new OwnedShipDisplay
                    {
                        OwnedShipId = owned.Id,
                        ShipId = owned.ShipId,
                        Callsign = owned.Callsign,
                        ShipName = catalogueShip?.Name ?? "Unknown Ship",
                        Manufacturer = catalogueShip?.Manufacturer ?? "Unknown",
                        Role = catalogueShip?.Role ?? "Unknown",
                        AcquisitionType = owned.AcquisitionType,
                        IsArchived = owned.IsArchived,
                        ContextualTags = contextualDisplayTags
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

    /// <summary>Navigates to the tag picker for group-level doctrine/focus tag editing.</summary>
    [RelayCommand]
    private async Task EditGroupTagsAsync()
    {
        if (GroupId <= 0) return;
        await Shell.Current.GoToAsync(nameof(TagPickerPage), new Dictionary<string, object>
        {
            { QueryParameters.TagPickerOwnedShipId, 0 },
            { QueryParameters.TagPickerGroupId, GroupId },
            { QueryParameters.TagPickerContextType, string.Empty },
            { QueryParameters.TagPickerContextId, 0 }
        });
    }

    /// <summary>Navigates to the tag picker for a specific ship's role within this group.</summary>
    [RelayCommand]
    private async Task EditShipRoleInGroupAsync(OwnedShipDisplay ship)
    {
        if (ship is null || GroupId <= 0) return;
        await Shell.Current.GoToAsync(nameof(TagPickerPage), new Dictionary<string, object>
        {
            { QueryParameters.TagPickerOwnedShipId, ship.OwnedShipId },
            { QueryParameters.TagPickerGroupId, GroupId },
            { QueryParameters.TagPickerContextType, "group" },
            { QueryParameters.TagPickerContextId, GroupId }
        });
    }

    /// <summary>Navigates back to the group overview.</summary>
    [RelayCommand]
    private async Task GoBackAsync()
    {
        await Shell.Current.GoToAsync("..");
    }
}
