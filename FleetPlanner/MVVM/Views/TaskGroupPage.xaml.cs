using FleetPlanner.MVVM.ViewModels;

namespace FleetPlanner.MVVM.Views;

public partial class TaskGroupPage : ContentPage
{
    public TaskGroupPage( ViewTaskGroupViewModel viewModel )
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}