using System.Collections.ObjectModel;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using FleetPlanner.Models;
using FleetPlanner.Repositories;
using FleetPlanner.Services;

namespace FleetPlanner.ViewModels;

[QueryProperty(nameof(SelectMode), "selectMode")]
[QueryProperty(nameof(FleetId), "fleetId")]
public partial class ShipBrowserViewModel : ObservableObject
{
    private readonly IShipDataService _shipDataService;
    private readonly IFleetRepository _fleetRepository;

    private List<Ship> _allShips = [];

    [ObservableProperty]
    private ObservableCollection<Ship> _ships = [];

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private bool _isRefreshing;

    [ObservableProperty]
    private string _searchText = string.Empty;

    [ObservableProperty]
    private string _selectedRole = "All";

    [ObservableProperty]
    private string _selectedManufacturer = "All";

    [ObservableProperty]
    private string _selectedSize = "All";

    [ObservableProperty]
    private ObservableCollection<string> _roles = ["All"];

    [ObservableProperty]
    private ObservableCollection<string> _manufacturers = ["All"];

    [ObservableProperty]
    private ObservableCollection<string> _sizes = ["All"];

    [ObservableProperty]
    private bool _selectMode;

    [ObservableProperty]
    private int _fleetId;

    [ObservableProperty]
    private string _lastUpdated = "Never";

    [ObservableProperty]
    private bool _isOffline;

    public ShipBrowserViewModel(IShipDataService shipDataService, IFleetRepository fleetRepository)
    {
        _shipDataService = shipDataService;
        _fleetRepository = fleetRepository;
    }

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
            .Select(s => s.Size.ToString())
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

        if (SelectedSize != "All" && int.TryParse(SelectedSize, out var size))
            filtered = filtered.Where(s => s.Size == size);

        Ships = new ObservableCollection<Ship>(filtered.ToList());
    }

    [RelayCommand]
    private async Task SelectShipAsync(Ship ship)
    {
        if (ship is null)
            return;

        if (SelectMode && FleetId > 0)
        {
            var fleetShip = new FleetShip
            {
                FleetId = FleetId,
                ShipId = ship.Id,
                Callsign = ship.Name
            };
            await _fleetRepository.SaveFleetShipAsync(fleetShip);
            await Shell.Current.GoToAsync("..");
        }
        else
        {
            await Shell.Current.GoToAsync($"ShipDetailPage?shipId={ship.Id}");
        }
    }
}
