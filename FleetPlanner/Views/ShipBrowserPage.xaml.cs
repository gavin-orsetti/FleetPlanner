using FleetPlanner.ViewModels;

namespace FleetPlanner.Views;

public partial class ShipBrowserPage : ContentPage
{
    private readonly ShipBrowserViewModel _viewModel;

    public ShipBrowserPage(ShipBrowserViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.LoadShipsCommand.ExecuteAsync(null);
    }
}
