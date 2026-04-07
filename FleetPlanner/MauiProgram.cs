using FleetPlanner.Repositories;
using FleetPlanner.Services;
using FleetPlanner.ViewModels;
using FleetPlanner.Views;
using LiveChartsCore.SkiaSharpView.Maui;
using SkiaSharp.Views.Maui.Controls.Hosting;

namespace FleetPlanner;

/// <summary>
/// The application entry point and dependency injection (DI) composition root.
/// All service lifetimes, repository bindings, and the startup bootstrap sequence are defined here.
///
/// <para><b>Startup sequence (order matters):</b>
/// <list type="number">
///   <item>Initialise SQLitePCL native bindings (<c>Batteries_V2.Init()</c>).</item>
///   <item>Create the MAUI builder and configure SkiaSharp, LiveCharts, and fonts.</item>
///   <item>Register the named <c>"ShipData"</c> HttpClient (User-Agent + 30s timeout).</item>
///   <item>Register all repositories as <b>singletons</b> (one DB connection per repo, lazily created).</item>
///   <item>Register services: <c>DatabaseBootstrapService</c>, <c>ShipDataService</c> (concrete),
///     <c>IShipDataService → CachedShipDataService</c> (via factory lambda), graph + recommendation services.</item>
///   <item>Register ViewModels and Pages as <b>transient</b> (fresh instance per navigation).</item>
///   <item>Register <c>AppShell</c> as <b>singleton</b> (one shell for the app lifetime).</item>
///   <item><c>builder.Build()</c> — creates the DI container and returns the configured <see cref="MauiApp"/>.</item>
/// </list></para>
///
/// <para><b>Database bootstrap:</b> <c>DatabaseBootstrapService.InitialiseAsync()</c> (table creation
/// and tag taxonomy seeding) is <b>not</b> called here. Because <c>CreateMauiApp()</c> is synchronous,
/// awaiting async work would deadlock the UI thread before the MAUI runtime is fully initialised.
/// Instead, bootstrap runs in <see cref="AppShell.OnAppearing"/> — the earliest point at which the
/// MAUI runtime is ready and async work can safely execute. The method is idempotent (checks
/// <c>schema_version</c> before seeding), so repeated <c>OnAppearing</c> calls are harmless.</para>
///
/// <para><b>Why the IShipDataService factory lambda is needed:</b> Both <see cref="ShipDataService"/>
/// and <see cref="CachedShipDataService"/> implement <see cref="IShipDataService"/>. If we registered
/// both as <c>IShipDataService</c>, the DI container would hit a circular resolution loop:
/// <c>IShipDataService → CachedShipDataService → needs IShipDataService → loop</c>. Instead,
/// <c>ShipDataService</c> is registered as its concrete type, and the lambda resolves it directly:
/// <c>sp.GetRequiredService&lt;ShipDataService&gt;()</c> — breaking the cycle.</para>
/// </summary>
public static class MauiProgram
{
    /// <summary>
    /// Creates and configures the MAUI application. See class-level documentation for the
    /// full startup sequence. Database bootstrap is deferred to <see cref="AppShell.OnAppearing"/>.
    /// </summary>
    /// <returns>The configured <see cref="MauiApp"/>.</returns>
    public static MauiApp CreateMauiApp()
    {
        SQLitePCL.Batteries_V2.Init();

        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .UseSkiaSharp()
            .UseLiveCharts()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });

        // ── HTTP client registration ──────────────────────────────────────
        builder.Services.AddHttpClient("ShipData", client =>
        {
            client.DefaultRequestHeaders.Add("User-Agent", "FleetPlanner/2.0");
            client.Timeout = TimeSpan.FromSeconds(30);
        });

        // ── Repository registrations ─────────────────────────────────────
        builder.Services.AddSingleton<IOwnedShipRepository, OwnedShipRepository>();
        builder.Services.AddSingleton<ITagRepository, TagRepository>();
        builder.Services.AddSingleton<IOwnedShipTagRepository, OwnedShipTagRepository>();
        builder.Services.AddSingleton<IUserFleetGroupRepository, UserFleetGroupRepository>();
        builder.Services.AddSingleton<IUserFleetGroupTagRepository, UserFleetGroupTagRepository>();

        // ── Service registrations ─────────────────────────────────────────
        builder.Services.AddSingleton<DatabaseBootstrapService>();

        // ShipDataService registered as concrete type, CachedShipDataService as IShipDataService
        // via factory lambda to break circular DI dependency.
        builder.Services.AddSingleton<ShipDataService>();
        builder.Services.AddSingleton<IShipDataService>(sp =>
            new CachedShipDataService(sp.GetRequiredService<ShipDataService>()));

        builder.Services.AddSingleton<IGraphBuildService, GraphBuildService>();
        builder.Services.AddSingleton<IRecommendationService, RecommendationService>();

        // ── ViewModel registrations ───────────────────────────────────────
        builder.Services.AddTransient<DashboardViewModel>();
        builder.Services.AddTransient<OwnedShipLibraryViewModel>();
        builder.Services.AddTransient<OwnedShipEditorViewModel>();
        builder.Services.AddTransient<GroupOverviewViewModel>();
        builder.Services.AddTransient<GroupDetailViewModel>();
        builder.Services.AddTransient<ShipBrowserViewModel>();
        builder.Services.AddTransient<ShipDetailViewModel>();
        builder.Services.AddTransient<RecommendationsViewModel>();
        builder.Services.AddTransient<SettingsViewModel>();
        builder.Services.AddTransient<TagPickerViewModel>();

        // ── Page registrations ────────────────────────────────────────────
        builder.Services.AddTransient<DashboardPage>();
        builder.Services.AddTransient<OwnedShipLibraryPage>();
        builder.Services.AddTransient<OwnedShipEditorPage>();
        builder.Services.AddTransient<GroupOverviewPage>();
        builder.Services.AddTransient<GroupDetailPage>();
        builder.Services.AddTransient<ShipBrowserPage>();
        builder.Services.AddTransient<ShipDetailPage>();
        builder.Services.AddTransient<RecommendationsPage>();
        builder.Services.AddTransient<SettingsPage>();
        builder.Services.AddTransient<TagPickerPage>();

        // ── Shell registration ────────────────────────────────────────────
        builder.Services.AddSingleton<AppShell>();

        // ── Build ─────────────────────────────────────────────────────────
        // Database bootstrap (table creation + tag seeding) is deferred to
        // AppShell.OnAppearing — see class-level docs for rationale.
        return builder.Build();
    }
}
