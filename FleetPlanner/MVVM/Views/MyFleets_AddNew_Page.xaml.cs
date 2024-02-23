using FleetPlanner.MVVM.ViewModels;

namespace FleetPlanner.MVVM.Views;

public partial class MyFleets_AddNew_Page : ContentPage
{
    public MyFleets_AddNew_Page( NewFleetViewModel viewModel )
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}