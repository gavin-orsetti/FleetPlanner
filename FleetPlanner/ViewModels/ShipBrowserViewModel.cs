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
/// ViewModel for the Ship Browser page — lists all known ships with search, filter, and add-to-collection support.
/// <para>
/// In the tag-centric model, adding a ship creates an <see cref="OwnedShip"/> record
/// and navigates to the <see cref="OwnedShipEditorPage"/> for tag assignment.
/// </para>
/// </summary>
public partial class ShipBrowserViewModel : ObservableObject
{
    private readonly IShipDataService _shipDataService;
    private readonly IOwnedShipRepository _ownedShipRepository;

    private List<Ship> _allShips = [];

    /// <summary>The filtered list of ships currently displayed in the CollectionView.</summary>
    [ObservableProperty]
    private ObservableCollection<Ship> _ships = [];

    /// <summary>True while the initial load is in progress.</summary>
    [ObservableProperty]
    private bool _isLoading;

    /// <summary>True while a pull-to-refresh is in progress.</summary>
    [ObservableProperty]
    private bool _isRefreshing;

    /// <summary>Text entered in the search bar.</summary>
    [ObservableProperty]
    private string _searchText = string.Empty;

    /// <summary>Currently selected role filter.</summary>
    [ObservableProperty]
    private string _selectedRole = "All";

    /// <summary>Currently selected manufacturer filter.</summary>
    [ObservableProperty]
    private string _selectedManufacturer = "All";

    /// <summary>Currently selected size filter.</summary>
    [ObservableProperty]
    private string _selectedSize = "All";

    /// <summary>Available role options for the filter picker.</summary>
    [ObservableProperty]
    private ObservableCollection<string> _roles = ["All"];

    /// <summary>Available manufacturer options for the filter picker.</summary>
    [ObservableProperty]
    private ObservableCollection<string> _manufacturers = ["All"];

    /// <summary>Available size options for the filter picker.</summary>
    [ObservableProperty]
    private ObservableCollection<string> _sizes = ["All"];

    /// <summary>Human-readable string showing when ship data was last refreshed.</summary>
    [ObservableProperty]
    private string _lastUpdated = "Never";

    /// <summary>True if the last load attempt failed due to network issues.</summary>
    [ObservableProperty]
    private bool _isOffline;

    /// <summary>
    /// Constructor — receives dependencies from the DI container.
    /// </summary>
    public ShipBrowserViewModel(IShipDataService shipDataService, IOwnedShipRepository ownedShipRepository)
    {
        _shipDataService = shipDataService;
        _ownedShipRepository = ownedShipRepository;
    }

    /// <summary>Loads the ship catalogue from the cache/API.</summary>
    [RelayCommand]
    private async Task LoadShipsAsync()
    {
        IsLoading = true;
        IsOffline = false;
        try
        {
            _allShips = await _shipDataService.GetAllShipsAsync();
            PopulateFilters();
            ApplyFilters();

            var lastUpdated = await _shipDataService.GetLastUpdatedAsync();
            LastUpdated = lastUpdated?.ToString("g") ?? "Never";
        }
        catch (Exception)
        {
            IsOffline = true;
        }
        finally
        {
            IsLoading = false;
        }
    }

    /// <summary>Force-refreshes ship data from the API.</summary>
    [RelayCommand]
    private async Task RefreshShipsAsync()
    {
        IsRefreshing = true;
        IsOffline = false;
        try
        {
            _allShips = await _shipDataService.GetAllShipsAsync(forceRefresh: true);
            PopulateFilters();
            ApplyFilters();

            var lastUpdated = await _shipDataService.GetLastUpdatedAsync();
            LastUpdated = lastUpdated?.ToString("g") ?? "Never";
        }
        catch (Exception)
        {
            IsOffline = true;
        }
        finally
        {
            IsRefreshing = false;
        }
    }

    partial void OnSearchTextChanged(string value) => ApplyFilters();
    partial void OnSelectedRoleChanged(string value) => ApplyFilters();
    partial void OnSelectedManufacturerChanged(string value) => ApplyFilters();
    partial void OnSelectedSizeChanged(string value) => ApplyFilters();

    private void PopulateFilters()
    {
        var roleList = _allShips
            .Select(s => s.Role)
            .Where(r => !string.IsNullOrWhiteSpace(r))
            .Distinct()
            .OrderBy(r => r)
            .ToList();
        Roles = new ObservableCollection<string>(["All", .. roleList]);

        var manufacturerList = _allShips
            .Select(s => s.Manufacturer)
            .Where(m => !string.IsNullOrWhiteSpace(m))
            .Distinct()
            .OrderBy(m => m)
            .ToList();
        Manufacturers = new ObservableCollection<string>(["All", .. manufacturerList]);

        var sizeList = _allShips
            .Select(s => s.Size)
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .Distinct()
            .OrderBy(s => s)
            .ToList();
        Sizes = new ObservableCollection<string>(["All", .. sizeList]);
    }

    private void ApplyFilters()
    {
        var filtered = _allShips.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(SearchText))
        {
            filtered = filtered.Where(s =>
                s.Name.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ||
                s.Manufacturer.Contains(SearchText, StringComparison.OrdinalIgnoreCase));
        }

        if (SelectedRole != "All")
            filtered = filtered.Where(s => s.Role == SelectedRole);

        if (SelectedManufacturer != "All")
            filtered = filtered.Where(s => s.Manufacturer == SelectedManufacturer);

        if (SelectedSize != "All")
            filtered = filtered.Where(s => s.Size == SelectedSize);

        Ships = new ObservableCollection<Ship>(filtered.ToList());
    }

    /// <summary>
    /// Handles tapping on a ship — navigates to the Ship Detail page in browse mode.
    /// </summary>
    [RelayCommand]
    private async Task SelectShipAsync(Ship ship)
    {
        if (ship is null)
            return;

        await Shell.Current.GoToAsync(nameof(ShipDetailPage), new Dictionary<string, object>
        {
            { QueryParameters.ShipId, ship.Id }
        });
    }

    /// <summary>
    /// Creates an OwnedShip record and navigates to the editor for tag assignment.
    /// Replaces the old AddShipToFleetAsync which created FleetShip records.
    /// </summary>
    [RelayCommand]
    private async Task AddToCollectionAsync(Ship ship)
    {
        if (ship is null)
            return;

        var input = await Shell.Current.DisplayPromptAsync(
            title: $"Add {ship.Name}",
            message: "How many would you like to add to your collection?",
            accept: "Add",
            cancel: "Cancel",
            placeholder: "1",
            initialValue: "1",
            keyboard: Keyboard.Numeric);

        if (input is null)
            return;

        if (!int.TryParse(input, out int quantity) || quantity < 1)
        {
            await Shell.Current.DisplayAlert("Invalid Quantity", "Please enter a number of 1 or more.", "OK");
            return;
        }

        if (quantity > 50)
        {
            await Shell.Current.DisplayAlert("Invalid Quantity", "You can add a maximum of 50 ships at once.", "OK");
            return;
        }

        int lastId = 0;
        for (int i = 0; i < quantity; i++)
        {
            var ownedShip = new OwnedShip
            {
                ShipId = ship.Id,
                Callsign = ship.Name,
                AcquisitionType = AcquisitionType.AUEC
            };
            await _ownedShipRepository.SaveOwnedShipAsync(ownedShip);
            lastId = ownedShip.Id;
        }

        if (quantity == 1 && lastId > 0)
        {
            // Navigate to editor for tag assignment
            await Shell.Current.GoToAsync(nameof(OwnedShipEditorPage), new Dictionary<string, object>
            {
                { QueryParameters.OwnedShipId, lastId }
            });
        }
        else
        {
            var message = $"{quantity}x {ship.Name} added to your collection.";
            await Shell.Current.DisplayAlert("Added", message, "OK");
        }
    }
}
