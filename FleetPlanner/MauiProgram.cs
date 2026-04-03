using FleetPlanner.Repositories;
using FleetPlanner.Services;
using FleetPlanner.ViewModels;
using FleetPlanner.Views;

using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;

using SkiaSharp;

namespace FleetPlanner;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .UseSkiaSharp()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });

        // LiveCharts2 configuration
        LiveCharts.Configure(config =>
            config.AddSkiaSharp()
                  .AddDefaultMappers());

        // HTTP client
        builder.Services.AddHttpClient("ShipData", client =>
        {
            client.DefaultRequestHeaders.Add("Accept", "application/json");
            client.Timeout = TimeSpan.FromSeconds(30);
        });

        // Repositories
        builder.Services.AddSingleton<IFleetRepository, FleetRepository>();

        // Services
        builder.Services.AddSingleton<ShipDataService>();
        builder.Services.AddSingleton<IShipDataService, CachedShipDataService>();
        builder.Services.AddSingleton<IRecommendationService, RecommendationService>();

        // ViewModels
        builder.Services.AddTransient<DashboardViewModel>();
        builder.Services.AddTransient<FleetListViewModel>();
        builder.Services.AddTransient<FleetDetailViewModel>();
        builder.Services.AddTransient<ShipBrowserViewModel>();
        builder.Services.AddTransient<ShipDetailViewModel>();
        builder.Services.AddTransient<RecommendationsViewModel>();
        builder.Services.AddTransient<SettingsViewModel>();

        // Pages
        builder.Services.AddTransient<DashboardPage>();
        builder.Services.AddTransient<FleetListPage>();
        builder.Services.AddTransient<FleetDetailPage>();
        builder.Services.AddTransient<ShipBrowserPage>();
        builder.Services.AddTransient<Views.ShipDetailPage>();
        builder.Services.AddTransient<RecommendationsPage>();
        builder.Services.AddTransient<SettingsPage>();

        return builder.Build();
    }
}
