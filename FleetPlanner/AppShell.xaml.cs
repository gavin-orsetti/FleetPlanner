using FleetPlanner.Views;

namespace FleetPlanner;

/// <summary>
/// Code-behind for <c>AppShell.xaml</c> — the app's navigation shell.
/// <para>
/// <b>MAUI Shell navigation:</b> Shell provides a URI-based navigation model. Pages can be
/// navigated to using <c>Shell.Current.GoToAsync("PageRoute")</c> with optional query parameters.
/// There are two kinds of routes:
/// <list type="bullet">
///   <item><b>Implicit routes</b> — pages declared in <c>AppShell.xaml</c> as <c>&lt;ShellContent&gt;</c>
///     are automatically registered with the route specified in their <c>Route</c> attribute.</item>
///   <item><b>Explicit routes</b> — pages NOT in the visual hierarchy (detail/sub-pages) must be
///     registered manually with <c>Routing.RegisterRoute()</c> in this constructor.</item>
/// </list>
/// The three pages registered below are detail/sub-pages that are pushed onto the navigation stack
/// (not top-level tabs), so they need explicit registration.
/// </para>
/// </summary>
/// <see href="https://learn.microsoft.com/en-us/dotnet/maui/fundamentals/shell/navigation"/>
public partial class AppShell : Shell
{
    /// <summary>
    /// Constructor — loads the XAML and registers detail-page routes for Shell navigation.
    /// </summary>
    public AppShell()
    {
        InitializeComponent();

        // Register routes for pages that are navigated to programmatically (not declared as tabs).
        // nameof() gives us compile-time safety — if the page class is renamed, this breaks at build time.
        Routing.RegisterRoute(nameof(FleetDetailPage), typeof(FleetDetailPage));
        Routing.RegisterRoute(nameof(FleetManagementPage), typeof(FleetManagementPage));
        Routing.RegisterRoute(nameof(ShipDetailPage), typeof(ShipDetailPage));
    }
}
