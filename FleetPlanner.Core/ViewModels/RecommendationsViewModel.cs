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

    [ObservableProperty]
    private ObservableCollection<Fleet> _fleets = [];

    [ObservableProperty]
    private Fleet? _selectedFleet;

    [ObservableProperty]
    private string _fleetSummary = string.Empty;

    public RecommendationsViewModel(
        IFleetRepository fleetRepository,
        IShipDataService shipDataService,
        IRecommendationService recommendationService)
    {
        _fleetRepository = fleetRepository;
        _shipDataService = shipDataService;
        _recommendationService = recommendationService;
    }

    partial void OnSelectedFleetChanged(Fleet? value)
    {
        if (value is not null)
            LoadRecommendationsCommand.Execute(null);
    }

    [RelayCommand]
    private async Task LoadRecommendationsAsync()
    {
        IsLoading = true;
        try
        {
            var fleets = await _fleetRepository.GetAllFleetsAsync();
            Fleets = new ObservableCollection<Fleet>(fleets);

            if (fleets.Count == 0)
            {
                HasNoFleet = true;
                IsEmpty = true;
                Recommendations = [];
                return;
            }

            HasNoFleet = false;

            if (SelectedFleet is null)
            {
                SelectedFleet = fleets[0];
                return; // OnSelectedFleetChanged will re-trigger
            }

            var fleet = SelectedFleet;
            FleetSummary = $"{fleet.PrimaryFocusEnum} fleet — {fleet.AvailableCrewCount} players — {fleet.OperatingScaleEnum} scale";

            var allShips = await _shipDataService.GetAllShipsAsync();
            var shipLookup = allShips.ToDictionary(s => s.Id);

            var fleetShips = await _fleetRepository.GetFleetShipsAsync(fleet.Id);
            var ownedShips = fleetShips
                .Where(fs => shipLookup.ContainsKey(fs.ShipId))
                .Select(fs => shipLookup[fs.ShipId])
                .ToList();

            if (ownedShips.Count == 0)
            {
                IsEmpty = true;
                Recommendations = [];
                return;
            }

            var recs = _recommendationService.GetRecommendations(fleet, ownedShips, allShips);
            Recommendations = new ObservableCollection<Recommendation>(recs);
            IsEmpty = recs.Count == 0;
        }
        finally
        {
            IsLoading = false;
        }
    }
}
