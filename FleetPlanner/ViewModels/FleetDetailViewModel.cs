using System.Collections.ObjectModel;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using FleetPlanner.Models;
using FleetPlanner.Repositories;
using FleetPlanner.Services;

namespace FleetPlanner.ViewModels;

[QueryProperty(nameof(FleetId), "fleetId")]
public partial class FleetDetailViewModel : ObservableObject
{
    private readonly IFleetRepository _fleetRepository;
    private readonly IShipDataService _shipDataService;

    [ObservableProperty]
    private int _fleetId;

    [ObservableProperty]
    private Fleet? _fleet;

    [ObservableProperty]
    private string _fleetName = string.Empty;

    [ObservableProperty]
    private string _affiliation = string.Empty;

    [ObservableProperty]
    private string _areaOfOperation = string.Empty;

    [ObservableProperty]
    private string _manifesto = string.Empty;

    [ObservableProperty]
    private string _notes = string.Empty;

    [ObservableProperty]
    private ObservableCollection<FleetShipDisplay> _ships = [];

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private bool _isEditing;

    public FleetDetailViewModel(IFleetRepository fleetRepository, IShipDataService shipDataService)
    {
        _fleetRepository = fleetRepository;
        _shipDataService = shipDataService;
    }

    [RelayCommand]
    private async Task LoadFleetAsync()
    {
        IsLoading = true;
        try
        {
            Fleet = await _fleetRepository.GetFleetAsync(FleetId);
            if (Fleet is null)
                return;

            FleetName = Fleet.Name;
            Affiliation = Fleet.Affiliation;
            AreaOfOperation = Fleet.AreaOfOperation;
            Manifesto = Fleet.Manifesto;
            Notes = Fleet.Notes;

            var fleetShips = await _fleetRepository.GetFleetShipsAsync(FleetId);
            var displays = new List<FleetShipDisplay>();

            foreach (var fs in fleetShips)
            {
                var ship = await _shipDataService.GetShipAsync(fs.ShipId);
                displays.Add(new FleetShipDisplay
                {
                    FleetShipId = fs.Id,
                    ShipId = fs.ShipId,
                    Callsign = fs.Callsign,
                    ShipName = ship?.Name ?? "Unknown",
                    Manufacturer = ship?.Manufacturer ?? "Unknown",
                    Role = ship?.Role ?? "Unknown",
                    PriceUsd = ship?.PriceUsd ?? 0
                });
            }

            Ships = new ObservableCollection<FleetShipDisplay>(displays);
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private void ToggleEdit()
    {
        IsEditing = !IsEditing;
    }

    [RelayCommand]
    private async Task SaveFleetAsync()
    {
        if (Fleet is null)
            return;

        Fleet.Name = FleetName;
        Fleet.Affiliation = Affiliation;
        Fleet.AreaOfOperation = AreaOfOperation;
        Fleet.Manifesto = Manifesto;
        Fleet.Notes = Notes;

        await _fleetRepository.SaveFleetAsync(Fleet);
        IsEditing = false;
    }

    [RelayCommand]
    private async Task AddShipAsync()
    {
        await Shell.Current.GoToAsync($"ShipBrowserPage?selectMode=true&fleetId={FleetId}");
    }

    [RelayCommand]
    private async Task RemoveShipAsync(FleetShipDisplay display)
    {
        if (display is null)
            return;

        await _fleetRepository.DeleteFleetShipAsync(display.FleetShipId);
        Ships.Remove(display);
    }

    [RelayCommand]
    private async Task GoBackAsync()
    {
        await Shell.Current.GoToAsync("..");
    }
}

public class FleetShipDisplay
{
    public int FleetShipId { get; set; }
    public int ShipId { get; set; }
    public string Callsign { get; set; } = string.Empty;
    public string ShipName { get; set; } = string.Empty;
    public string Manufacturer { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public decimal PriceUsd { get; set; }
}
