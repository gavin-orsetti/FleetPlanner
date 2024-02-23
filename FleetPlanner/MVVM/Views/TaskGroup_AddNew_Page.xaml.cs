using FleetPlanner.MVVM.ViewModels;

namespace FleetPlanner.MVVM.Views;

public partial class TaskGroup_AddNew_Page : ContentPage
{
    public TaskGroup_AddNew_Page( NewTaskGroupViewModel viewModel )
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}