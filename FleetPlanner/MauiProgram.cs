using FleetPlanner.Repositories;
using FleetPlanner.Services;
using FleetPlanner.ViewModels;
using FleetPlanner.Views;

using LiveChartsCore.SkiaSharpView.Maui;
using SkiaSharp.Views.Maui.Controls.Hosting;

namespace FleetPlanner;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        SQLitePCL.Batteries_V2.Init();

        CrashLogger.Register();

        try
        {
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
            builder.Services.AddTransient<ShipDetailPage>();
            builder.Services.AddTransient<RecommendationsPage>();
            builder.Services.AddTransient<SettingsPage>();

            // Shell — must be Singleton so the same instance is used for the app lifetime
            builder.Services.AddSingleton<AppShell>();

            return builder.Build();
        }
        catch (Exception ex)
        {
            CrashLogger.WriteLogPublic(ex);
            throw;
        }
    }
}
