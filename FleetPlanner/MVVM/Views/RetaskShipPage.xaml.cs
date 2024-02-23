using FleetPlanner.MVVM.ViewModels;

namespace FleetPlanner.MVVM.Views;

public partial class RetaskShipPage : ContentPage
{
    public RetaskShipPage( ReTaskViewModel viewModel )
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}