using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using FleetPlanner.Models;
using FleetPlanner.Services;

namespace FleetPlanner.ViewModels;

/// <summary>
/// ViewModel for the Ship Detail page — displays a single ship's full specifications.
/// <para>
/// <b>Simple ViewModel:</b> This is one of the simplest ViewModels in the app. It receives
/// a ship ID via Shell navigation, loads the ship from the cache, and exposes it to the View.
/// No editing, no CRUD — just a read-only detail display.
/// </para>
/// <para>
/// <b>Navigation flow:</b> The user arrives here from the ShipBrowser page. The ship ID is
/// passed as a query parameter: <c>GoToAsync("ShipDetailPage?shipId=123")</c>.
/// </para>
/// </summary>
/// <see href="https://learn.microsoft.com/en-us/dotnet/maui/fundamentals/shell/navigation#process-navigation-data-using-query-property-attributes"/>
[QueryProperty(nameof(ShipId), "shipId")]
public partial class ShipDetailViewModel : ObservableObject
{
    /// <summary>For loading the ship's full details from the cache.</summary>
    private readonly IShipDataService _shipDataService;

    /// <summary>
    /// The ship ID from Shell navigation query parameter.
    /// Set automatically by <c>[QueryProperty]</c> before the page appears.
    /// </summary>
    [ObservableProperty]
    private int _shipId;

    /// <summary>
    /// The loaded Ship object — bound to the View's labels and data displays.
    /// Null until <see cref="LoadShipAsync"/> completes.
    /// </summary>
    [ObservableProperty]
    private Ship? _ship;

    /// <summary>True while loading the ship data.</summary>
    [ObservableProperty]
    private bool _isLoading;

    /// <summary>
    /// Constructor — receives the ship data service from DI.
    /// </summary>
    /// <param name="shipDataService">For loading ship details from the local cache.</param>
    public ShipDetailViewModel(IShipDataService shipDataService)
    {
        _shipDataService = shipDataService;
    }

    /// <summary>
    /// Loads the ship by ID from the ship data service (local SQLite cache).
    /// <para>
    /// <see cref="IShipDataService.GetShipAsync"/> reads from the local cache — this is
    /// not a network call. The ship data was downloaded and cached when the user first
    /// opened the app or last refreshed from Settings.
    /// </para>
    /// </summary>
    [RelayCommand]
    private async Task LoadShipAsync()
    {
        IsLoading = true;
        try
        {
            Ship = await _shipDataService.GetShipAsync(ShipId);
        }
        finally
        {
            IsLoading = false;
        }
    }

    /// <summary>
    /// Navigates back to the previous page using Shell's relative ".." syntax.
    /// </summary>
    [RelayCommand]
    private async Task GoBackAsync()
    {
        await Shell.Current.GoToAsync("..");
    }
}
