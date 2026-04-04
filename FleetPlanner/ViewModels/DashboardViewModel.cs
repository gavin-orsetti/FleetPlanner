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
/// ViewModel for the Dashboard page — aggregates fleet statistics and renders charts.
/// <para>
/// <b>Data flow:</b>
/// <list type="number">
///   <item>Load all fleets from the repository.</item>
///   <item>User selects a fleet (or auto-select the first one).</item>
///   <item>Resolve each FleetShip to its Ship reference via the ship catalogue.</item>
///   <item>Compute summary statistics (total value, roles covered, crew size, acquisition breakdown).</item>
///   <item>Build LiveCharts2 series for a pie chart (role composition) and a bar chart (top-10 value).</item>
/// </list>
/// </para>
/// <para>
/// <b>LiveCharts2 integration:</b> This ViewModel creates <see cref="ISeries"/> arrays that the View
/// binds to <c>&lt;lvc:PieChart&gt;</c> and <c>&lt;lvc:CartesianChart&gt;</c> controls.
/// LiveCharts2 uses SkiaSharp as its rendering backend, which is why we reference
/// <see cref="SolidColorPaint"/> and <see cref="SKColors"/> here.
/// </para>
/// </summary>
/// <see href="https://livecharts.dev/docs/maui/2.0.0-rc6/"/>
/// <see href="https://learn.microsoft.com/en-us/dotnet/communitytoolkit/mvvm/"/>
public partial class DashboardViewModel : ObservableObject
{
    /// <summary>For loading fleets and fleet-ship mappings from SQLite.</summary>
    private readonly IFleetRepository _fleetRepository;

    /// <summary>For loading the ship catalogue (names, stats, prices) from cache.</summary>
    private readonly IShipDataService _shipDataService;

    // ── UI state properties ──────────────────────────────────────────
    // Each [ObservableProperty] field generates a public property with INotifyPropertyChanged
    // support. E.g., _isLoading → public bool IsLoading { get; set; } with change notification.

    /// <summary>True while loading — drives the loading indicator in the View.</summary>
    [ObservableProperty]
    private bool _isLoading;

    // ── Aggregate statistics ─────────────────────────────────────────
    // These are computed from the resolved (FleetShip, Ship) pairs and displayed as KPI cards.

    /// <summary>Total number of ships across the selected fleet(s).</summary>
    [ObservableProperty]
    private int _totalShips;

    /// <summary>Total number of fleets the user has created.</summary>
    [ObservableProperty]
    private int _totalFleets;

    /// <summary>Sum of PriceUsd for all resolved ships in the selected fleet(s).</summary>
    [ObservableProperty]
    private decimal _totalFleetValue;

    /// <summary>Count of distinct ship roles (e.g., Combat, Mining) covered by the fleet.</summary>
    [ObservableProperty]
    private int _rolesCovered;

    /// <summary>Average minimum crew size across all ships in the fleet.</summary>
    [ObservableProperty]
    private double _avgCrewSize;

    // ── Acquisition breakdown ────────────────────────────────────────
    // Star Citizen ships can be acquired with real money (pledges) or in-game currency (aUEC).
    // These stats help the user understand their financial investment.

    /// <summary>Number of ships acquired via real-money pledge store purchases.</summary>
    [ObservableProperty]
    private int _pledgedShipCount;

    /// <summary>Number of ships acquired with in-game currency (aUEC).</summary>
    [ObservableProperty]
    private int _inGameShipCount;

    /// <summary>Total real-money (USD) spent on pledge store purchases.</summary>
    [ObservableProperty]
    private decimal _totalPledgeValueUsd;

    /// <summary>Human-readable timestamp of the last ship data cache refresh.</summary>
    [ObservableProperty]
    private string _lastUpdated = "Never";

    // ── Chart data ───────────────────────────────────────────────────
    // LiveCharts2 binds to ISeries[] arrays. When these properties change, the charts re-render.
    // See: https://livecharts.dev/docs/maui/2.0.0-rc6/

    /// <summary>Pie chart data — one slice per ship role, sized by ship count.</summary>
    [ObservableProperty]
    private ISeries[] _fleetCompositionSeries = [];

    /// <summary>Bar chart data — top 10 most valuable ships.</summary>
    [ObservableProperty]
    private ISeries[] _valueDistributionSeries = [];

    /// <summary>X-axis config for the value chart — ship names as labels, rotated 45°.</summary>
    [ObservableProperty]
    private Axis[] _valueDistributionXAxes = [];

    /// <summary>Y-axis config for the value chart — labelled "USD".</summary>
    [ObservableProperty]
    private Axis[] _valueDistributionYAxes = [];

    // ── Fleet picker ─────────────────────────────────────────────────

    /// <summary>All available fleets — populates the fleet picker in the View.</summary>
    [ObservableProperty]
    private ObservableCollection<Fleet> _fleets = [];

    /// <summary>
    /// The fleet currently selected in the picker. Changing this triggers
    /// <see cref="OnSelectedFleetChanged"/> which reloads all dashboard data.
    /// </summary>
    [ObservableProperty]
    private Fleet? _selectedFleet;

    /// <summary>
    /// Constructor — receives dependencies from the DI container.
    /// </summary>
    /// <param name="fleetRepository">For loading fleet and fleet-ship data from SQLite.</param>
    /// <param name="shipDataService">For loading the cached ship catalogue.</param>
    public DashboardViewModel(IFleetRepository fleetRepository, IShipDataService shipDataService)
    {
        _fleetRepository = fleetRepository;
        _shipDataService = shipDataService;
    }

    /// <summary>
    /// Called automatically when <see cref="SelectedFleet"/> changes.
    /// <para>
    /// This is a partial method hook generated by <c>[ObservableProperty]</c>. The naming convention
    /// is <c>On{PropertyName}Changed</c>. We use it to reload the dashboard whenever the user
    /// picks a different fleet from the dropdown.
    /// </para>
    /// </summary>
    /// <param name="value">The newly selected fleet, or null.</param>
    /// <see href="https://learn.microsoft.com/en-us/dotnet/communitytoolkit/mvvm/generators/observableproperty"/>
    partial void OnSelectedFleetChanged(Fleet? value)
    {
        if (value is not null)
            LoadDataCommand.Execute(null);
    }

    /// <summary>
    /// Main loading method — fetches fleets, resolves ships, computes stats, and builds charts.
    /// <para>
    /// <b>Tuple pattern:</b> Uses a list of <c>(FleetShip fs, Ship? ship)</c> tuples to pair
    /// each user-owned FleetShip with its resolved Ship reference. The Ship can be null if
    /// the ship ID doesn't exist in the catalogue (orphaned data).
    /// </para>
    /// </summary>
    [RelayCommand]
    private async Task LoadDataAsync()
    {
        IsLoading = true;
        try
        {
            var fleets = await _fleetRepository.GetAllFleetsAsync();
            Fleets = new ObservableCollection<Fleet>(fleets);
            TotalFleets = fleets.Count;

            // Auto-select the first fleet if none is selected.
            // Setting SelectedFleet triggers OnSelectedFleetChanged → LoadData re-entry,
            // so return early to avoid duplicate computation.
            if (SelectedFleet is null && fleets.Count > 0)
            {
                SelectedFleet = fleets[0];
                return; // OnSelectedFleetChanged will re-trigger LoadData
            }

            // Build a dictionary for O(1) ship lookups by ID.
            var allShips = await _shipDataService.GetAllShipsAsync();
            var shipLookup = allShips.ToDictionary(s => s.Id);

            // Resolve all FleetShips → (FleetShip, Ship?) tuples for the selected fleet.
            // C# 12 collection expression: [SelectedFleet] creates a single-element list.
            var fleetsToShow = SelectedFleet is not null ? [SelectedFleet] : fleets;
            var allFleetShips = new List<(FleetShip fs, Ship? ship)>();
            foreach (var fleet in fleetsToShow)
            {
                var fleetShips = await _fleetRepository.GetFleetShipsAsync(fleet.Id);
                foreach (var fs in fleetShips)
                {
                    // TryGetValue returns false and sets ship to null if not found.
                    shipLookup.TryGetValue(fs.ShipId, out var ship);
                    allFleetShips.Add((fs, ship));
                }
            }

            // ── Compute aggregate statistics ─────────────────────────────
            TotalShips = allFleetShips.Count;
            TotalFleetValue = allFleetShips
                .Where(x => x.ship is not null)
                .Sum(x => x.ship!.PriceUsd);

            // Acquisition breakdown: split ships by how they were acquired.
            // The int cast is needed because FleetShip.AcquisitionType is stored as int in SQLite.
            PledgedShipCount = allFleetShips.Count(x => x.fs.AcquisitionType == (int)AcquisitionType.RealMoney);
            InGameShipCount = allFleetShips.Count(x => x.fs.AcquisitionType == (int)AcquisitionType.AUEC);
            TotalPledgeValueUsd = allFleetShips
                .Where(x => x.fs.AcquisitionType == (int)AcquisitionType.RealMoney && x.fs.PledgeStorePriceUsd.HasValue)
                .Sum(x => x.fs.PledgeStorePriceUsd!.Value);

            // Count distinct roles — tells the user how many gameplay loops their fleet covers.
            var coveredRoles = allFleetShips
                .Where(x => x.ship is not null && !string.IsNullOrWhiteSpace(x.ship!.Role))
                .Select(x => x.ship!.Role)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Count();
            RolesCovered = coveredRoles;

            // Average minimum crew — helps gauge how many players are needed to fly the fleet.
            var crewShips = allFleetShips.Where(x => x.ship is not null).ToList();
            AvgCrewSize = crewShips.Count > 0
                ? crewShips.Average(x => (double)x.ship!.CrewMin)
                : 0;

            var lastUpdated = await _shipDataService.GetLastUpdatedAsync();
            LastUpdated = lastUpdated?.ToString("g") ?? "Never";

            // Build chart data from the resolved fleet ships.
            BuildFleetCompositionChart(allFleetShips);
            BuildValueDistributionChart(allFleetShips);
        }
        finally
        {
            IsLoading = false;
        }
    }

    /// <summary>
    /// Builds a pie chart showing fleet composition by ship role.
    /// <para>
    /// <b>LiveCharts2 PieSeries:</b> Each <see cref="PieSeries{T}"/> represents one slice.
    /// The <c>Values</c> array contains a single int (the count of ships with that role).
    /// LiveCharts automatically sizes each slice proportionally.
    /// </para>
    /// <para>
    /// <b>SolidColorPaint:</b> A SkiaSharp paint brush used by LiveCharts for rendering text
    /// and shapes on the SkiaSharp canvas. <c>SKColors.White</c> makes data labels readable
    /// on the coloured pie slices.
    /// </para>
    /// </summary>
    /// <param name="fleetShips">Resolved (FleetShip, Ship?) tuples to chart.</param>
    /// <see href="https://livecharts.dev/docs/maui/2.0.0-rc6/"/>
    private void BuildFleetCompositionChart(List<(FleetShip fs, Ship? ship)> fleetShips)
    {
        // Group ships by role (e.g., "Combat", "Mining", "Transport") and create one pie slice per group.
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

    /// <summary>
    /// Builds a bar chart showing the top 10 most valuable ships by USD price.
    /// <para>
    /// <b>ColumnSeries:</b> A vertical bar chart series. Each bar represents one ship,
    /// ordered by descending price. Only the top 10 are shown to keep the chart readable.
    /// </para>
    /// <para>
    /// <b>Axis configuration:</b> LiveCharts2 uses <see cref="Axis"/> objects to configure
    /// labels, rotation, and sizing. The X-axis uses ship names with 45° rotation to prevent
    /// overlap; the Y-axis is labelled "USD".
    /// </para>
    /// </summary>
    /// <param name="fleetShips">Resolved (FleetShip, Ship?) tuples to chart.</param>
    private void BuildValueDistributionChart(List<(FleetShip fs, Ship? ship)> fleetShips)
    {
        // Take the top 10 most expensive ships for the bar chart.
        var shipValues = fleetShips
            .Where(x => x.ship is not null && x.ship.PriceUsd > 0)
            .OrderByDescending(x => x.ship!.PriceUsd)
            .Take(10)
            .ToList();

        var labels = shipValues.Select(x => x.ship!.Name ?? "Unknown").ToArray();
        var values = shipValues.Select(x => (double)x.ship!.PriceUsd).ToArray();

        // SKColor(0x00, 0xD4, 0xFF) = cyan accent colour matching the app's theme.
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
