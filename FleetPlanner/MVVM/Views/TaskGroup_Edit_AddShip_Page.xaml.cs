using FleetPlanner.MVVM.ViewModels;

namespace FleetPlanner.MVVM.Views;

public partial class TaskGroup_Edit_AddShip_Page : ContentPage
{
    public TaskGroup_Edit_AddShip_Page( TaskGroupViewModel_Edit_AddShips viewModel )
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}