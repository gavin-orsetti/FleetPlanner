using FleetPlanner.ViewModels;

namespace FleetPlanner.Views;

public partial class RecommendationsPage : ContentPage
{
    private readonly RecommendationsViewModel _viewModel;

    public RecommendationsPage(RecommendationsViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.LoadRecommendationsCommand.ExecuteAsync(null);
    }
}
