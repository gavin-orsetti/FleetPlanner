using System.Collections.ObjectModel;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using FleetPlanner.Models;
using FleetPlanner.Repositories;
using FleetPlanner.Services;

namespace FleetPlanner.ViewModels;

/// <summary>
/// ViewModel for the Recommendations page — placeholder for Phase 3.
/// Currently loads owned ships and displays a "coming soon" message.
/// Will be fully rewritten with the graph-driven recommendation engine.
/// </summary>
public partial class RecommendationsViewModel : ObservableObject
{
    private readonly IOwnedShipRepository _ownedShipRepository;
    private readonly IShipDataService _shipDataService;

    /// <summary>True while loading.</summary>
    [ObservableProperty]
    private bool _isLoading;

    /// <summary>True when no owned ships exist.</summary>
    [ObservableProperty]
    private bool _isEmpty;

    /// <summary>Summary text for the current state.</summary>
    [ObservableProperty]
    private string _statusMessage = "Recommendations engine is being upgraded. Check back soon!";

    /// <summary>Total number of owned ships for context.</summary>
    [ObservableProperty]
    private int _totalShips;

    /// <summary>
    /// Constructor — receives dependencies from the DI container.
    /// </summary>
    public RecommendationsViewModel(
        IOwnedShipRepository ownedShipRepository,
        IShipDataService shipDataService)
    {
        _ownedShipRepository = ownedShipRepository;
        _shipDataService = shipDataService;
    }

    /// <summary>Loads current state information.</summary>
    [RelayCommand]
    private async Task LoadRecommendationsAsync()
    {
        IsLoading = true;
        try
        {
            var ownedShips = await _ownedShipRepository.GetAllOwnedShipsAsync();
            TotalShips = ownedShips.Count;
            IsEmpty = ownedShips.Count == 0;

            StatusMessage = ownedShips.Count == 0
                ? "Add some ships to your collection first to get recommendations."
                : $"You have {ownedShips.Count} ships. Graph-driven recommendations coming in Phase 3.";
        }
        finally
        {
            IsLoading = false;
        }
    }
}
