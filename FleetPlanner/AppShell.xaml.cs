namespace FleetPlanner;

public partial class AppShell : Shell
{
    public AppShell()
    {
        InitializeComponent();

        #region Routes
        // Home Page
        Routing.RegisterRoute( Routes.Main_Page_PageName, Routes.Main_Page_PageType );

        #region Fleet

        // Create New Fleet
        Routing.RegisterRoute( Routes.MyFleets_NewFleetPage_PageName, Routes.MyFleets_NewFleetPage_PageType );

        // Fleet Page
        Routing.RegisterRoute( Routes.Fleet_Page_PageName, Routes.Fleet_Page_PageType );

        // Edit Fleet Page
        Routing.RegisterRoute( Routes.Fleet_EditPage_PageName, Routes.Fleet_EditPage_PageType );
        #endregion Fleet

        #region Task Group
        // New Page
        Routing.RegisterRoute( Routes.TaskGroup_AddNewPage_PageName, Routes.TaskGroup_AddNewPage_PageType );

        // Task Group Page
        Routing.RegisterRoute( Routes.TaskGroup_Page_PageName, Routes.TaskGroup_Page_PageType );

        // Edit Page
        Routing.RegisterRoute( Routes.TaskGroup_EditPage_PageName, Routes.TaskGroup_EditPage_PageType );
        #endregion Task Group

        // Edit Add Ships Page
        Routing.RegisterRoute( Routes.TaskGroup_Edit_AddShipsPage_PageName, Routes.TaskGroup_Edit_AddShipsPage_PageType );

        #region ShipDetail
        // Ship Detail Page
        Routing.RegisterRoute( Routes.ShipDetail_Page_PageName, Routes.ShipDetail_Page_PageType );

        // Edit Page
        Routing.RegisterRoute( Routes.ShipDetail_EditPage_PageName, Routes.ShipDetail_EditPage_PageType );
        #endregion ShipDetail

        #region Re-Task Ship
        // Re-Task Ship Page
        Routing.RegisterRoute( Routes.ReTaskShip_Page_PageName, Routes.RetaskShip_Page_PageType );
        #endregion Re-Task Ship

        #region Shopping List
        // Main Shopping List Page
        Routing.RegisterRoute( Routes.ShoppingList_Page_PageName, Routes.ShoppingList_Page_PageType );

        // Fleet Selected Page
        Routing.RegisterRoute( Routes.ShoppingList_FleetSelectedPage_PageName, Routes.ShoppingList_FleetSelectedPage_PageType );
        #endregion Shopping List
        #endregion Routes

        #region Theme Settings
        // Gets the user's system theme and sets the app theme to match
        App.Current.UserAppTheme = Application.Current.RequestedTheme;

        // Sets the Light/Dark theme switch to match the selected theme
        themeSwitch.IsToggled = App.Current.RequestedTheme == AppTheme.Dark;
        App.Current.RequestedThemeChanged += ( s, e ) =>
        {
            themeSwitch.IsToggled = App.Current.RequestedTheme == AppTheme.Dark;
        };
        #endregion ThemeSettings

    }
    /// <summary>
    /// Swaps the theme when the switch is flipped.
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e">The boolean value that represents the switch state (true == dark mode, false == light mode) </param>
    private void ThemeToggled( object sender, ToggledEventArgs e )
    {
        App.Current.UserAppTheme = e.Value ? AppTheme.Dark : AppTheme.Light;
    }
}
