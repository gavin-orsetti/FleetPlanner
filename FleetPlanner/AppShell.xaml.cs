namespace FleetPlanner;

public partial class AppShell : Shell
{
    public AppShell()
    {
        InitializeComponent();

        #region Routes
        #region Fleet Routes
        // Home Page
        Routing.RegisterRoute( Routes.MainPage_PageName, Routes.MainPage_PageType );

        // Create New Fleet
        Routing.RegisterRoute( Routes.MyFleets_NewFleet_PageName, Routes.MyFleets_NewFleet_PageType );

        // Fleet Page
        Routing.RegisterRoute( Routes.FleetPage_PageName, Routes.FleetPage_PageType );

        // Edit Fleet Page
        Routing.RegisterRoute( Routes.EditFleetPage_PageName, Routes.EditFleetPage_PageType );
        #endregion Fleet Routes
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
