using FleetPlanner.Repositories;
using FleetPlanner.Services;
using FleetPlanner.ViewModels;
using FleetPlanner.Views;
using LiveChartsCore.SkiaSharpView.Maui;
using SkiaSharp.Views.Maui.Controls.Hosting;

namespace FleetPlanner;

/// <summary>
/// The application entry point and dependency injection (DI) composition root.
/// <para>
/// <b>What is a composition root?</b> It's the single place in the app where all dependencies
/// are wired together. Every service, ViewModel, and page is registered here so the DI container
/// knows how to construct them. No other file should use <c>new SomeService()</c> directly —
/// everything flows through DI.
/// </para>
/// <para>
/// <b>MauiAppBuilder pattern:</b> Similar to ASP.NET Core's <c>WebApplicationBuilder</c>.
/// You configure services, middleware (fonts, SkiaSharp, LiveCharts), and build the app.
/// </para>
/// </summary>
/// <see href="https://learn.microsoft.com/en-us/dotnet/maui/fundamentals/dependency-injection"/>
public static class MauiProgram
{
    /// <summary>
    /// Creates and configures the MAUI application.
    /// <para>
    /// This method is called by each platform's entry point (e.g., <c>MainActivity</c> on Android,
    /// <c>AppDelegate</c> on iOS). It returns a fully configured <see cref="MauiApp"/> instance.
    /// </para>
    /// </summary>
    /// <returns>The configured <see cref="MauiApp"/>.</returns>
    public static MauiApp CreateMauiApp()
    {
        // Initialise the SQLite native bindings for the current platform.
        // This MUST be called before any SQLite operations. sqlite-net-pcl requires the
        // SQLitePCLRaw.bundle_e_sqlite3 NuGet package, and Batteries_V2.Init() loads the
        // correct native library for the runtime platform (Android, iOS, Windows, etc.).
        SQLitePCL.Batteries_V2.Init();

        var builder = MauiApp.CreateBuilder();
        builder
            // UseMauiApp<App>() — tells the builder which Application subclass to use as the root.
            .UseMauiApp<App>()
            // UseSkiaSharp() — registers SkiaSharp's rendering handlers. Required by LiveCharts2,
            // which uses SkiaSharp as its drawing backend for cross-platform chart rendering.
            // See: https://github.com/nicholasgasior/SkiaSharp
            .UseSkiaSharp()
            // UseLiveCharts() — registers LiveCharts2's MAUI handlers for chart controls.
            // This makes <lvc:CartesianChart> and <lvc:PieChart> available in XAML.
            // See: https://livecharts.dev/docs/maui/2.0.0-rc6/
            .UseLiveCharts()
            .ConfigureFonts(fonts =>
            {
                // Register custom fonts shipped as embedded resources.
                // The second parameter is the alias used in XAML: FontFamily="OpenSansRegular"
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });

        // ── HTTP client registration ──────────────────────────────────────
        // AddHttpClient registers IHttpClientFactory and a named client "ShipData".
        // IHttpClientFactory manages HttpClient lifetimes and connection pooling —
        // using it avoids socket exhaustion that occurs when creating HttpClient instances directly.
        // See: https://learn.microsoft.com/en-us/dotnet/fundamentals/networking/http/httpclient-guidelines
        builder.Services.AddHttpClient("ShipData", client =>
        {
            client.Timeout = TimeSpan.FromSeconds(30);
            // Set Accept header so the UEX Corp API returns JSON (not XML or other formats).
            client.DefaultRequestHeaders.Accept.Add(
                new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
        });

        // ── Service registrations ─────────────────────────────────────────
        // AddSingleton = one instance for the entire app lifetime.
        // These are long-lived services that hold state (database connections, caches).

        // Repository — single instance because it manages the SQLite connection.
        builder.Services.AddSingleton<IFleetRepository, FleetRepository>();

        // ShipDataService (the "live" API service) — registered as its CONCRETE type,
        // not as IShipDataService. This is intentional — read on for why.
        builder.Services.AddSingleton<ShipDataService>();

        // CachedShipDataService — registered as IShipDataService using a FACTORY LAMBDA.
        //
        // WHY A FACTORY LAMBDA? To break a circular dependency:
        //   If we wrote: AddSingleton<IShipDataService, CachedShipDataService>()
        //   the DI container would try to resolve CachedShipDataService's constructor parameter
        //   (IShipDataService) → which resolves to CachedShipDataService → infinite loop!
        //
        // The factory lambda manually constructs CachedShipDataService, passing in the
        // concrete ShipDataService (resolved by type, not by interface). This breaks the cycle:
        //   IShipDataService → CachedShipDataService(ShipDataService) → no further IShipDataService needed.
        //
        // See: https://learn.microsoft.com/en-us/dotnet/maui/fundamentals/dependency-injection
        builder.Services.AddSingleton<IShipDataService>(sp =>
            new CachedShipDataService(sp.GetRequiredService<ShipDataService>()));

        // Recommendation engine — stateless, so singleton is fine (no mutable state).
        builder.Services.AddSingleton<IRecommendationService, RecommendationService>();

        // ── ViewModel registrations ───────────────────────────────────────
        // AddTransient = new instance every time one is requested.
        // ViewModels are transient because each page navigation should get a fresh state.
        // If they were singletons, navigating away and back would show stale data.
        builder.Services.AddTransient<DashboardViewModel>();
        builder.Services.AddTransient<FleetListViewModel>();
        builder.Services.AddTransient<FleetDetailViewModel>();
        builder.Services.AddTransient<FleetManagementViewModel>();
        builder.Services.AddTransient<ShipBrowserViewModel>();
        builder.Services.AddTransient<ShipDetailViewModel>();
        builder.Services.AddTransient<RecommendationsViewModel>();
        builder.Services.AddTransient<SettingsViewModel>();

        // ── Page registrations ────────────────────────────────────────────
        // Pages are also transient — MAUI creates a new page instance each time it's navigated to.
        // Each page receives its ViewModel via constructor injection.
        builder.Services.AddTransient<DashboardPage>();
        builder.Services.AddTransient<FleetListPage>();
        builder.Services.AddTransient<FleetDetailPage>();
        builder.Services.AddTransient<FleetManagementPage>();
        builder.Services.AddTransient<ShipBrowserPage>();
        builder.Services.AddTransient<ShipDetailPage>();
        builder.Services.AddTransient<RecommendationsPage>();
        builder.Services.AddTransient<SettingsPage>();

        // ── Shell registration ────────────────────────────────────────────
        // AppShell is a singleton — there's only one shell for the entire app lifecycle.
        builder.Services.AddSingleton<AppShell>();

        return builder.Build();
    }
}
