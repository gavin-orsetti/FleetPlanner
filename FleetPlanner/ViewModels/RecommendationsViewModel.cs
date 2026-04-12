using System.Collections.ObjectModel;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using FleetPlanner.Helpers;
using FleetPlanner.Models;
using FleetPlanner.Services;
using FleetPlanner.Views;

namespace FleetPlanner.ViewModels;

/// <summary>
/// ViewModel for the Recommendations page — builds the fleet graph, runs the 12-pattern
/// recommendation engine, and displays results sorted by score descending.
///
/// <para><b>Page lifecycle:</b> <see cref="LoadRecommendationsCommand"/> runs on every
/// <c>OnAppearing</c>. It calls <see cref="IGraphBuildService.GetOrRebuildAsync"/> (which
/// may serve a cached graph if the data hasn't changed) and then
/// <see cref="IRecommendationService.GetRecommendations"/>. The <see cref="Recommendations"/>
/// ObservableCollection is cleared and rebuilt on each load.</para>
///
/// <para><b>Empty states:</b> <see cref="StatusMessage"/> provides context-aware messages:
/// "Add some ships first" if the graph has no ships, "No recommendations — your fleet looks good"
/// if analysis produced zero results, or an error message if an exception occurred.</para>
///
/// <para><b>Error handling:</b> Exceptions (e.g. from graph build failures) are caught and
/// displayed in <see cref="StatusMessage"/> rather than crashing. The page shows the empty
/// state in this case.</para>
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

    /// <summary>Navigates to the entity referenced by a recommendation (ship editor or group detail).</summary>
    [RelayCommand]
    private async Task NavigateToEntityAsync(Recommendation rec)
    {
        if (rec?.ScopeId is null) return;

        if (rec.ScopeType == RecommendationScope.Ship)
        {
            await Shell.Current.GoToAsync(nameof(OwnedShipEditorPage), new Dictionary<string, object>
            {
                { QueryParameters.OwnedShipId, rec.ScopeId.Value }
            });
        }
        else if (rec.ScopeType == RecommendationScope.Group)
        {
            await Shell.Current.GoToAsync(nameof(GroupDetailPage), new Dictionary<string, object>
            {
                { QueryParameters.GroupId, rec.ScopeId.Value }
            });
        }
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
