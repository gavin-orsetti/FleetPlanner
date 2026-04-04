using FleetPlanner.ViewModels;

namespace FleetPlanner.Views;

/// <summary>
/// Code-behind for the Dashboard page — displays fleet statistics and LiveCharts2 charts.
/// <para>
/// <b>MAUI code-behind pattern:</b> In MVVM, the code-behind is intentionally minimal.
/// It only handles two things:
/// <list type="number">
///   <item>Setting up the ViewModel as the <see cref="BindableObject.BindingContext"/> in the constructor.</item>
///   <item>Triggering data loading in <see cref="OnAppearing"/>.</item>
/// </list>
/// All business logic lives in <see cref="DashboardViewModel"/> — the View only connects it.
/// </para>
/// <para>
/// <b>Constructor injection:</b> The ViewModel is injected by the DI container. Because both
/// <c>DashboardPage</c> and <c>DashboardViewModel</c> are registered in <c>MauiProgram.cs</c>,
/// MAUI automatically resolves and passes the ViewModel when it creates this page.
/// </para>
/// </summary>
/// <see href="https://learn.microsoft.com/en-us/dotnet/maui/xaml/fundamentals/mvvm"/>
public partial class DashboardPage : ContentPage
{
    private readonly DashboardViewModel _viewModel;

    /// <summary>
    /// Constructor — receives the ViewModel from DI, loads XAML, and sets the binding context.
    /// </summary>
    /// <param name="viewModel">The Dashboard ViewModel injected by the DI container.</param>
    public DashboardPage(DashboardViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = viewModel;
    }

    /// <summary>
    /// Called every time the page becomes visible (including back-navigation).
    /// <para>
    /// <b>OnAppearing pattern:</b> This is the standard MAUI lifecycle hook for loading data.
    /// Using <c>OnAppearing</c> (not the constructor) ensures data is refreshed each time
    /// the user navigates to this page, not just the first time. The <c>async void</c> pattern
    /// is acceptable here because lifecycle events cannot return <c>Task</c>.
    /// </para>
    /// <para>
    /// <b>ExecuteAsync vs Execute:</b> <c>LoadDataCommand.ExecuteAsync(null)</c> awaits the
    /// async operation, ensuring any exceptions propagate. Using <c>Execute(null)</c> would
    /// fire-and-forget, potentially swallowing errors.
    /// </para>
    /// </summary>
    /// <see href="https://learn.microsoft.com/en-us/dotnet/maui/fundamentals/app-lifecycle"/>
    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.LoadDataCommand.ExecuteAsync(null);
    }
}
