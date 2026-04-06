using System.Collections.ObjectModel;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using FleetPlanner.Models;
using FleetPlanner.Services;

namespace FleetPlanner.ViewModels;

/// <summary>
/// ViewModel for the Recommendations page — runs the graph-driven
/// recommendation engine and displays results grouped by priority.
/// </summary>
public partial class RecommendationsViewModel : ObservableObject
{
    private readonly IGraphBuildService _graphBuildService;
    private readonly IRecommendationService _recommendationService;

    /// <summary>True while loading.</summary>
    [ObservableProperty]
    private bool _isLoading;

    /// <summary>True when no recommendations were generated.</summary>
    [ObservableProperty]
    private bool _isEmpty;

    /// <summary>Summary text for the current state.</summary>
    [ObservableProperty]
    private string _statusMessage = string.Empty;

    /// <summary>Total number of recommendations generated.</summary>
    [ObservableProperty]
    private int _totalRecommendations;

    /// <summary>Recommendations to display.</summary>
    public ObservableCollection<Recommendation> Recommendations { get; } = new();

    /// <summary>
    /// Constructor — receives dependencies from the DI container.
    /// </summary>
    public RecommendationsViewModel(
        IGraphBuildService graphBuildService,
        IRecommendationService recommendationService)
    {
        _graphBuildService = graphBuildService;
        _recommendationService = recommendationService;
    }

    /// <summary>Builds the graph and runs the recommendation engine.</summary>
    [RelayCommand]
    private async Task LoadRecommendationsAsync()
    {
        IsLoading = true;
        try
        {
            var graph = await _graphBuildService.GetOrRebuildAsync();
            var results = _recommendationService.GetRecommendations(graph);

            Recommendations.Clear();
            foreach (var r in results)
                Recommendations.Add(r);

            TotalRecommendations = results.Count;
            IsEmpty = results.Count == 0;

            StatusMessage = results.Count == 0
                ? graph.Ships.Count == 0
                    ? "Add some ships to your collection first to get recommendations."
                    : "No recommendations right now — your fleet looks good!"
                : $"{results.Count} recommendations generated.";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error generating recommendations: {ex.Message}";
            IsEmpty = true;
        }
        finally
        {
            IsLoading = false;
        }
    }
}
