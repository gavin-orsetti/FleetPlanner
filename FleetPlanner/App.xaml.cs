using FleetPlanner.MVVM.ViewModels;

using UraniumUI;
using UraniumUI.Material;

namespace FleetPlanner;

public partial class App : Application
{
    public App( MainPageViewModel viewModel )
    {
        InitializeComponent();


        MainPage = new AppShell( viewModel );
    }
}
