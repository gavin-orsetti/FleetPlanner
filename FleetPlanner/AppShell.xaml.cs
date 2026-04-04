using FleetPlanner.Views;

namespace FleetPlanner;

public partial class AppShell : Shell
{
    public AppShell()
    {
        InitializeComponent();
        Routing.RegisterRoute(nameof(FleetDetailPage), typeof(FleetDetailPage));
        Routing.RegisterRoute(nameof(ShipDetailPage), typeof(ShipDetailPage));
    }
}
