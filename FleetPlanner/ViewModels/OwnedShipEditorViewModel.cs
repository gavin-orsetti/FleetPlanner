using System.Collections.ObjectModel;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using FleetPlanner.Models;
using FleetPlanner.Repositories;
using FleetPlanner.Services;

namespace FleetPlanner.ViewModels;

/// <summary>
/// ViewModel for the Owned Ship Editor page — allows editing callsign, notes,
/// acquisition info, and viewing tag assignments for a single owned ship.
///
/// <para><b>QueryProperty:</b> Receives <see cref="OwnedShipId"/> via Shell navigation
/// parameter <c>"ownedShipId"</c> (an integer). The property is set before <c>OnAppearing</c>.</para>
///
/// <para><b>Page lifecycle:</b> <see cref="LoadShipCommand"/> runs on <c>OnAppearing</c>,
/// loading the <see cref="OwnedShip"/> record, resolving the catalogue ship name, and
/// loading all tags (global + contextual) into <see cref="AppliedTags"/>.</para>
///
/// <para><b>Save behaviour:</b> <see cref="SaveCommand"/> writes the edited fields back to
/// the <c>OwnedShip</c> record via <see cref="IOwnedShipRepository.SaveOwnedShipAsync"/>
/// (which stamps <c>UpdatedUtc</c>) and navigates back. <see cref="CancelCommand"/> navigates
/// back without saving.</para>
///
/// <para><b>Destructive operations:</b> None — this page only edits metadata. Archive/delete
/// are handled on the <c>OwnedShipLibraryPage</c>.</para>
/// </summary>
[QueryProperty(nameof(OwnedShipId), "ownedShipId")]
public partial class OwnedShipEditorViewModel : ObservableObject
{
    private readonly IOwnedShipRepository _ownedShipRepository;
    private readonly IOwnedShipTagRepository _ownedShipTagRepository;
    private readonly ITagRepository _tagRepository;
    private readonly IShipDataService _shipDataService;
    private readonly IGraphBuildService _graphBuildService;

    /// <summary>The OwnedShip Id received via query parameter.</summary>
    [ObservableProperty]
    private int _ownedShipId;

    /// <summary>User-assigned callsign for this ship.</summary>
    [ObservableProperty]
    private string _callsign = string.Empty;

    /// <summary>Free-form user notes.</summary>
    [ObservableProperty]
    private string _notes = string.Empty;

    /// <summary>How the ship was acquired.</summary>
    [ObservableProperty]
    private AcquisitionType _acquisitionType = AcquisitionType.AUEC;

    /// <summary>USD price paid (if real money).</summary>
    [ObservableProperty]
    private decimal? _acquiredPriceUsd;

    /// <summary>aUEC price paid (if in-game).</summary>
    [ObservableProperty]
    private long? _acquiredPriceAuec;

    /// <summary>Name of the catalogue ship for display.</summary>
    [ObservableProperty]
    private string _shipName = string.Empty;

    /// <summary>Tags currently applied to this ship.</summary>
    [ObservableProperty]
    private ObservableCollection<OwnedShipTag> _appliedTags = [];

    /// <summary>True while loading.</summary>
    [ObservableProperty]
    private bool _isLoading;

    private OwnedShip? _currentShip;

    /// <summary>
    /// Constructor — receives dependencies from the DI container.
    /// </summary>
    public OwnedShipEditorViewModel(
        IOwnedShipRepository ownedShipRepository,
        IOwnedShipTagRepository ownedShipTagRepository,
        ITagRepository tagRepository,
        IShipDataService shipDataService,
        IGraphBuildService graphBuildService)
    {
        _ownedShipRepository = ownedShipRepository;
        _ownedShipTagRepository = ownedShipTagRepository;
        _tagRepository = tagRepository;
        _shipDataService = shipDataService;
        _graphBuildService = graphBuildService;
    }

    /// <summary>Loads the owned ship and its tags.</summary>
    [RelayCommand]
    private async Task LoadShipAsync()
    {
        if (OwnedShipId <= 0) return;
        IsLoading = true;
        try
        {
            _currentShip = await _ownedShipRepository.GetOwnedShipAsync(OwnedShipId);
            if (_currentShip is null) return;

            Callsign = _currentShip.Callsign;
            Notes = _currentShip.Notes;
            AcquisitionType = _currentShip.AcquisitionType;
            AcquiredPriceUsd = _currentShip.AcquiredPriceUsd;
            AcquiredPriceAuec = _currentShip.AcquiredPriceAuec;

            // Resolve catalogue ship name
            var catalogueShip = await _shipDataService.GetShipAsync(_currentShip.ShipId);
            ShipName = catalogueShip?.Name ?? "Unknown Ship";

            // Load tags
            var tags = await _ownedShipTagRepository.GetTagsForOwnedShipAsync(OwnedShipId);
            AppliedTags = new ObservableCollection<OwnedShipTag>(tags);
        }
        finally
        {
            IsLoading = false;
        }
    }

    /// <summary>Saves the owned ship with current edits.</summary>
    [RelayCommand]
    private async Task SaveAsync()
    {
        if (_currentShip is null) return;

        _currentShip.Callsign = Callsign;
        _currentShip.Notes = Notes;
        _currentShip.AcquisitionType = AcquisitionType;
        _currentShip.AcquiredPriceUsd = AcquiredPriceUsd;
        _currentShip.AcquiredPriceAuec = AcquiredPriceAuec;

        await _ownedShipRepository.SaveOwnedShipAsync(_currentShip);
        _graphBuildService.InvalidateCache();
        await Shell.Current.GoToAsync("..");
    }

    /// <summary>Navigates back without saving.</summary>
    [RelayCommand]
    private async Task CancelAsync()
    {
        await Shell.Current.GoToAsync("..");
    }
}
