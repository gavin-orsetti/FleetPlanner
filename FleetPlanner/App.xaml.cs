using UraniumUI;
using UraniumUI.Material;

namespace FleetPlanner;

public partial class App : Application
{
    public App()
    {
        InitializeComponent();


        MainPage = new AppShell();
    }
}
