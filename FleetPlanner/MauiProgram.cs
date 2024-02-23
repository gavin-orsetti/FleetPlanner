using FleetPlanner.MVVM.ViewModels;
using FleetPlanner.MVVM.Views;

using System.Threading.Tasks;

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
        #region Global
        builder.Services.AddSingleton<GlobalViewModel>();
        builder.Services.AddSingleton<ServiceProvider>();
        #endregion Global

        #region ViewModelBase
        builder.Services.AddTransient<ViewModelBase>();
        #endregion ViewModels

        #region Main Page
        // ViewModel
        builder.Services.AddTransient<MainPageViewModel>();
        // Page
        builder.Services.AddTransient<MainPage>();
        #endregion Main Page

        #region Fleet Page
        // ViewModels
        builder.Services.AddTransient<FleetViewModel>();
        builder.Services.AddTransient<FleetViewModel_Edit>();
        builder.Services.AddTransient<FleetViewModel_Populated>();
        builder.Services.AddTransient<NewFleetViewModel>();

        // Pages
        builder.Services.AddTransient<FleetPage>();
        builder.Services.AddTransient<Fleet_Edit_Page>();
        builder.Services.AddTransient<MyFleets_AddNew_Page>();
        #endregion Fleet Page

        #region Task Group Page
        // ViewModels
        builder.Services.AddTransient<TaskGroupViewModel>();
        builder.Services.AddTransient<TaskGroupViewModel_Edit>();
        builder.Services.AddTransient<TaskGroupViewModel_Edit_AddShips>();
        builder.Services.AddTransient<TaskGroupViewModel_Populated>();
        builder.Services.AddTransient<NewTaskGroupViewModel>();
        builder.Services.AddTransient<ViewTaskGroupViewModel>();

        // Pages
        builder.Services.AddTransient<TaskGroupPage>();
        builder.Services.AddTransient<TaskGroup_AddNew_Page>();
        builder.Services.AddTransient<TaskGroup_Edit_AddShip_Page>();
        builder.Services.AddTransient<TaskGroup_Edit_Page>();
        #endregion Task Group Page

        #region Ship Detail Page
        // ViewModels
        builder.Services.AddTransient<ShipDetailViewModel>();
        builder.Services.AddTransient<ShipDetailViewModel_Edit>();
        builder.Services.AddTransient<ShipDetailViewModel_Populated>();
        builder.Services.AddTransient<ViewShipDetailViewModel>();

        // Pages
        builder.Services.AddTransient<ShipDetailPage>();
        builder.Services.AddTransient<ShipDetail_Edit_Page>();
        #endregion Ship Detail Page

        #region ReTask Ship Page
        // ViewModels
        builder.Services.AddTransient<ReTaskViewModel>();

        // Pages
        builder.Services.AddTransient<RetaskShipPage>();
        #endregion ReTask Ship Page

        #region Ship Balance Sheet
        // ViewModels
        builder.Services.AddTransient<ShipBalanceSheetViewModel>();
        #endregion Ship Balance Sheet

        #region Ship
        // ViewModels
        builder.Services.AddTransient<ShipViewModel>();
        builder.Services.AddTransient<SelectableShipViewModel>();

        #endregion Ship Page

        #region Shopping List Page
        // ViewModels
        builder.Services.AddTransient<ShoppingListViewModel>();
        builder.Services.AddTransient<ShoppingListShipDetailViewModel>();
        builder.Services.AddTransient<ShoppingListFleetSelectedViewModel>();

        // Pages
        builder.Services.AddTransient<ShoppingListPage>();
        builder.Services.AddTransient<ShoppingList_FleetSelected_Page>();
        #endregion Shopping List Page
        #endregion Dependancies

        return builder.Build();
    }
}
