using FleetPlanner.MVVM.ViewModels;
using FleetPlanner.MVVM.Views;

using UraniumUI;

namespace FleetPlanner;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .UseUraniumUI()
            .UseUraniumUIMaterial()
            .ConfigureFonts( fonts =>
            {
                fonts.AddFont( "OpenSans-Regular.ttf", "OpenSansRegular" );
                fonts.AddFont( "OpenSans-Semibold.ttf", "OpenSansSemibold" );
                fonts.AddMaterialIconFonts();
            } );

        #region Dependancies
        builder.Services.AddTransient<ShoppingListViewModel>();
        builder.Services.AddTransient<ShoppingListPage>();
        builder.Services.AddSingleton<ServiceProvider>();
        #endregion Dependancies

        return builder.Build();
    }
}
