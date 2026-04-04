using FleetPlanner.ViewModels;

namespace FleetPlanner.Views;

/// <summary>
/// Code-behind for the Settings page — dark mode toggle, cache refresh, and about section.
/// <para>
/// <b>LoadSettings on appearing:</b> This is critical because <c>Application.Current</c>
/// (needed for theme detection) is not available during DI construction. Loading settings
/// in <c>OnAppearing</c> guarantees the Application instance exists.
/// </para>
/// </summary>
public partial class SettingsPage : ContentPage
{
    private readonly SettingsViewModel _viewModel;

    /// <summary>
    /// Constructor — receives the ViewModel from DI, loads XAML, and sets the binding context.
    /// </summary>
    /// <param name="viewModel">The Settings ViewModel injected by the DI container.</param>
    public SettingsPage(SettingsViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = viewModel;
    }

    /// <summary>
    /// Loads current settings (theme state, cache timestamp) each time the page appears.
    /// </summary>
    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.LoadSettingsCommand.ExecuteAsync(null);
    }
}
