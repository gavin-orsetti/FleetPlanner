using FleetPlanner.MVVM.ViewModels;

namespace FleetPlanner.MVVM.Views;

public partial class ShipDetail_Edit_Page : ContentPage
{
    public ShipDetail_Edit_Page( ShipDetailViewModel_Edit viewModel )
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}