using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using FleetPlanner.Models;
using FleetPlanner.Services;

namespace FleetPlanner.ViewModels;

[QueryProperty(nameof(ShipId), "shipId")]
public partial class ShipDetailViewModel : ObservableObject
{
    private readonly IShipDataService _shipDataService;

    [ObservableProperty]
    private int _shipId;

    [ObservableProperty]
    private Ship? _ship;

    [ObservableProperty]
    private bool _isLoading;

    public ShipDetailViewModel(IShipDataService shipDataService)
    {
        _shipDataService = shipDataService;
    }

    [RelayCommand]
    private async Task LoadShipAsync()
    {
        IsLoading = true;
        try
        {
            Ship = await _shipDataService.GetShipAsync(ShipId);
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task GoBackAsync()
    {
        await Shell.Current.GoToAsync("..");
    }
}
