using FleetPlanner.ViewModels;

namespace FleetPlanner.Views;

/// <summary>
/// Code-behind for the Tag Editor page — a form for creating or editing
/// user-defined tag definitions. System tags are displayed read-only.
/// </summary>
public partial class TagEditorPage : ContentPage
{
    private readonly TagEditorViewModel _viewModel;

    /// <summary>
    /// Constructor — receives the ViewModel from DI, loads XAML, and sets the binding context.
    /// </summary>
    public TagEditorPage(TagEditorViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = viewModel;
    }

    /// <summary>Loads the tag data when the page appears.</summary>
    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await Task.Yield();
        await _viewModel.LoadCommand.ExecuteAsync(null);
    }
}
