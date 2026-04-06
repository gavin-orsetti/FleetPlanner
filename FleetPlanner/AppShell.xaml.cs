using FleetPlanner.Views;

namespace FleetPlanner;

/// <summary>
/// Code-behind for <c>AppShell.xaml</c> — the app's navigation shell.
/// Registers explicit routes for detail/sub-pages that are pushed onto the nav stack.
/// </summary>
public partial class AppShell : Shell
{
    /// <summary>
    /// Constructor — loads the XAML and registers detail-page routes for Shell navigation.
    /// </summary>
    public AppShell()
    {
        InitializeComponent();

        // Detail/sub-pages navigated to programmatically
        Routing.RegisterRoute(nameof(ShipDetailPage), typeof(ShipDetailPage));
        Routing.RegisterRoute(nameof(OwnedShipEditorPage), typeof(OwnedShipEditorPage));
        Routing.RegisterRoute(nameof(GroupDetailPage), typeof(GroupDetailPage));
    }
}
