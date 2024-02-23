using FleetPlanner.MVVM.ViewModels;

namespace FleetPlanner;

public partial class MainPage : ContentPage
{

    public MainPage( MainPageViewModel viewModel )
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}

