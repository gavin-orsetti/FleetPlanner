using FleetPlanner.ViewModels;

namespace FleetPlanner.Views;

/// <summary>
/// Code-behind for the Tag Manager page — a browsable list of all tag definitions
/// with search, create, and archive capabilities.
/// </summary>
public partial class TagManagerPage : ContentPage
{
    private readonly TagManagerViewModel _viewModel;

    /// <summary>
    /// Constructor — receives the ViewModel from DI, loads XAML, and sets the binding context.
    /// </summary>
    public TagManagerPage(TagManagerViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = viewModel;
    }

    /// <summary>Loads tags when the page appears (or re-appears after editing).</summary>
    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.LoadTagsCommand.ExecuteAsync(null);
    }
}
