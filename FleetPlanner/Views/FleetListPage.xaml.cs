using FleetPlanner.ViewModels;

namespace FleetPlanner.Views;

public partial class FleetListPage : ContentPage
{
    private readonly FleetListViewModel _viewModel;

    public FleetListPage(FleetListViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.LoadFleetsCommand.ExecuteAsync(null);
    }
}
