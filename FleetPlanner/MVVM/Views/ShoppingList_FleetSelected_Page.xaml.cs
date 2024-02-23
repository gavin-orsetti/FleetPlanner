using FleetPlanner.MVVM.ViewModels;

namespace FleetPlanner.MVVM.Views;

public partial class ShoppingList_FleetSelected_Page : ContentPage
{
    public ShoppingList_FleetSelected_Page( ShoppingListFleetSelectedViewModel viewModel )
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}