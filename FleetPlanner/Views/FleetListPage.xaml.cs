using FleetPlanner.ViewModels;

namespace FleetPlanner.Views;

/// <summary>
/// Code-behind for the Fleet List page — displays all user-created fleets.
/// <para>
/// <b>List→Detail navigation:</b> This is a master/list page. Tapping a fleet item navigates
/// to <see cref="FleetDetailPage"/>; the "+" button navigates to <see cref="FleetManagementPage"/>
/// in create mode. Swipe-to-delete triggers <see cref="FleetListViewModel.DeleteFleetCommand"/>.
/// </para>
/// <para>
/// <b>RefreshView binding:</b> The XAML wraps the CollectionView in a <c>RefreshView</c>
/// with <c>Command="{Binding LoadFleetsCommand}"</c>, enabling pull-to-refresh on mobile.
/// </para>
/// </summary>
/// <see href="https://learn.microsoft.com/en-us/dotnet/maui/user-interface/controls/refreshview"/>
public partial class FleetListPage : ContentPage
{
    private readonly FleetListViewModel _viewModel;

    /// <summary>
    /// Constructor — receives the ViewModel from DI, loads XAML, and sets the binding context.
    /// </summary>
    /// <param name="viewModel">The FleetList ViewModel injected by the DI container.</param>
    public FleetListPage(FleetListViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = viewModel;
    }

    /// <summary>
    /// Reloads the fleet list each time the page appears — ensures fresh data after
    /// creating, editing, or deleting a fleet on a sub-page.
    /// </summary>
    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.LoadFleetsCommand.ExecuteAsync(null);
    }
}
