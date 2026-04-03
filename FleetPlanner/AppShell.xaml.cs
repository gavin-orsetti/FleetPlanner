using FleetPlanner.Views;

namespace FleetPlanner;

public partial class AppShell : Shell
{
    public AppShell()
    {
        InitializeComponent();

        // Register detail routes — MAUI Shell resolves these via DI when navigating
        Routing.RegisterRoute(nameof(FleetDetailPage), typeof(FleetDetailPage));
        Routing.RegisterRoute(nameof(ShipDetailPage), typeof(ShipDetailPage));
    }
}
