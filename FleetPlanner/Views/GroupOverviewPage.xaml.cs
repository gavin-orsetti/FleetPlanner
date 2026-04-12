using FleetPlanner.ViewModels;

namespace FleetPlanner.Views;

/// <summary>
/// Code-behind for the Group Overview page — lists all user-created fleet groups.
/// </summary>
public partial class GroupOverviewPage : ContentPage
{
    private readonly GroupOverviewViewModel _viewModel;

    /// <summary>
    /// Constructor — receives the ViewModel from DI, loads XAML, and sets the binding context.
    /// </summary>
    public GroupOverviewPage(GroupOverviewViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = viewModel;
    }

    /// <summary>Loads groups each time the page appears.</summary>
    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.LoadGroupsCommand.ExecuteAsync(null);
    }
}
