using FleetPlanner.Views;

namespace FleetPlanner;

public partial class AppShell : Shell
{
    public AppShell()
    {
        InitializeComponent();

        // Register detail routes for navigation
        Routing.RegisterRoute("FleetDetailPage", typeof(FleetDetailPage));
        Routing.RegisterRoute("ShipDetailPage", typeof(Views.ShipDetailPage));
        Routing.RegisterRoute("ShipBrowserPage", typeof(ShipBrowserPage));

        // Match system theme
        if (Application.Current is not null)
            Application.Current.UserAppTheme = Application.Current.RequestedTheme;
    }
}
