using FleetPlanner.ViewModels;

namespace FleetPlanner.Views;

public partial class ShipDetailPage : ContentPage
{
    private readonly ShipDetailViewModel _viewModel;

    public ShipDetailPage(ShipDetailViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.LoadShipCommand.ExecuteAsync(null);
    }
}
