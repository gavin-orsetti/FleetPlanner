using FleetPlanner.ViewModels;

namespace FleetPlanner.Views;

/// <summary>
/// Code-behind for the Group Detail page — shows a group's doctrine tags and member ships.
/// </summary>
public partial class GroupDetailPage : ContentPage
{
    private readonly GroupDetailViewModel _viewModel;

    /// <summary>
    /// Constructor — receives the ViewModel from DI, loads XAML, and sets the binding context.
    /// </summary>
    public GroupDetailPage(GroupDetailViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = viewModel;
    }

    /// <summary>Loads the group data when the page appears.</summary>
    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.LoadGroupCommand.ExecuteAsync(null);
    }
}
