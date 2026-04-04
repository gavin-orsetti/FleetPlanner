using FleetPlanner.ViewModels;

namespace FleetPlanner.Views;

/// <summary>
/// Code-behind for the Recommendations page — displays AI-generated fleet improvement suggestions.
/// <para>
/// <b>Data flow:</b> The user selects a fleet from the Picker, the ViewModel loads the fleet's
/// ships, passes them to the <see cref="Services.IRecommendationService"/>, and displays the
/// results as categorized, prioritized recommendation cards with suggested ship lists.
/// </para>
/// <para>
/// <b>Nested CollectionView:</b> The XAML uses a nested <c>CollectionView</c> inside each
/// recommendation's <c>DataTemplate</c> to display suggested ships as horizontally scrollable
/// chips. This uses <c>LinearItemsLayout Orientation="Horizontal"</c> for a horizontal layout.
/// </para>
/// </summary>
/// <see href="https://learn.microsoft.com/en-us/dotnet/maui/user-interface/controls/collectionview/layout"/>
public partial class RecommendationsPage : ContentPage
{
    private readonly RecommendationsViewModel _viewModel;

    /// <summary>
    /// Constructor — receives the ViewModel from DI, loads XAML, and sets the binding context.
    /// </summary>
    /// <param name="viewModel">The Recommendations ViewModel injected by the DI container.</param>
    public RecommendationsPage(RecommendationsViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = viewModel;
    }

    /// <summary>
    /// Loads fleets and runs the recommendation engine each time the page appears.
    /// </summary>
    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.LoadRecommendationsCommand.ExecuteAsync(null);
    }
}
