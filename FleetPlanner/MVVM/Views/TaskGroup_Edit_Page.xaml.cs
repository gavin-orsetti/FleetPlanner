using FleetPlanner.MVVM.ViewModels;

namespace FleetPlanner.MVVM.Views;

public partial class TaskGroup_Edit_Page : ContentPage
{
    public TaskGroup_Edit_Page( TaskGroupViewModel_Edit viewModel )
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}