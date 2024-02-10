using FleetPlanner.MVVM.ViewModels;

namespace FleetPlanner.MVVM.Views;

public partial class ShoppingListPage : ContentPage
{
    public ShoppingListPage( ShoppingListViewModel viewModel )
    {
        InitializeComponent();
        BindingContext = viewModel;
        viewModel.PopulateCommand.ExecuteAsync();
    }
}