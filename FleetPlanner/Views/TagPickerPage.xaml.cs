using FleetPlanner.ViewModels;

namespace FleetPlanner.Views;

/// <summary>
/// Code-behind for the Tag Picker page — a reusable chip-based tag assignment UI
/// that works for ship global tags, ship contextual (group-scoped) tags, and group tags.
/// </summary>
public partial class TagPickerPage : ContentPage
{
    private readonly TagPickerViewModel _viewModel;

    /// <summary>
    /// Constructor — receives the ViewModel from DI, loads XAML, and sets the binding context.
    /// </summary>
    public TagPickerPage(TagPickerViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = viewModel;
    }

    /// <summary>Loads the available tags when the page appears.</summary>
    protected override async void OnAppearing()
    {
        base.OnAppearing();
        // Small yield to ensure Shell has applied QueryProperty values to the ViewModel
        await Task.Yield();
        await _viewModel.LoadTagsCommand.ExecuteAsync(null);
    }
}
