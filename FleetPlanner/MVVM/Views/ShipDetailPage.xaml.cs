using FleetPlanner.MVVM.ViewModels;

namespace FleetPlanner.MVVM.Views;

public partial class ShipDetailPage : ContentPage
{
    public ShipDetailPage( ViewShipDetailViewModel viewModel )
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}