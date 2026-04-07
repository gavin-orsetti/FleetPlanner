using FleetPlanner.Services;
using FleetPlanner.Views;

namespace FleetPlanner;

/// <summary>
/// Code-behind for <c>AppShell.xaml</c> — the app's navigation shell.
/// Registers explicit routes for detail/sub-pages that are pushed onto the nav stack
/// and runs <see cref="DatabaseBootstrapService.InitialiseAsync"/> on first appearance.
/// </summary>
public partial class AppShell : Shell
{
    /// <summary>Constructor — loads the XAML and registers detail-page routes for Shell navigation.</summary>
    public AppShell()
    {
        InitializeComponent();

        // Detail/sub-pages navigated to programmatically
        Routing.RegisterRoute(nameof(ShipDetailPage), typeof(ShipDetailPage));
        Routing.RegisterRoute(nameof(OwnedShipEditorPage), typeof(OwnedShipEditorPage));
        Routing.RegisterRoute(nameof(GroupDetailPage), typeof(GroupDetailPage));
        Routing.RegisterRoute(nameof(TagPickerPage), typeof(TagPickerPage));
        Routing.RegisterRoute(nameof(TagManagerPage), typeof(TagManagerPage));
        Routing.RegisterRoute(nameof(TagEditorPage), typeof(TagEditorPage));
    }

    /// <summary>
    /// Runs database bootstrap (table creation + tag taxonomy seeding) on first appearance.
    /// <para>
    /// <b>Why here and not in <c>MauiProgram.CreateMauiApp()</c>?</b>
    /// <c>CreateMauiApp()</c> is synchronous — calling <c>.GetAwaiter().GetResult()</c> on async
    /// work there deadlocks the UI thread before the MAUI runtime is fully initialised, causing
    /// the app to hang on the splash screen. <c>OnAppearing</c> fires after the MAUI runtime is
    /// ready, so async work executes safely.
    /// </para>
    /// <para>
    /// <c>InitialiseAsync()</c> is idempotent (it checks <c>schema_version</c> before seeding),
    /// so repeated <c>OnAppearing</c> calls are harmless.
    /// </para>
    /// </summary>
    protected override async void OnAppearing()
    {
        base.OnAppearing();

        var bootstrap = Handler?.MauiContext?.Services.GetRequiredService<DatabaseBootstrapService>();
        if (bootstrap is not null)
            await bootstrap.InitialiseAsync();
    }
}
