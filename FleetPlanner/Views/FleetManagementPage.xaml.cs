using FleetPlanner.ViewModels;

namespace FleetPlanner.Views;

/// <summary>
/// Code-behind for the Fleet Management page — create/edit fleet form.
/// <para>
/// <b>Dual-mode form:</b> This page is used for both creating new fleets (<c>fleetId=0</c>)
/// and editing existing ones (<c>fleetId &gt; 0</c>). The ViewModel's <c>LoadCommand</c>
/// determines which mode to use based on the query parameter.
/// </para>
/// <para>
/// <b>Synchronous Execute vs ExecuteAsync:</b> Unlike other pages that use <c>ExecuteAsync</c>,
/// this page uses <c>Execute(null)</c> (fire-and-forget). This works because the <c>OnAppearing</c>
/// override here is synchronous (<c>void</c>), and the ViewModel's <c>LoadCommand</c> handles
/// its own error states via the <c>IsLoading</c> flag.
/// </para>
/// </summary>
public partial class FleetManagementPage : ContentPage
{
    /// <summary>
    /// Constructor — receives the ViewModel from DI, loads XAML, and sets the binding context.
    /// </summary>
    /// <param name="viewModel">The FleetManagement ViewModel injected by the DI container.</param>
    public FleetManagementPage(FleetManagementViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }

    /// <summary>
    /// Triggers the ViewModel's Load command when the page appears.
    /// <para>
    /// Uses a pattern-match cast (<c>is FleetManagementViewModel vm</c>) to safely access
    /// the typed ViewModel from the untyped <c>BindingContext</c>. This is defensive coding —
    /// the cast should always succeed, but the pattern avoids a potential <c>InvalidCastException</c>.
    /// </para>
    /// </summary>
    protected override void OnAppearing()
    {
        base.OnAppearing();
        if (BindingContext is FleetManagementViewModel vm)
            vm.LoadCommand.Execute(null);
    }
}
