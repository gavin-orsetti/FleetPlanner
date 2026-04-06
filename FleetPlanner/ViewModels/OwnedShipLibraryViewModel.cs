using System.Collections.ObjectModel;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using FleetPlanner.Models;
using FleetPlanner.Repositories;
using FleetPlanner.Services;
using FleetPlanner.Views;

namespace FleetPlanner.ViewModels;

/// <summary>
/// ViewModel for the Owned Ship Library page — lists all ships the user owns.
/// </summary>
public partial class OwnedShipLibraryViewModel : ObservableObject
{
    private readonly IOwnedShipRepository _ownedShipRepository;
    private readonly IShipDataService _shipDataService;

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
    public OwnedShipLibraryViewModel(IOwnedShipRepository ownedShipRepository, IShipDataService shipDataService)
    {
        _ownedShipRepository = ownedShipRepository;
        _shipDataService = shipDataService;
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
        await Shell.Current.GoToAsync(nameof(ShipBrowserPage));
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

    /// <summary>Archives an owned ship (soft delete).</summary>
    [RelayCommand]
    private async Task ArchiveShipAsync(OwnedShipDisplay ship)
    {
        if (ship is null) return;
        await _ownedShipRepository.ArchiveOwnedShipAsync(ship.OwnedShipId);
        await LoadOwnedShipsAsync();
    }

    /// <summary>Hard-deletes an owned ship after confirmation.</summary>
    [RelayCommand]
    private async Task DeleteShipAsync(OwnedShipDisplay ship)
    {
        if (ship is null) return;
        var confirm = await Shell.Current.DisplayAlert("Delete Ship",
            $"Permanently delete {ship.ShipName}?", "Delete", "Cancel");
        if (!confirm) return;
        await _ownedShipRepository.DeleteOwnedShipAsync(ship.OwnedShipId);
        await LoadOwnedShipsAsync();
    }
}

/// <summary>
/// Display DTO merging OwnedShip with catalogue Ship data for UI binding.
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
}
