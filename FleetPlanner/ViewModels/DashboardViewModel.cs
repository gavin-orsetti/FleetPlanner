using System.Collections.ObjectModel;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using FleetPlanner.Models;
using FleetPlanner.Repositories;
using FleetPlanner.Services;

namespace FleetPlanner.ViewModels;

public partial class DashboardViewModel : ObservableObject
{
    private readonly IFleetRepository _fleetRepository;
    private readonly IShipDataService _shipDataService;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private int _totalShips;

    [ObservableProperty]
    private int _totalFleets;

    [ObservableProperty]
    private decimal _totalFleetValue;

    [ObservableProperty]
    private string _lastUpdated = "Never";

    // LiveCharts series/axes stubbed out for crash diagnosis

    public DashboardViewModel(IFleetRepository fleetRepository, IShipDataService shipDataService)
    {
        _fleetRepository = fleetRepository;
        _shipDataService = shipDataService;
    }

    [RelayCommand]
    private async Task LoadDataAsync()
    {
        IsLoading = true;
        try
        {
            var fleets = await _fleetRepository.GetAllFleetsAsync();
            TotalFleets = fleets.Count;

            var allShips = await _shipDataService.GetAllShipsAsync();
            var shipLookup = allShips.ToDictionary(s => s.Id);

            var allFleetShips = new List<(FleetShip fs, Ship? ship)>();
            foreach (var fleet in fleets)
            {
                var fleetShips = await _fleetRepository.GetFleetShipsAsync(fleet.Id);
                foreach (var fs in fleetShips)
                {
                    shipLookup.TryGetValue(fs.ShipId, out var ship);
                    allFleetShips.Add((fs, ship));
                }
            }

            TotalShips = allFleetShips.Count;
            TotalFleetValue = allFleetShips
                .Where(x => x.ship is not null)
                .Sum(x => x.ship!.PriceUsd);

            var lastUpdated = await _shipDataService.GetLastUpdatedAsync();
            LastUpdated = lastUpdated?.ToString("g") ?? "Never";
        }
        finally
        {
            IsLoading = false;
        }
    }
}
