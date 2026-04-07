using System.Collections.ObjectModel;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using FleetPlanner.Models;
using FleetPlanner.Repositories;
using FleetPlanner.Services;
using FleetPlanner.Views;

namespace FleetPlanner.ViewModels;

/// <summary>
/// ViewModel for the Owned Ship Library page — lists all non-archived ships the user owns,
/// resolved against the ship catalogue for display names and metadata.
///
/// <para><b>Page lifecycle:</b> <see cref="LoadOwnedShipsCommand"/> runs on every
/// <c>OnAppearing</c>, rebuilding the <see cref="OwnedShips"/> collection from scratch.
/// This ensures edits made in the <c>OwnedShipEditorPage</c> are reflected on return.</para>
///
/// <para><b>Destructive commands:</b>
/// <list type="bullet">
///   <item><see cref="ArchiveShipCommand"/> — soft-deletes (sets IsArchived). No confirmation dialog.</item>
///   <item><see cref="DeleteShipCommand"/> — hard-deletes after a confirmation dialog ("Permanently delete?").</item>
/// </list>
/// Both reload the list after completion.</para>
///
/// <para><b>Navigation:</b> <see cref="EditOwnedShipCommand"/> pushes to
/// <c>OwnedShipEditorPage</c> with <see cref="Helpers.QueryParameters.OwnedShipId"/>.
/// <see cref="AddOwnedShipCommand"/> navigates to the <c>ShipBrowserPage</c> tab.</para>
/// </summary>
public partial class OwnedShipLibraryViewModel : ObservableObject
{
    private readonly IOwnedShipRepository _ownedShipRepository;
    private readonly IShipDataService _shipDataService;
    private readonly IGraphBuildService _graphBuildService;

    /// <summary>The list of owned ships currently displayed.</summary>
    [ObservableProperty]
    private ObservableCollection<OwnedShipDisplay> _ownedShips = [];

    /// <summary>True while loading.</summary>
    [ObservableProperty]
    private bool _isLoading;

    /// <summary>True when the user has no owned ships.</summary>
    [ObservableProperty]
    private bool _isEmpty;

    /// <summary>
    /// Constructor — receives dependencies from the DI container.
    /// </summary>
    public OwnedShipLibraryViewModel(IOwnedShipRepository ownedShipRepository, IShipDataService shipDataService, IGraphBuildService graphBuildService)
    {
        _ownedShipRepository = ownedShipRepository;
        _shipDataService = shipDataService;
        _graphBuildService = graphBuildService;
    }

    /// <summary>Loads all non-archived owned ships and resolves catalogue data.</summary>
    [RelayCommand]
    private async Task LoadOwnedShipsAsync()
    {
        IsLoading = true;
        try
        {
            var owned = await _ownedShipRepository.GetAllOwnedShipsAsync();
            var allShips = await _shipDataService.GetAllShipsAsync();
            var shipLookup = allShips.ToDictionary(s => s.Id);

            var displays = owned.Select(o =>
            {
                shipLookup.TryGetValue(o.ShipId, out var catalogueShip);
                return new OwnedShipDisplay
                {
                    OwnedShipId = o.Id,
                    ShipId = o.ShipId,
                    Callsign = o.Callsign,
                    ShipName = catalogueShip?.Name ?? "Unknown Ship",
                    Manufacturer = catalogueShip?.Manufacturer ?? "Unknown",
                    Role = catalogueShip?.Role ?? "Unknown",
                    AcquisitionType = o.AcquisitionType,
                    IsArchived = o.IsArchived
                };
            }).ToList();

            OwnedShips = new ObservableCollection<OwnedShipDisplay>(displays);
            IsEmpty = displays.Count == 0;
        }
        finally
        {
            IsLoading = false;
        }
    }

    /// <summary>Navigates to the Ship Browser to add a new ship to the collection.</summary>
    [RelayCommand]
    private async Task AddOwnedShipAsync()
    {
        await Shell.Current.GoToAsync($"//{nameof(ShipBrowserPage)}");
    }

    /// <summary>Navigates to the editor for an owned ship.</summary>
    [RelayCommand]
    private async Task EditOwnedShipAsync(OwnedShipDisplay ship)
    {
        if (ship is null) return;
        await Shell.Current.GoToAsync(nameof(OwnedShipEditorPage), new Dictionary<string, object>
        {
            { Helpers.QueryParameters.OwnedShipId, ship.OwnedShipId }
        });
    }

    /// <summary>Archives an owned ship (soft delete) and invalidates the graph cache.</summary>
    [RelayCommand]
    private async Task ArchiveShipAsync(OwnedShipDisplay ship)
    {
        if (ship is null) return;
        await _ownedShipRepository.ArchiveOwnedShipAsync(ship.OwnedShipId);
        _graphBuildService.InvalidateCache();
        await LoadOwnedShipsAsync();
    }

    /// <summary>Hard-deletes an owned ship after confirmation and invalidates the graph cache.</summary>
    [RelayCommand]
    private async Task DeleteShipAsync(OwnedShipDisplay ship)
    {
        if (ship is null) return;
        var confirm = await Shell.Current.DisplayAlert("Delete Ship",
            $"Permanently delete {ship.ShipName}?", "Delete", "Cancel");
        if (!confirm) return;
        await _ownedShipRepository.DeleteOwnedShipAsync(ship.OwnedShipId);
        _graphBuildService.InvalidateCache();
        await LoadOwnedShipsAsync();
    }
}

/// <summary>
/// Display DTO that merges <see cref="OwnedShip"/> user data with <see cref="Ship"/> catalogue
/// data for UI binding in the Owned Ship Library and Group Detail pages. This avoids exposing
/// raw domain models to the View and provides computed display properties like <see cref="AcquisitionBadge"/>.
/// </summary>
public class OwnedShipDisplay
{
    /// <summary>The OwnedShip record Id.</summary>
    public int OwnedShipId { get; set; }

    /// <summary>The catalogue ship Id.</summary>
    public int ShipId { get; set; }

    /// <summary>User-assigned callsign.</summary>
    public string Callsign { get; set; } = string.Empty;

    /// <summary>Name from the ship catalogue.</summary>
    public string ShipName { get; set; } = string.Empty;

    /// <summary>Manufacturer from the ship catalogue.</summary>
    public string Manufacturer { get; set; } = string.Empty;

    /// <summary>Role from the ship catalogue.</summary>
    public string Role { get; set; } = string.Empty;

    /// <summary>How the ship was acquired.</summary>
    public AcquisitionType AcquisitionType { get; set; }

    /// <summary>Whether the ship is archived.</summary>
    public bool IsArchived { get; set; }

    /// <summary>Display badge for acquisition type.</summary>
    public string AcquisitionBadge => AcquisitionType == AcquisitionType.RealMoney ? "$USD" : "aUEC";

    /// <summary>Contextual tags for this ship within a specific group (populated by GroupDetailViewModel).</summary>
    public List<TagDisplayItem> ContextualTags { get; set; } = [];

    /// <summary>Whether this ship has any contextual tags assigned in the current group context.</summary>
    public bool HasContextualTags => ContextualTags.Count > 0;
}
