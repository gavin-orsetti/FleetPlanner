using FleetPlanner.ViewModels;

namespace FleetPlanner.Views;

/// <summary>
/// Code-behind for the Ship Browser page — searchable/filterable catalogue of all ships.
/// <para>
/// <b>Dual mode (browse vs. select):</b> When navigated to from the FleetDetail page with
/// <c>selectMode=true&amp;fleetId=42</c>, tapping a ship adds it to the fleet. In normal browse
/// mode, tapping navigates to <see cref="ShipDetailPage"/>.
/// </para>
/// <para>
/// <b>Real-time filtering:</b> The XAML binds a <c>SearchBar</c> and three <c>Picker</c>
/// controls to the ViewModel's filter properties. As the user types or selects filters,
/// the <c>[ObservableProperty]</c> partial method hooks in <see cref="ShipBrowserViewModel"/>
/// trigger immediate re-filtering of the ship list.
/// </para>
/// <para>
/// <b>Offline banner:</b> If the ship data service detected it's serving cached (offline) data,
/// the ViewModel's <c>IsOffline</c> flag shows a warning banner at the top of the page.
/// </para>
/// </summary>
/// <see href="https://learn.microsoft.com/en-us/dotnet/maui/user-interface/controls/searchbar"/>
public partial class ShipBrowserPage : ContentPage
{
    private readonly ShipBrowserViewModel _viewModel;

    /// <summary>
    /// Constructor — receives the ViewModel from DI, loads XAML, and sets the binding context.
    /// </summary>
    /// <param name="viewModel">The ShipBrowser ViewModel injected by the DI container.</param>
    public ShipBrowserPage(ShipBrowserViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = viewModel;
    }

    /// <summary>
    /// Loads the ship catalogue each time the page appears.
    /// The first load may trigger an API call; subsequent loads use the cached data.
    /// </summary>
    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.LoadShipsCommand.ExecuteAsync(null);
    }
}
