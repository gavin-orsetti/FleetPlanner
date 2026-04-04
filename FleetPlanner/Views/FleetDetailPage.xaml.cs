using FleetPlanner.ViewModels;

namespace FleetPlanner.Views;

/// <summary>
/// Code-behind for the Fleet Detail page — shows a single fleet and its ships.
/// <para>
/// <b>Detail page with inline editing:</b> This page displays a read-only view of a fleet's
/// properties and ship list, with an "Edit" toggle for inline name/description editing
/// and a "Settings" button for full fleet configuration via <see cref="FleetManagementPage"/>.
/// </para>
/// <para>
/// <b>SwipeView for ship removal:</b> Each ship card in the XAML uses a <c>SwipeView</c> with
/// a "Remove" swipe action that calls <see cref="FleetDetailViewModel.RemoveShipCommand"/>.
/// The <c>RelativeSource AncestorType</c> binding pattern reaches up from the <c>DataTemplate</c>
/// to the page's ViewModel, since the DataTemplate's context is a <c>FleetShipDisplay</c> item.
/// </para>
/// </summary>
/// <see href="https://learn.microsoft.com/en-us/dotnet/maui/user-interface/controls/swipeview"/>
public partial class FleetDetailPage : ContentPage
{
    private readonly FleetDetailViewModel _viewModel;

    /// <summary>
    /// Constructor — receives the ViewModel from DI, loads XAML, and sets the binding context.
    /// </summary>
    /// <param name="viewModel">The FleetDetail ViewModel injected by the DI container.</param>
    public FleetDetailPage(FleetDetailViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = viewModel;
    }

    /// <summary>
    /// Loads the fleet and its ships each time the page appears.
    /// Re-triggers on back-navigation from ShipBrowser (after adding a ship).
    /// </summary>
    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.LoadFleetCommand.ExecuteAsync(null);
    }
}
