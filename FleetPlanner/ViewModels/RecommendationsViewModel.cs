using System.Collections.ObjectModel;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using FleetPlanner.Models;
using FleetPlanner.Repositories;
using FleetPlanner.Services;

namespace FleetPlanner.ViewModels;

public partial class RecommendationsViewModel : ObservableObject
{
    private readonly IFleetRepository _fleetRepository;
    private readonly IShipDataService _shipDataService;
    private readonly IRecommendationService _recommendationService;

    [ObservableProperty]
    private ObservableCollection<Recommendation> _recommendations = [];

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private bool _isEmpty;

    [ObservableProperty]
    private bool _hasNoFleet;

    public RecommendationsViewModel(
        IFleetRepository fleetRepository,
        IShipDataService shipDataService,
        IRecommendationService recommendationService)
    {
        _fleetRepository = fleetRepository;
        _shipDataService = shipDataService;
        _recommendationService = recommendationService;
    }

    [RelayCommand]
    private async Task LoadRecommendationsAsync()
    {
        IsLoading = true;
        try
        {
            var fleets = await _fleetRepository.GetAllFleetsAsync();
            if (fleets.Count == 0)
            {
                HasNoFleet = true;
                IsEmpty = true;
                Recommendations = [];
                return;
            }

            HasNoFleet = false;
            var allShips = await _shipDataService.GetAllShipsAsync();
            var shipLookup = allShips.ToDictionary(s => s.Id);

            var ownedShips = new List<Ship>();
            foreach (var fleet in fleets)
            {
                var fleetShips = await _fleetRepository.GetFleetShipsAsync(fleet.Id);
                foreach (var fs in fleetShips)
                {
                    if (shipLookup.TryGetValue(fs.ShipId, out var ship))
                        ownedShips.Add(ship);
                }
            }

            if (ownedShips.Count == 0)
            {
                IsEmpty = true;
                Recommendations = [];
                return;
            }

            var recs = _recommendationService.GetRecommendations(ownedShips, allShips);
            Recommendations = new ObservableCollection<Recommendation>(recs);
            IsEmpty = recs.Count == 0;
        }
        finally
        {
            IsLoading = false;
        }
    }
}
