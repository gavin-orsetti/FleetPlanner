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
        IsDarkMode = Application.Current?.RequestedTheme == AppTheme.Dark;
    }

    [RelayCommand]
    private async Task LoadSettingsAsync()
    {
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
