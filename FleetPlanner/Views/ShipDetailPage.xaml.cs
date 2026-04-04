using FleetPlanner.ViewModels;

namespace FleetPlanner.Views;

/// <summary>
/// Code-behind for the Ship Detail page — shows a single ship's full specifications.
/// <para>
/// This is a simple read-only detail page. The XAML displays the ship's name, manufacturer,
/// role, size, crew, cargo, prices (USD and aUEC), and description. No editing or CRUD
/// operations — just a data display pushed onto the navigation stack from the ShipBrowser.
/// </para>
/// </summary>
public partial class ShipDetailPage : ContentPage
{
    private readonly ShipDetailViewModel _viewModel;

    /// <summary>
    /// Constructor — receives the ViewModel from DI, loads XAML, and sets the binding context.
    /// </summary>
    /// <param name="viewModel">The ShipDetail ViewModel injected by the DI container.</param>
    public ShipDetailPage(ShipDetailViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = viewModel;
    }

    /// <summary>
    /// Loads the ship data each time the page appears. Reads from the local SQLite cache.
    /// </summary>
    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.LoadShipCommand.ExecuteAsync(null);
    }
}
