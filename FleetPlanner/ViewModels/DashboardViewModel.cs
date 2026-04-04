using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FleetPlanner.Models;
using FleetPlanner.Repositories;
using FleetPlanner.Services;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using SkiaSharp;

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
    private int _rolesCovered;

    [ObservableProperty]
    private double _avgCrewSize;

    [ObservableProperty]
    private int _pledgedShipCount;

    [ObservableProperty]
    private int _inGameShipCount;

    [ObservableProperty]
    private decimal _totalPledgeValueUsd;

    [ObservableProperty]
    private string _lastUpdated = "Never";

    [ObservableProperty]
    private ISeries[] _fleetCompositionSeries = [];

    [ObservableProperty]
    private ISeries[] _valueDistributionSeries = [];

    [ObservableProperty]
    private Axis[] _valueDistributionXAxes = [];

    [ObservableProperty]
    private Axis[] _valueDistributionYAxes = [];

    [ObservableProperty]
    private ObservableCollection<Fleet> _fleets = [];

    [ObservableProperty]
    private Fleet? _selectedFleet;

    public DashboardViewModel(IFleetRepository fleetRepository, IShipDataService shipDataService)
    {
        _fleetRepository = fleetRepository;
        _shipDataService = shipDataService;
    }

    partial void OnSelectedFleetChanged(Fleet? value)
    {
        if (value is not null)
            LoadDataCommand.Execute(null);
    }

    [RelayCommand]
    private async Task LoadDataAsync()
    {
        IsLoading = true;
        try
        {
            var fleets = await _fleetRepository.GetAllFleetsAsync();
            Fleets = new ObservableCollection<Fleet>(fleets);
            TotalFleets = fleets.Count;

            if (SelectedFleet is null && fleets.Count > 0)
            {
                SelectedFleet = fleets[0];
                return; // OnSelectedFleetChanged will re-trigger LoadData
            }

            var allShips = await _shipDataService.GetAllShipsAsync();
            var shipLookup = allShips.ToDictionary(s => s.Id);

            var fleetsToShow = SelectedFleet is not null ? [SelectedFleet] : fleets;
            var allFleetShips = new List<(FleetShip fs, Ship? ship)>();
            foreach (var fleet in fleetsToShow)
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

            // Acquisition breakdown
            PledgedShipCount = allFleetShips.Count(x => x.fs.AcquisitionType == (int)AcquisitionType.RealMoney);
            InGameShipCount = allFleetShips.Count(x => x.fs.AcquisitionType == (int)AcquisitionType.AUEC);
            TotalPledgeValueUsd = allFleetShips
                .Where(x => x.fs.AcquisitionType == (int)AcquisitionType.RealMoney && x.fs.PledgeStorePriceUsd.HasValue)
                .Sum(x => x.fs.PledgeStorePriceUsd!.Value);

            // Roles covered
            var coveredRoles = allFleetShips
                .Where(x => x.ship is not null && !string.IsNullOrWhiteSpace(x.ship!.Role))
                .Select(x => x.ship!.Role)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Count();
            RolesCovered = coveredRoles;

            // Avg crew
            var crewShips = allFleetShips.Where(x => x.ship is not null).ToList();
            AvgCrewSize = crewShips.Count > 0
                ? crewShips.Average(x => (double)x.ship!.CrewMin)
                : 0;

            var lastUpdated = await _shipDataService.GetLastUpdatedAsync();
            LastUpdated = lastUpdated?.ToString("g") ?? "Never";

            BuildFleetCompositionChart(allFleetShips);
            BuildValueDistributionChart(allFleetShips);
        }
        finally
        {
            IsLoading = false;
        }
    }

    private void BuildFleetCompositionChart(List<(FleetShip fs, Ship? ship)> fleetShips)
    {
        var roleGroups = fleetShips
            .Where(x => x.ship is not null)
            .GroupBy(x => x.ship!.Role ?? "Unknown")
            .Select(g => new PieSeries<int>
            {
                Name = g.Key,
                Values = [g.Count()],
                DataLabelsPaint = new SolidColorPaint(SKColors.White),
                DataLabelsSize = 12,
                DataLabelsPosition = LiveChartsCore.Measure.PolarLabelsPosition.Middle
            })
            .ToArray();

        FleetCompositionSeries = roleGroups;
    }

    private void BuildValueDistributionChart(List<(FleetShip fs, Ship? ship)> fleetShips)
    {
        var shipValues = fleetShips
            .Where(x => x.ship is not null && x.ship.PriceUsd > 0)
            .OrderByDescending(x => x.ship!.PriceUsd)
            .Take(10)
            .ToList();

        var labels = shipValues.Select(x => x.ship!.Name ?? "Unknown").ToArray();
        var values = shipValues.Select(x => (double)x.ship!.PriceUsd).ToArray();

        ValueDistributionSeries =
        [
            new ColumnSeries<double>
            {
                Name = "Value (USD)",
                Values = values,
                Fill = new SolidColorPaint(new SKColor(0x00, 0xD4, 0xFF)),
                DataLabelsPaint = new SolidColorPaint(SKColors.White),
                DataLabelsSize = 10
            }
        ];

        ValueDistributionXAxes =
        [
            new Axis
            {
                Labels = labels,
                LabelsRotation = 45,
                TextSize = 10
            }
        ];

        ValueDistributionYAxes =
        [
            new Axis
            {
                Name = "USD",
                TextSize = 10
            }
        ];
    }
}
