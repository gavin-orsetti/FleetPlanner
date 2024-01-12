namespace FleetPlanner;

public partial class AppShell : Shell
{
    public AppShell()
    {
        InitializeComponent();

        #region Fleet Routes
        // Home Page
        Routing.RegisterRoute( Routes.MainPage_PageName, Routes.MainPage_PageType );

        // Create New Fleet
        Routing.RegisterRoute( Routes.MyFleets_NewFleet_PageName, Routes.MyFleets_NewFleet_PageType );
        #endregion Fleet Routes
    }
}
