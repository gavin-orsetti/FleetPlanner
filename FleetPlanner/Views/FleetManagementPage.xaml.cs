using FleetPlanner.ViewModels;

namespace FleetPlanner.Views;

public partial class FleetManagementPage : ContentPage
{
    public FleetManagementPage(FleetManagementViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        if (BindingContext is FleetManagementViewModel vm)
            vm.LoadCommand.Execute(null);
    }
}
