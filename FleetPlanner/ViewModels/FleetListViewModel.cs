using System.Collections.ObjectModel;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using FleetPlanner.Models;
using FleetPlanner.Repositories;

namespace FleetPlanner.ViewModels;

public partial class FleetListViewModel : ObservableObject
{
    private readonly IFleetRepository _fleetRepository;

    [ObservableProperty]
    private ObservableCollection<Fleet> _fleets = [];

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private bool _isEmpty;

    public FleetListViewModel(IFleetRepository fleetRepository)
    {
        _fleetRepository = fleetRepository;
    }

    [RelayCommand]
    private async Task LoadFleetsAsync()
    {
        IsLoading = true;
        try
        {
            var fleets = await _fleetRepository.GetAllFleetsAsync();
            Fleets = new ObservableCollection<Fleet>(fleets);
            IsEmpty = Fleets.Count == 0;
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task AddFleetAsync()
    {
        await Shell.Current.GoToAsync("FleetManagementPage?fleetId=0");
    }

    [RelayCommand]
    private async Task DeleteFleetAsync(Fleet fleet)
    {
        if (fleet is null)
            return;
        await _fleetRepository.DeleteFleetAsync(fleet.Id);
        await LoadFleetsAsync();
    }

    [RelayCommand]
    private async Task GoToFleetDetailAsync(Fleet fleet)
    {
        if (fleet is null)
            return;
        await Shell.Current.GoToAsync($"FleetDetailPage?fleetId={fleet.Id}");
    }
}
