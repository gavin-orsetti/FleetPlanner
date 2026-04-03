using System.Collections.ObjectModel;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using FleetPlanner.Models;
using FleetPlanner.Repositories;
using FleetPlanner.Services;

using LiveChartsCore;
using LiveChartsCore.Measure;
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
    private string _lastUpdated = "Never";

    [ObservableProperty]
    private ObservableCollection<ISeries> _fleetCompositionSeries = [];

    [ObservableProperty]
    private ObservableCollection<ISeries> _valueDistributionSeries = [];

    [ObservableProperty]
    private ObservableCollection<ISeries> _roleCoverageSeries = [];

    [ObservableProperty]
    private ObservableCollection<Axis> _roleCoverageAngles = [];

    [ObservableProperty]
    private ObservableCollection<ISeries> _costBreakdownSeries = [];

    [ObservableProperty]
    private ObservableCollection<Axis> _costBreakdownXAxes = [];

    [ObservableProperty]
    private ObservableCollection<Axis> _costBreakdownYAxes = [];

    [ObservableProperty]
    private ObservableCollection<Axis> _valueDistributionXAxes = [];

    [ObservableProperty]
    private ObservableCollection<Axis> _valueDistributionYAxes = [];

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

            BuildFleetCompositionChart(allFleetShips);
            BuildValueDistributionChart(allFleetShips);
            BuildRoleCoverageChart(allFleetShips);
            BuildCostBreakdownChart(allFleetShips);
        }
        finally
        {
            IsLoading = false;
        }
    }

    private void BuildFleetCompositionChart(List<(FleetShip fs, Ship? ship)> fleetShips)
    {
        var byRole = fleetShips
            .Where(x => x.ship is not null)
            .GroupBy(x => string.IsNullOrWhiteSpace(x.ship!.Role) ? "Unknown" : x.ship.Role)
            .Select(g => new PieSeries<int>
            {
                Values = [g.Count()],
                Name = g.Key,
                DataLabelsPaint = new SolidColorPaint(SKColors.White),
                DataLabelsFormatter = p => $"{g.Key}: {g.Count()}"
            })
            .ToList();

        FleetCompositionSeries = new ObservableCollection<ISeries>(byRole);
    }

    private void BuildValueDistributionChart(List<(FleetShip fs, Ship? ship)> fleetShips)
    {
        var items = fleetShips
            .Where(x => x.ship is not null && x.ship.PriceUsd > 0)
            .Select(x => new { x.ship!.Name, x.ship.PriceUsd })
            .OrderByDescending(x => x.PriceUsd)
            .Take(10)
            .ToList();

        var series = new ColumnSeries<decimal>
        {
            Values = items.Select(x => x.PriceUsd).ToList(),
            Name = "Value (USD)"
        };

        ValueDistributionSeries = new ObservableCollection<ISeries> { series };
        ValueDistributionXAxes =
        [
            new Axis { Labels = items.Select(x => x.Name).ToList(), LabelsRotation = 45 }
        ];
        ValueDistributionYAxes =
        [
            new Axis { Name = "USD", Labeler = v => $"${v:N0}" }
        ];
    }

    private void BuildRoleCoverageChart(List<(FleetShip fs, Ship? ship)> fleetShips)
    {
        var roles = new[] { "Combat", "Hauling", "Mining", "Medical", "Exploration", "Salvage", "Support", "Recon" };
        var roleMappings = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
        {
            ["Combat"] = ["Combat", "Fighter", "Bomber", "Dropship"],
            ["Hauling"] = ["Transport", "Hauling", "Freight"],
            ["Mining"] = ["Mining"],
            ["Medical"] = ["Medical"],
            ["Exploration"] = ["Exploration", "Pathfinder", "Touring"],
            ["Salvage"] = ["Salvage"],
            ["Support"] = ["Refueling", "Repair"],
            ["Recon"] = ["Reconnaissance", "Stealth", "Data"]
        };

        var coverage = roles.Select(role =>
        {
            var mappedRoles = roleMappings.GetValueOrDefault(role, [role]);
            return (double)fleetShips.Count(x =>
                x.ship is not null &&
                mappedRoles.Any(r => x.ship.Role?.Contains(r, StringComparison.OrdinalIgnoreCase) == true));
        }).ToList();

        var series = new PolarLineSeries<double>
        {
            Values = coverage,
            Name = "Ships",
            GeometrySize = 10,
            LineSmoothness = 0,
            Fill = new SolidColorPaint(SKColors.CornflowerBlue.WithAlpha(80))
        };

        RoleCoverageSeries = new ObservableCollection<ISeries> { series };
        RoleCoverageAngles =
        [
            new PolarAxis
            {
                Labels = roles.ToList(),
                MinStep = 1
            }
        ];
    }

    private void BuildCostBreakdownChart(List<(FleetShip fs, Ship? ship)> fleetShips)
    {
        var byManufacturer = fleetShips
            .Where(x => x.ship is not null && x.ship.PriceUsd > 0)
            .GroupBy(x => string.IsNullOrWhiteSpace(x.ship!.Manufacturer) ? "Unknown" : x.ship.Manufacturer)
            .Select(g => new PieSeries<decimal>
            {
                Values = [g.Sum(x => x.ship!.PriceUsd)],
                Name = g.Key,
                DataLabelsFormatter = p => $"{g.Key}: ${g.Sum(x => x.ship!.PriceUsd):N0}"
            })
            .ToList();

        CostBreakdownSeries = new ObservableCollection<ISeries>(byManufacturer);
    }
}
