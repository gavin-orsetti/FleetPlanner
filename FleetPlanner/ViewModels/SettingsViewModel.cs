using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using FleetPlanner.Services;
using FleetPlanner.Views;

namespace FleetPlanner.ViewModels;

/// <summary>
/// ViewModel for the Settings page — manages cache refresh and dark mode toggle.
/// <para>
/// <b>Responsibilities:</b>
/// <list type="bullet">
///   <item>Display when the ship data cache was last refreshed.</item>
///   <item>Allow the user to force-refresh the cache (re-download from starcitizen.tools wiki API).</item>
///   <item>Toggle dark/light theme at runtime via <see cref="Application.UserAppTheme"/>.</item>
/// </list>
/// </para>
/// <para>
/// <b>Application.Current timing:</b> MAUI's <c>Application.Current</c> is not available
/// during DI construction (before <c>MauiApp.Build()</c> completes). Accessing it in the
/// constructor would throw <c>NullReferenceException</c>. That's why theme detection is
/// deferred to <see cref="LoadSettingsAsync"/>, which runs after the UI is ready.
/// </para>
/// </summary>
/// <see href="https://learn.microsoft.com/en-us/dotnet/maui/user-interface/theming"/>
/// <see href="https://learn.microsoft.com/en-us/dotnet/communitytoolkit/mvvm/"/>
public partial class SettingsViewModel : ObservableObject
{
    /// <summary>For checking cache age and triggering a force-refresh of ship data.</summary>
    private readonly IShipDataService _shipDataService;

    /// <summary>Human-readable timestamp of the last cache refresh (e.g., "3/15/2026 2:30 PM").</summary>
    [ObservableProperty]
    private string _lastUpdated = "Never";

    /// <summary>True while the cache is being refreshed — drives a loading indicator in the View.</summary>
    [ObservableProperty]
    private bool _isRefreshing;

    /// <summary>
    /// Whether dark mode is enabled. Changing this triggers <see cref="OnIsDarkModeChanged"/>
    /// which updates the app's theme in real time.
    /// </summary>
    [ObservableProperty]
    private bool _isDarkMode;

    /// <summary>
    /// Constructor — receives the ship data service from DI.
    /// <para>
    /// <b>Note:</b> We intentionally do NOT access <c>Application.Current</c> here.
    /// During DI construction, the Application instance may not exist yet. Theme detection
    /// is deferred to <see cref="LoadSettingsAsync"/>.
    /// </para>
    /// </summary>
    /// <param name="shipDataService">For cache management operations.</param>
    public SettingsViewModel(IShipDataService shipDataService)
    {
        _shipDataService = shipDataService;
        // FIX: Defer Application.Current access. During DI construction the
        // Application instance may not exist yet, causing a NullReferenceException.
        // The value is set in LoadSettingsAsync which runs after the UI is ready.
    }

    /// <summary>
    /// Loads current settings — detects the active theme and reads the cache timestamp.
    /// <para>
    /// Called from the page's <c>OnAppearing</c> handler, which is guaranteed to fire after
    /// <c>Application.Current</c> is available.
    /// </para>
    /// </summary>
    [RelayCommand]
    private async Task LoadSettingsAsync()
    {
        // FIX: Initialise IsDarkMode here instead of in the constructor,
        // because Application.Current is guaranteed to exist by this point.
        IsDarkMode = Application.Current?.RequestedTheme == AppTheme.Dark;

        var lastUpdated = await _shipDataService.GetLastUpdatedAsync();
        LastUpdated = lastUpdated?.ToString("g") ?? "Never";
    }

    /// <summary>
    /// Force-refreshes the ship data cache by re-downloading from the starcitizen.tools wiki API.
    /// <para>
    /// <b>forceRefresh: true</b> bypasses the cache-age check in <see cref="CachedShipDataService"/>,
    /// forcing a fresh HTTP request regardless of when the cache was last updated.
    /// If the API is unreachable, a user-friendly error dialog is shown.
    /// </para>
    /// </summary>
    [RelayCommand]
    private async Task RefreshCacheAsync()
    {
        IsRefreshing = true;
        try
        {
            // forceRefresh: true skips the 24-hour cache window and hits the API immediately.
            await _shipDataService.GetAllShipsAsync(forceRefresh: true);
            var lastUpdated = await _shipDataService.GetLastUpdatedAsync();
            LastUpdated = lastUpdated?.ToString("g") ?? "Never";
        }
        catch (Exception)
        {
            // FIX: The method is DisplayAlert (returns Task), not DisplayAlertAsync
            // which does not exist on Shell/Page and would throw MissingMethodException.
            await Shell.Current.DisplayAlert("Error", "Could not refresh ship data. Check your connection.", "OK");
        }
        finally
        {
            IsRefreshing = false;
        }
    }

    /// <summary>Navigates to the Tag Manager page.</summary>
    [RelayCommand]
    private async Task ManageTagsAsync()
    {
        await Shell.Current.GoToAsync(nameof(TagManagerPage));
    }

    /// <summary>
    /// Partial method hook — fires whenever <see cref="IsDarkMode"/> changes.
    /// <para>
    /// <b>Runtime theme switching:</b> Setting <c>Application.Current.UserAppTheme</c> to
    /// <see cref="AppTheme.Dark"/> or <see cref="AppTheme.Light"/> immediately changes the
    /// app's visual theme. MAUI's resource dictionaries (Colors.xaml, Styles.xaml) define
    /// <c>AppThemeBinding</c> values that respond to this property automatically.
    /// </para>
    /// </summary>
    /// <param name="value">True for dark mode, false for light mode.</param>
    /// <see href="https://learn.microsoft.com/en-us/dotnet/maui/user-interface/theming"/>
    partial void OnIsDarkModeChanged(bool value)
    {
        if (Application.Current is not null)
            Application.Current.UserAppTheme = value ? AppTheme.Dark : AppTheme.Light;
    }
}
