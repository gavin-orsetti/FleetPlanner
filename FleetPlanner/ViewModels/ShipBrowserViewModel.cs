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
/// ViewModel for the Ship Browser page — lists all known ships with search, filter, and selection support.
/// <para>
/// <b>MVVM pattern:</b> This ViewModel holds the state and logic for the Ship Browser view.
/// The View (XAML) binds to properties and commands defined here. The ViewModel never references
/// the View directly — communication flows through data binding and commands.
/// </para>
/// <para>
/// <b>CommunityToolkit.Mvvm source generators:</b> This class uses two key source generators:
/// <list type="bullet">
///   <item><c>[ObservableProperty]</c> — generates a public property with <c>INotifyPropertyChanged</c>
///     notification from a private backing field. E.g., <c>_isLoading</c> generates <c>IsLoading</c>
///     with automatic change notification. The field must follow the <c>_camelCase</c> naming convention.</item>
///   <item><c>[RelayCommand]</c> — generates an <c>ICommand</c> property from an async method.
///     E.g., <c>LoadShipsAsync()</c> generates <c>LoadShipsCommand</c> that the View can bind to.</item>
/// </list>
/// The class must be <c>partial</c> because the source generators add the generated code in a separate partial file.
/// </para>
/// <para>
/// <b>QueryProperty attributes:</b> These tell MAUI Shell navigation to populate properties from
/// URL query parameters. When navigating to <c>"ShipBrowserPage?selectMode=true&amp;fleetId=3"</c>,
/// Shell automatically sets <c>SelectMode = true</c> and <c>FleetId = 3</c>.
/// </para>
/// </summary>
/// <see href="https://learn.microsoft.com/en-us/dotnet/communitytoolkit/mvvm/generators/observableproperty"/>
/// <see href="https://learn.microsoft.com/en-us/dotnet/communitytoolkit/mvvm/generators/relaycommand"/>
/// <see href="https://learn.microsoft.com/en-us/dotnet/maui/fundamentals/shell/navigation"/>
[QueryProperty(nameof(SelectMode), "selectMode")]
[QueryProperty(nameof(FleetId), "fleetId")]
public partial class ShipBrowserViewModel : ObservableObject
{
    /// <summary>Service for fetching/caching ship data (injected via DI constructor).</summary>
    private readonly IShipDataService _shipDataService;

    /// <summary>Repository for fleet/fleet-ship persistence (needed for "add ship to fleet" in select mode).</summary>
    private readonly IFleetRepository _fleetRepository;

    /// <summary>
    /// Master list of all ships — kept in memory for fast client-side filtering.
    /// <see cref="Ships"/> is the filtered subset displayed in the UI.
    /// </summary>
    private List<Ship> _allShips = [];

    // ── Observable properties ─────────────────────────────────────────
    // Each [ObservableProperty] field generates a public property that raises
    // PropertyChanged when set. The View binds to the generated PascalCase names.

    /// <summary>The filtered list of ships currently displayed in the CollectionView.</summary>
    [ObservableProperty]
    private ObservableCollection<Ship> _ships = [];

    /// <summary>True while the initial load is in progress — drives a loading spinner in the View.</summary>
    [ObservableProperty]
    private bool _isLoading;

    /// <summary>True while a pull-to-refresh is in progress — drives the RefreshView indicator.</summary>
    [ObservableProperty]
    private bool _isRefreshing;

    /// <summary>Text entered in the search bar — filters by ship name or manufacturer.</summary>
    [ObservableProperty]
    private string _searchText = string.Empty;

    /// <summary>Currently selected role filter (e.g., "Combat", "Mining", or "All").</summary>
    [ObservableProperty]
    private string _selectedRole = "All";

    /// <summary>Currently selected manufacturer filter.</summary>
    [ObservableProperty]
    private string _selectedManufacturer = "All";

    /// <summary>Currently selected size filter.</summary>
    [ObservableProperty]
    private string _selectedSize = "All";

    /// <summary>Available role options for the filter picker, populated from the ship data.</summary>
    [ObservableProperty]
    private ObservableCollection<string> _roles = ["All"];

    /// <summary>Available manufacturer options for the filter picker.</summary>
    [ObservableProperty]
    private ObservableCollection<string> _manufacturers = ["All"];

    /// <summary>Available size options for the filter picker.</summary>
    [ObservableProperty]
    private ObservableCollection<string> _sizes = ["All"];

    /// <summary>
    /// When true, tapping a ship adds it to a fleet instead of navigating to the detail page.
    /// Set via Shell query parameter from the Fleet Management page.
    /// </summary>
    [ObservableProperty]
    private bool _selectMode;

    /// <summary>
    /// The fleet to add a ship to when in select mode. Set via Shell query parameter.
    /// </summary>
    [ObservableProperty]
    private int _fleetId;

    /// <summary>Human-readable string showing when ship data was last refreshed from the API.</summary>
    [ObservableProperty]
    private string _lastUpdated = "Never";

    /// <summary>True if the last load/refresh attempt failed due to network issues.</summary>
    [ObservableProperty]
    private bool _isOffline;

    /// <summary>
    /// Constructor — receives dependencies from the DI container.
    /// <para>
    /// In MAUI, ViewModels are registered in <c>MauiProgram.cs</c> and the DI container
    /// automatically resolves their constructor parameters. This is "constructor injection" —
    /// the preferred DI pattern because it makes dependencies explicit and immutable.
    /// </para>
    /// </summary>
    /// <param name="shipDataService">For loading the ship catalogue.</param>
    /// <param name="fleetRepository">For saving a ship to a fleet in select mode.</param>
    /// <see href="https://learn.microsoft.com/en-us/dotnet/maui/fundamentals/dependency-injection"/>
    public ShipBrowserViewModel(IShipDataService shipDataService, IFleetRepository fleetRepository)
    {
        _shipDataService = shipDataService;
        _fleetRepository = fleetRepository;
    }

    /// <summary>
    /// Loads the ship catalogue from the cache/API. Bound to a command that the View
    /// triggers on page appearance (typically via <c>Loaded</c> event or <c>OnAppearing</c>).
    /// <para>
    /// <c>[RelayCommand]</c> generates a property called <c>LoadShipsCommand</c> of type
    /// <c>IAsyncRelayCommand</c>. The generated command automatically handles:
    /// <list type="bullet">
    ///   <item>Disabling the command while the async operation is running (prevents double-taps).</item>
    ///   <item>Propagating exceptions to the command's <c>ExecutionTask</c> for error handling.</item>
    /// </list>
    /// </para>
    /// </summary>
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

    /// <summary>
    /// Force-refreshes ship data from the API (bypassing cache). Triggered by pull-to-refresh.
    /// </summary>
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

    // ── Partial methods generated by [ObservableProperty] ─────────────
    // When the source generator creates a property from a field, it also generates
    // optional partial methods: On{PropertyName}Changing and On{PropertyName}Changed.
    // Implementing these lets us react to property changes without manual event wiring.
    // Here, any filter change re-applies all filters to update the displayed list.

    /// <summary>Re-applies filters whenever the search text changes.</summary>
    partial void OnSearchTextChanged(string value) => ApplyFilters();

    /// <summary>Re-applies filters whenever the selected role changes.</summary>
    partial void OnSelectedRoleChanged(string value) => ApplyFilters();

    /// <summary>Re-applies filters whenever the selected manufacturer changes.</summary>
    partial void OnSelectedManufacturerChanged(string value) => ApplyFilters();

    /// <summary>Re-applies filters whenever the selected size changes.</summary>
    partial void OnSelectedSizeChanged(string value) => ApplyFilters();

    /// <summary>
    /// Extracts distinct values from the loaded ship data to populate the filter pickers.
    /// Each picker gets "All" as the first option, plus the unique values from the data.
    /// <para>
    /// The <c>[.. roleList]</c> syntax is the C# 12 "spread" operator — it expands the list
    /// into the collection initialiser, equivalent to <c>new[] { "All" }.Concat(roleList)</c>.
    /// </para>
    /// </summary>
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

    /// <summary>
    /// Applies all active filters (search, role, manufacturer, size) to <see cref="_allShips"/>
    /// and updates <see cref="Ships"/> with the result.
    /// <para>
    /// <b>Performance note:</b> Filtering is done in-memory via LINQ because the ship
    /// catalogue is small (~700 ships). For larger datasets, you'd want server-side
    /// filtering or SQLite WHERE clauses.
    /// </para>
    /// <para>
    /// <b>LINQ chain:</b> Each <c>.Where()</c> lazily adds a filter predicate. Nothing
    /// executes until <c>.ToList()</c> materialises the result. This is efficient — only
    /// one pass over the data regardless of how many filters are active.
    /// </para>
    /// </summary>
    private void ApplyFilters()
    {
        // Start with all ships and progressively narrow down.
        var filtered = _allShips.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(SearchText))
        {
            // Search matches against both ship name and manufacturer (case-insensitive).
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

        // Replace the entire ObservableCollection — this triggers a single UI refresh.
        // (Alternatively, you could Clear+AddRange, but replacing is simpler.)
        Ships = new ObservableCollection<Ship>(filtered.ToList());
    }

    /// <summary>
    /// Handles tapping on a ship in the list. Behaviour depends on <see cref="SelectMode"/>:
    /// <list type="bullet">
    ///   <item><b>Select mode ON:</b> Creates a new <see cref="FleetShip"/> linking this ship to the fleet,
    ///     saves it to the database, then navigates back to the previous page (<c>".."</c> in Shell).</item>
    ///   <item><b>Select mode OFF:</b> Navigates to the Ship Detail page to show full ship info.</item>
    /// </list>
    /// <para>
    /// <c>Shell.Current.GoToAsync("..")</c> pops the current page off the navigation stack,
    /// returning to wherever the user came from. This is the Shell equivalent of "back".
    /// </para>
    /// </summary>
    /// <param name="ship">The tapped ship. May be null if the selection binding fires with no item.</param>
    /// <see href="https://learn.microsoft.com/en-us/dotnet/maui/fundamentals/shell/navigation"/>
    [RelayCommand]
    private async Task SelectShipAsync(Ship ship)
    {
        if (ship is null)
            return;

        if (SelectMode && FleetId > 0)
        {
            // SELECT MODE: Add this ship to the fleet and go back.
            var fleetShip = new FleetShip
            {
                FleetId = FleetId,
                ShipId = ship.Id,
                Callsign = ship.Name  // Default callsign to ship name; user can rename later.
            };
            await _fleetRepository.SaveFleetShipAsync(fleetShip);
            await Shell.Current.GoToAsync("..");
        }
        else
        {
            // BROWSE MODE: Navigate to the detail page with the ship's ID as a query parameter.
            await Shell.Current.GoToAsync(nameof(ShipDetailPage), new Dictionary<string, object>
            {
                { QueryParameters.ShipId, ship.Id }
            });
        }
    }

    /// <summary>
    /// Prompts the user for a quantity then adds that many copies of the given
    /// ship to the current fleet. Each copy is a separate FleetShip record,
    /// which allows them to have different acquisition types or notes later.
    /// </summary>
    [RelayCommand]
    private async Task AddShipToFleetAsync(Ship ship)
    {
        if (ship is null || FleetId <= 0)
            return;

        var input = await Shell.Current.DisplayPromptAsync(
            title: $"Add {ship.Name}",
            message: "How many would you like to add to your fleet?",
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

        var fleetShips = Enumerable.Range(0, quantity).Select(_ => new FleetShip
        {
            FleetId         = FleetId,
            ShipId          = ship.Id,
            Callsign        = ship.Name,
            AcquisitionType = (int)Models.AcquisitionType.AUEC
        });

        foreach (var fleetShip in fleetShips)
            await _fleetRepository.SaveFleetShipAsync(fleetShip);

        if (SelectMode)
        {
            await Shell.Current.GoToAsync("..");
        }
        else
        {
            var message = quantity == 1
                ? $"{ship.Name} has been added to your fleet."
                : $"{quantity}x {ship.Name} have been added to your fleet.";

            await Shell.Current.DisplayAlert("Added", message, "OK");
        }
    }
}
