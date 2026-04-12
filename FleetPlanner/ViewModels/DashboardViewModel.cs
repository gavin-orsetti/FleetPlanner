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

/// <summary>
/// ViewModel for the Dashboard page — aggregates owned-ship statistics and renders
/// LiveCharts2 pie and bar charts showing fleet composition and value distribution.
///
/// <para><b>Page lifecycle:</b> <see cref="LoadDataCommand"/> is executed from
/// <c>DashboardPage.OnAppearing</c> on every navigation to the page (including
/// back-navigation from detail pages). This ensures statistics reflect the latest
/// data. The entire collection is re-resolved each time — there is no incremental update.</para>
///
/// <para><b>Data flow:</b> Loads owned ships from <see cref="IOwnedShipRepository"/>,
/// all catalogue ships from <see cref="IShipDataService"/>, and all groups from
/// <see cref="IUserFleetGroupRepository"/>. Resolves each OwnedShip to its catalogue
/// Ship via an ID lookup dictionary, then computes aggregates (counts, sums, averages)
/// and builds chart series.</para>
///
/// <para><b>Chart data:</b> <see cref="FleetCompositionSeries"/> (pie chart) groups
/// ships by <c>Ship.Role</c>. <see cref="ValueDistributionSeries"/> (bar chart) shows
/// the top 10 ships by <c>PriceUsd</c>. Both are rebuilt on every load.</para>
/// </summary>
public partial class DashboardViewModel : ObservableObject
{
    private readonly IOwnedShipRepository _ownedShipRepository;
    private readonly IUserFleetGroupRepository _groupRepository;
    private readonly IShipDataService _shipDataService;

    /// <summary>True while loading — drives the loading indicator.</summary>
    [ObservableProperty]
    private bool _isLoading;

    /// <summary>Total number of owned ships.</summary>
    [ObservableProperty]
    private int _totalShips;

    /// <summary>Total number of groups.</summary>
    [ObservableProperty]
    private int _totalGroups;

    /// <summary>Sum of AcquiredPriceUsd for real-money acquisitions.</summary>
    [ObservableProperty]
    private decimal _totalPledgeValueUsd;

    /// <summary>Sum of AcquiredPriceAuec for in-game acquisitions.</summary>
    [ObservableProperty]
    private long _totalAuecValue;

    /// <summary>Count of distinct ship roles covered.</summary>
    [ObservableProperty]
    private int _rolesCovered;

    /// <summary>Average minimum crew across all owned ships.</summary>
    [ObservableProperty]
    private double _avgCrewSize;

    /// <summary>Number of ships acquired via pledge store.</summary>
    [ObservableProperty]
    private int _pledgedShipCount;

    /// <summary>Number of ships acquired with in-game currency.</summary>
    [ObservableProperty]
    private int _inGameShipCount;

    /// <summary>Human-readable timestamp of the last cache refresh.</summary>
    [ObservableProperty]
    private string _lastUpdated = "Never";

    /// <summary>Pie chart data — one slice per ship role.</summary>
    [ObservableProperty]
    private ISeries[] _fleetCompositionSeries = [];

    /// <summary>Bar chart data — top 10 most valuable ships.</summary>
    [ObservableProperty]
    private ISeries[] _valueDistributionSeries = [];

    /// <summary>X-axis config for the value chart.</summary>
    [ObservableProperty]
    private Axis[] _valueDistributionXAxes = [];

    /// <summary>Y-axis config for the value chart.</summary>
    [ObservableProperty]
    private Axis[] _valueDistributionYAxes = [];

    /// <summary>
    /// Constructor — receives dependencies from the DI container.
    /// </summary>
    public DashboardViewModel(
        IOwnedShipRepository ownedShipRepository,
        IUserFleetGroupRepository groupRepository,
        IShipDataService shipDataService)
    {
        _ownedShipRepository = ownedShipRepository;
        _groupRepository = groupRepository;
        _shipDataService = shipDataService;
    }

    /// <summary>Main loading method — fetches owned ships, resolves catalogue data, computes stats, builds charts.</summary>
    [RelayCommand]
    private async Task LoadDataAsync()
    {
        IsLoading = true;
        try
        {
            var ownedShips = await _ownedShipRepository.GetAllOwnedShipsAsync();
            var groups = await _groupRepository.GetAllGroupsAsync();
            var allShips = await _shipDataService.GetAllShipsAsync();
            var shipLookup = allShips.ToDictionary(s => s.Id);

            TotalShips = ownedShips.Count;
            TotalGroups = groups.Count;

            // Resolve each OwnedShip to its catalogue Ship reference
            var resolved = ownedShips
                .Select(o => (owned: o, ship: shipLookup.GetValueOrDefault(o.ShipId)))
                .ToList();

            // Acquisition breakdown
            PledgedShipCount = ownedShips.Count(o => o.AcquisitionType == AcquisitionType.RealMoney);
            InGameShipCount = ownedShips.Count(o => o.AcquisitionType == AcquisitionType.AUEC);
            TotalPledgeValueUsd = ownedShips
                .Where(o => o.AcquisitionType == AcquisitionType.RealMoney && o.AcquiredPriceUsd.HasValue)
                .Sum(o => o.AcquiredPriceUsd!.Value);
            TotalAuecValue = ownedShips
                .Where(o => o.AcquiredPriceAuec.HasValue)
                .Sum(o => o.AcquiredPriceAuec!.Value);

            // Role coverage
            var coveredRoles = resolved
                .Where(x => x.ship is not null && !string.IsNullOrWhiteSpace(x.ship.Role))
                .Select(x => x.ship!.Role)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Count();
            RolesCovered = coveredRoles;

            // Average crew
            var crewShips = resolved.Where(x => x.ship is not null).ToList();
            AvgCrewSize = crewShips.Count > 0
                ? crewShips.Average(x => (double)x.ship!.CrewMin)
                : 0;

            var lastUpdated = await _shipDataService.GetLastUpdatedAsync();
            LastUpdated = lastUpdated?.ToString("g") ?? "Never";

            BuildFleetCompositionChart(resolved);
            BuildValueDistributionChart(resolved);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error loading dashboard: {ex}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    private void BuildFleetCompositionChart(List<(OwnedShip owned, Ship? ship)> resolved)
    {
        var roleGroups = resolved
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

    private void BuildValueDistributionChart(List<(OwnedShip owned, Ship? ship)> resolved)
    {
        var shipValues = resolved
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
