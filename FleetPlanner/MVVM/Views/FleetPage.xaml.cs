using FleetPlanner.MVVM.ViewModels;

namespace FleetPlanner.MVVM.Views;

public partial class FleetPage : ContentPage
{
    public FleetPage( FleetViewModel viewModel )
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}