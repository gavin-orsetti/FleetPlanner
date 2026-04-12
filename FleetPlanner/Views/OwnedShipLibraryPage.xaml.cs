using FleetPlanner.ViewModels;

namespace FleetPlanner.Views;

/// <summary>
/// Code-behind for the Owned Ship Library page — lists all user-owned ships.
/// </summary>
public partial class OwnedShipLibraryPage : ContentPage
{
    private readonly OwnedShipLibraryViewModel _viewModel;

    /// <summary>
    /// Constructor — receives the ViewModel from DI, loads XAML, and sets the binding context.
    /// </summary>
    public OwnedShipLibraryPage(OwnedShipLibraryViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = viewModel;
    }

    /// <summary>Loads owned ships each time the page appears.</summary>
    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.LoadOwnedShipsCommand.ExecuteAsync(null);
    }
}
