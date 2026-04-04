using System.Collections.ObjectModel;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using FleetPlanner.Models;
using FleetPlanner.Repositories;

namespace FleetPlanner.ViewModels;

[QueryProperty(nameof(FleetId), "fleetId")]
public partial class FleetManagementViewModel : ObservableObject
{
    private readonly IFleetRepository _fleetRepository;

    [ObservableProperty]
    private int _fleetId;

    [ObservableProperty]
    private string _fleetName = string.Empty;

    [ObservableProperty]
    private string _description = string.Empty;

    [ObservableProperty]
    private int _selectedFocusIndex;

    [ObservableProperty]
    private int _selectedScaleIndex;

    [ObservableProperty]
    private int _availableCrewCount = 1;

    [ObservableProperty]
    private ObservableCollection<string> _focusOptions = new(Enum.GetNames<FleetFocus>());

    [ObservableProperty]
    private ObservableCollection<string> _scaleOptions = new(Enum.GetNames<FleetOperatingScale>());

    [ObservableProperty]
    private ObservableCollection<FleetRoleSelection> _roleSelections = [];

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private bool _isNewFleet;

    public FleetManagementViewModel(IFleetRepository fleetRepository)
    {
        _fleetRepository = fleetRepository;
        InitializeRoleSelections();
    }

    private void InitializeRoleSelections()
    {
        var roles = new ObservableCollection<FleetRoleSelection>();
        foreach (var role in Enum.GetValues<FleetRole>())
        {
            roles.Add(new FleetRoleSelection { Role = role, IsSelected = false });
        }
        RoleSelections = roles;
    }

    [RelayCommand]
    private async Task LoadAsync()
    {
        IsLoading = true;
        try
        {
            if (FleetId > 0)
            {
                var fleet = await _fleetRepository.GetFleetAsync(FleetId);
                if (fleet is null) return;

                IsNewFleet = false;
                FleetName = fleet.Name;
                Description = fleet.Description;
                SelectedFocusIndex = fleet.PrimaryFocus;
                SelectedScaleIndex = fleet.OperatingScale;
                AvailableCrewCount = fleet.AvailableCrewCount;

                var declaredRoles = fleet.DeclaredRoles;
                foreach (var rs in RoleSelections)
                    rs.IsSelected = declaredRoles.Contains(rs.Role);
            }
            else
            {
                IsNewFleet = true;
                FleetName = string.Empty;
                Description = string.Empty;
                SelectedFocusIndex = (int)FleetFocus.Multipurpose;
                SelectedScaleIndex = (int)FleetOperatingScale.Solo;
                AvailableCrewCount = 1;
            }
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        if (string.IsNullOrWhiteSpace(FleetName))
            return;

        Fleet fleet;
        if (FleetId > 0)
        {
            fleet = await _fleetRepository.GetFleetAsync(FleetId) ?? new Fleet();
        }
        else
        {
            fleet = new Fleet();
        }

        fleet.Name = FleetName.Trim();
        fleet.Description = Description?.Trim() ?? string.Empty;
        fleet.PrimaryFocus = SelectedFocusIndex;
        fleet.OperatingScale = SelectedScaleIndex;
        fleet.AvailableCrewCount = AvailableCrewCount;
        fleet.DeclaredRoles = RoleSelections
            .Where(rs => rs.IsSelected)
            .Select(rs => rs.Role)
            .ToList();

        await _fleetRepository.SaveFleetAsync(fleet);
        await Shell.Current.GoToAsync("..");
    }

    [RelayCommand]
    private async Task DeleteAsync()
    {
        if (FleetId <= 0) return;

        var confirm = await Shell.Current.DisplayAlert(
            "Delete Fleet",
            $"Are you sure you want to delete '{FleetName}'? All ships in this fleet will be removed.",
            "Delete", "Cancel");

        if (!confirm) return;

        await _fleetRepository.DeleteFleetAsync(FleetId);
        await Shell.Current.GoToAsync("..");
    }

    [RelayCommand]
    private async Task CancelAsync()
    {
        await Shell.Current.GoToAsync("..");
    }
}

public class FleetRoleSelection : ObservableObject
{
    public FleetRole Role { get; set; }

    private bool _isSelected;
    public bool IsSelected
    {
        get => _isSelected;
        set => SetProperty(ref _isSelected, value);
    }

    public string DisplayName => Role.ToString();
}
