using FleetPlanner.ViewModels;

namespace FleetPlanner.Views;

/// <summary>
/// Code-behind for the Owned Ship Editor page — edit callsign, notes, and tags for an owned ship.
/// </summary>
public partial class OwnedShipEditorPage : ContentPage
{
    private readonly OwnedShipEditorViewModel _viewModel;

    /// <summary>
    /// Constructor — receives the ViewModel from DI, loads XAML, and sets the binding context.
    /// </summary>
    public OwnedShipEditorPage(OwnedShipEditorViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = viewModel;
    }

    /// <summary>Loads the ship data when the page appears.</summary>
    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.LoadShipCommand.ExecuteAsync(null);
    }
}
