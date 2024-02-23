using FleetPlanner.MVVM.ViewModels;

namespace FleetPlanner.MVVM.Views;

public partial class Fleet_Edit_Page : ContentPage
{
    public Fleet_Edit_Page( FleetViewModel_Edit viewModel )
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}