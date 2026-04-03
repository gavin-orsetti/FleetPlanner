using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using FleetPlanner.Services;

namespace FleetPlanner.ViewModels;

public partial class SettingsViewModel : ObservableObject
{
    private readonly IShipDataService _shipDataService;

    [ObservableProperty]
    private string _lastUpdated = "Never";

    [ObservableProperty]
    private bool _isRefreshing;

    [ObservableProperty]
    private bool _isDarkMode;

    public SettingsViewModel(IShipDataService shipDataService)
    {
        _shipDataService = shipDataService;
        // FIX: Defer Application.Current access. During DI construction the
        // Application instance may not exist yet, causing a NullReferenceException.
        // The value is set in LoadSettingsAsync which runs after the UI is ready.
    }

    [RelayCommand]
    private async Task LoadSettingsAsync()
    {
        // FIX: Initialise IsDarkMode here instead of in the constructor,
        // because Application.Current is guaranteed to exist by this point.
        IsDarkMode = Application.Current?.RequestedTheme == AppTheme.Dark;

        var lastUpdated = await _shipDataService.GetLastUpdatedAsync();
        LastUpdated = lastUpdated?.ToString("g") ?? "Never";
    }

    [RelayCommand]
    private async Task RefreshCacheAsync()
    {
        IsRefreshing = true;
        try
        {
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

    partial void OnIsDarkModeChanged(bool value)
    {
        if (Application.Current is not null)
            Application.Current.UserAppTheme = value ? AppTheme.Dark : AppTheme.Light;
    }
}
