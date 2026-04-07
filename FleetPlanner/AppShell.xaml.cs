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
    private readonly DatabaseBootstrapService _bootstrap;

    /// <summary>
    /// Constructor — receives <see cref="DatabaseBootstrapService"/> via DI, loads the XAML,
    /// and registers detail-page routes for Shell navigation.
    /// </summary>
    public AppShell(DatabaseBootstrapService bootstrap)
    {
        _bootstrap = bootstrap;
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
    /// <b>Why constructor injection?</b> <c>AppShell</c> is registered as a singleton in
    /// <see cref="MauiProgram"/>, so the DI container resolves it and can inject
    /// <see cref="DatabaseBootstrapService"/> directly. The previous approach resolved the
    /// service via <c>Handler?.MauiContext?.Services</c>, which silently returned
    /// <see langword="null"/> on Android because the platform handler is not yet attached
    /// when <c>OnAppearing</c> fires — causing bootstrap to be skipped entirely.
    /// </para>
    /// <para>
    /// <c>InitialiseAsync()</c> is idempotent (it checks <c>schema_version</c> before seeding),
    /// so repeated <c>OnAppearing</c> calls are harmless.
    /// </para>
    /// </summary>
    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _bootstrap.InitialiseAsync();
    }
}
