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
/// ViewModel for the Fleet Detail page — displays a single fleet's properties and its ships.
/// <para>
/// <b>Navigation:</b> This is a "detail" page pushed onto the Shell navigation stack from
/// <see cref="FleetListViewModel"/>. The fleet ID is passed as a query parameter and
/// automatically deserialized into <see cref="FleetId"/> by the <c>[QueryProperty]</c> attribute.
/// </para>
/// <para>
/// <b>[QueryProperty] pattern:</b> Shell navigation passes data between pages via URI query strings.
/// <c>[QueryProperty(nameof(FleetId), "fleetId")]</c> tells Shell to set the <c>FleetId</c> property
/// from the <c>fleetId</c> query parameter (e.g., <c>GoToAsync("FleetDetailPage?fleetId=42")</c>).
/// The source generator creates the public property from the <c>[ObservableProperty]</c> field.
/// </para>
/// <para>
/// <b>Inline editing:</b> The page supports toggling between read-only and edit modes via
/// <see cref="IsEditing"/>. In edit mode, the user can change the fleet name and description
/// and save changes back to SQLite.
/// </para>
/// </summary>
/// <see href="https://learn.microsoft.com/en-us/dotnet/maui/fundamentals/shell/navigation#process-navigation-data-using-query-property-attributes"/>
/// <see href="https://learn.microsoft.com/en-us/dotnet/communitytoolkit/mvvm/"/>
[QueryProperty(nameof(FleetId), "fleetId")]
public partial class FleetDetailViewModel : ObservableObject
{
    /// <summary>For loading fleet data and managing fleet-ship associations.</summary>
    private readonly IFleetRepository _fleetRepository;

    /// <summary>For resolving FleetShip IDs to full Ship references (names, stats, prices).</summary>
    private readonly IShipDataService _shipDataService;

    /// <summary>
    /// The fleet ID passed via Shell navigation query parameter.
    /// Set automatically by <c>[QueryProperty]</c> before the page appears.
    /// </summary>
    [ObservableProperty]
    private int _fleetId;

    /// <summary>The loaded Fleet entity from SQLite.</summary>
    [ObservableProperty]
    private Fleet? _fleet;

    /// <summary>Editable fleet name — bound two-way to an Entry in the View.</summary>
    [ObservableProperty]
    private string _fleetName = string.Empty;

    /// <summary>Editable fleet description — bound two-way to an Editor in the View.</summary>
    [ObservableProperty]
    private string _description = string.Empty;

    /// <summary>
    /// Human-readable summary of the fleet's configuration (focus, crew, scale).
    /// Built from the Fleet's enum properties for display as a subtitle.
    /// </summary>
    [ObservableProperty]
    private string _fleetIntentSummary = string.Empty;

    /// <summary>
    /// The fleet's ships as display-ready objects. Uses <see cref="FleetShipDisplay"/>
    /// rather than raw <see cref="FleetShip"/> because the View needs resolved ship names,
    /// roles, and formatted strings that come from joining FleetShip with Ship.
    /// </summary>
    [ObservableProperty]
    private ObservableCollection<FleetShipDisplay> _ships = [];

    /// <summary>True while loading — drives a loading indicator in the View.</summary>
    [ObservableProperty]
    private bool _isLoading;

    /// <summary>True when the user has toggled inline editing mode.</summary>
    [ObservableProperty]
    private bool _isEditing;

    /// <summary>
    /// Constructor — receives dependencies from the DI container.
    /// </summary>
    /// <param name="fleetRepository">For fleet and fleet-ship CRUD operations.</param>
    /// <param name="shipDataService">For resolving ship IDs to Ship objects.</param>
    public FleetDetailViewModel(IFleetRepository fleetRepository, IShipDataService shipDataService)
    {
        _fleetRepository = fleetRepository;
        _shipDataService = shipDataService;
    }

    /// <summary>
    /// Loads the fleet and resolves all its ships into display-ready objects.
    /// <para>
    /// <b>Resolution pattern:</b> For each FleetShip record, we call
    /// <see cref="IShipDataService.GetShipAsync"/> to look up the full Ship reference
    /// (name, manufacturer, role, price, crew). This is an N+1 query pattern — acceptable
    /// here because fleet sizes are small (typically &lt;20 ships), and the ship data service
    /// reads from a local SQLite cache (not a remote API).
    /// </para>
    /// </summary>
    [RelayCommand]
    private async Task LoadFleetAsync()
    {
        IsLoading = true;
        try
        {
            Fleet = await _fleetRepository.GetFleetAsync(FleetId);
            if (Fleet is null)
                return;

            // Populate editable fields from the loaded fleet.
            FleetName = Fleet.Name;
            Description = Fleet.Description;
            FleetIntentSummary = $"{Fleet.PrimaryFocusEnum} fleet — {Fleet.AvailableCrewCount} players — {Fleet.OperatingScaleEnum} scale";

            // Resolve each FleetShip to a FleetShipDisplay with full ship details.
            var fleetShips = await _fleetRepository.GetFleetShipsAsync(FleetId);
            var displays = new List<FleetShipDisplay>();

            foreach (var fs in fleetShips)
            {
                var ship = await _shipDataService.GetShipAsync(fs.ShipId);
                displays.Add(new FleetShipDisplay
                {
                    FleetShipId = fs.Id,
                    ShipId = fs.ShipId,
                    Callsign = fs.Callsign,
                    ShipName = ship?.Name ?? "Unknown",
                    Manufacturer = ship?.Manufacturer ?? "Unknown",
                    Role = ship?.Role ?? "Unknown",
                    PriceUsd = ship?.PriceUsd ?? 0,
                    CrewMin = ship?.CrewMin ?? 0,
                    CrewMax = ship?.CrewMax ?? 0,
                    AcquisitionType = (AcquisitionType)fs.AcquisitionType,
                    PledgeStorePriceUsd = fs.PledgeStorePriceUsd
                });
            }

            Ships = new ObservableCollection<FleetShipDisplay>(displays);
        }
        finally
        {
            IsLoading = false;
        }
    }

    /// <summary>Toggles between read-only and inline-edit modes.</summary>
    [RelayCommand]
    private void ToggleEdit()
    {
        IsEditing = !IsEditing;
    }

    /// <summary>
    /// Saves the edited fleet name and description back to SQLite.
    /// <para>
    /// Only the name and description are editable on this page. For full fleet configuration
    /// (focus, scale, roles, crew), the user navigates to <see cref="FleetManagementViewModel"/>
    /// via <see cref="ManageFleetAsync"/>.
    /// </para>
    /// </summary>
    [RelayCommand]
    private async Task SaveFleetAsync()
    {
        if (Fleet is null)
            return;

        Fleet.Name = FleetName;
        Fleet.Description = Description;

        await _fleetRepository.SaveFleetAsync(Fleet);
        IsEditing = false;
    }

    /// <summary>
    /// Navigates to FleetManagementPage for full fleet configuration editing.
    /// </summary>
    [RelayCommand]
    private async Task ManageFleetAsync()
    {
        await Shell.Current.GoToAsync(nameof(FleetManagementPage), new Dictionary<string, object>
        {
            { QueryParameters.FleetId, FleetId }
        });
    }

    /// <summary>
    /// Navigates to the ShipBrowser in "select mode" to add a ship to this fleet.
    /// <para>
    /// <c>selectMode=true</c> tells <see cref="ShipBrowserViewModel"/> to show an "Add to Fleet"
    /// button instead of "View Details". <c>fleetId</c> tells it which fleet to add the ship to.
    /// </para>
    /// </summary>
    [RelayCommand]
    private async Task AddShipAsync()
    {
        await Shell.Current.GoToAsync(nameof(ShipBrowserPage), new Dictionary<string, object>
        {
            { QueryParameters.SelectMode, true },
            { QueryParameters.FleetId, FleetId }
        });
    }

    /// <summary>
    /// Removes a ship from the fleet by deleting the FleetShip join record.
    /// <para>
    /// After deleting from SQLite, we also remove the item from the observable collection
    /// so the UI updates immediately without a full reload.
    /// </para>
    /// </summary>
    /// <param name="display">The ship display object to remove.</param>
    [RelayCommand]
    private async Task RemoveShipAsync(FleetShipDisplay display)
    {
        if (display is null)
            return;

        await _fleetRepository.DeleteFleetShipAsync(display.FleetShipId);
        // Remove from the local collection for instant UI feedback.
        Ships.Remove(display);
    }

    /// <summary>
    /// Navigates back to the previous page in the Shell navigation stack.
    /// <para>
    /// <c>".."</c> is Shell's relative navigation syntax — it pops the current page
    /// off the navigation stack, similar to a browser's "back" button.
    /// </para>
    /// </summary>
    [RelayCommand]
    private async Task GoBackAsync()
    {
        await Shell.Current.GoToAsync("..");
    }
}

/// <summary>
/// A display-ready DTO that joins FleetShip (user data) with Ship (reference data).
/// <para>
/// <b>Why a separate class?</b> The View needs to show ship names, roles, and formatted strings
/// that come from the Ship catalogue, but the data source is FleetShip records from SQLite.
/// This class merges both into a single bindable object, avoiding complex multi-binding in XAML.
/// </para>
/// <para>
/// <b>Not a Model:</b> This is a ViewModel-layer DTO, not a database entity. It has no
/// SQLite attributes and is not persisted — it exists only for the duration of this page.
/// </para>
/// </summary>
public class FleetShipDisplay
{
    /// <summary>The FleetShip record's primary key — needed for delete operations.</summary>
    public int FleetShipId { get; set; }

    /// <summary>The Ship catalogue ID — matches <see cref="Ship.Id"/>.</summary>
    public int ShipId { get; set; }

    /// <summary>User-assigned callsign for this ship instance (e.g., "Alpha-1").</summary>
    public string Callsign { get; set; } = string.Empty;

    /// <summary>Ship name from the catalogue (e.g., "Anvil Carrack").</summary>
    public string ShipName { get; set; } = string.Empty;

    /// <summary>Ship manufacturer from the catalogue (e.g., "Anvil Aerospace").</summary>
    public string Manufacturer { get; set; } = string.Empty;

    /// <summary>Ship role from the catalogue (e.g., "Exploration", "Combat").</summary>
    public string Role { get; set; } = string.Empty;

    /// <summary>Ship price in USD from the catalogue.</summary>
    public decimal PriceUsd { get; set; }

    /// <summary>Minimum crew required to operate the ship.</summary>
    public int CrewMin { get; set; }

    /// <summary>Maximum crew the ship can carry.</summary>
    public int CrewMax { get; set; }

    /// <summary>How the user acquired this ship (real money vs in-game currency).</summary>
    public AcquisitionType AcquisitionType { get; set; }

    /// <summary>USD price paid if acquired via the pledge store; null if bought with aUEC.</summary>
    public decimal? PledgeStorePriceUsd { get; set; }

    /// <summary>Short badge text for the UI — "$USD" for pledged ships, "aUEC" for in-game purchases.</summary>
    public string AcquisitionBadge => AcquisitionType == AcquisitionType.RealMoney ? "$USD" : "aUEC";

    /// <summary>Formatted crew range — "3" if min==max, "1-3" otherwise.</summary>
    public string CrewDisplay => CrewMin == CrewMax ? $"{CrewMin}" : $"{CrewMin}-{CrewMax}";
}
