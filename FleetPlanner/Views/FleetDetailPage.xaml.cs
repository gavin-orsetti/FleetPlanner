using FleetPlanner.ViewModels;

namespace FleetPlanner.Views;

public partial class FleetDetailPage : ContentPage
{
    private readonly FleetDetailViewModel _viewModel;

    public FleetDetailPage(FleetDetailViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.LoadFleetCommand.ExecuteAsync(null);
    }
}
