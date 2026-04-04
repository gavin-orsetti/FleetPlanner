using System.Collections.ObjectModel;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using FleetPlanner.Models;
using FleetPlanner.Repositories;

namespace FleetPlanner.ViewModels;

/// <summary>
/// ViewModel for the Fleet List page — displays all user-created fleets and supports CRUD operations.
/// <para>
/// <b>Navigation pattern:</b> This is a "list" page in a list→detail navigation flow.
/// Tapping a fleet navigates to <c>FleetDetailPage</c> (read-only view); the "Add" button
/// navigates to <c>FleetManagementPage</c> with <c>fleetId=0</c> (create mode).
/// </para>
/// <para>
/// <b>Shell navigation:</b> <c>Shell.Current.GoToAsync("PageRoute?key=value")</c> is the
/// URI-based navigation system. Query parameters are automatically deserialized into
/// <c>[QueryProperty]</c>-annotated properties on the destination ViewModel.
/// </para>
/// </summary>
/// <see href="https://learn.microsoft.com/en-us/dotnet/maui/fundamentals/shell/navigation"/>
/// <see href="https://learn.microsoft.com/en-us/dotnet/communitytoolkit/mvvm/"/>
public partial class FleetListViewModel : ObservableObject
{
    /// <summary>For loading, creating, and deleting fleets in SQLite.</summary>
    private readonly IFleetRepository _fleetRepository;

    /// <summary>The list of all user-created fleets, bound to a CollectionView in the View.</summary>
    [ObservableProperty]
    private ObservableCollection<Fleet> _fleets = [];

    /// <summary>True while loading fleets — drives a loading indicator in the View.</summary>
    [ObservableProperty]
    private bool _isLoading;

    /// <summary>True when the user has no fleets — shows an empty-state message in the View.</summary>
    [ObservableProperty]
    private bool _isEmpty;

    /// <summary>
    /// Constructor — receives the fleet repository from the DI container.
    /// </summary>
    /// <param name="fleetRepository">For all fleet CRUD operations.</param>
    public FleetListViewModel(IFleetRepository fleetRepository)
    {
        _fleetRepository = fleetRepository;
    }

    /// <summary>
    /// Loads all fleets from the repository and updates the observable collection.
    /// <para>
    /// Replacing the entire <see cref="Fleets"/> collection (rather than clearing and re-adding)
    /// triggers a single change notification, which is more efficient for the MAUI binding engine.
    /// </para>
    /// </summary>
    [RelayCommand]
    private async Task LoadFleetsAsync()
    {
        IsLoading = true;
        try
        {
            var fleets = await _fleetRepository.GetAllFleetsAsync();
            Fleets = new ObservableCollection<Fleet>(fleets);
            IsEmpty = Fleets.Count == 0;
        }
        finally
        {
            IsLoading = false;
        }
    }

    /// <summary>
    /// Navigates to the FleetManagementPage in "create" mode.
    /// <para>
    /// <c>fleetId=0</c> signals that this is a new fleet (Id == 0 means "not yet saved" —
    /// the same convention used throughout the app and in <see cref="FleetManagementViewModel"/>).
    /// </para>
    /// </summary>
    [RelayCommand]
    private async Task AddFleetAsync()
    {
        // fleetId=0 tells FleetManagementPage to create a new fleet instead of editing an existing one.
        await Shell.Current.GoToAsync("FleetManagementPage?fleetId=0");
    }

    /// <summary>
    /// Deletes a fleet and reloads the list.
    /// <para>
    /// <b>Cascade delete:</b> <see cref="IFleetRepository.DeleteFleetAsync"/> also removes
    /// all associated FleetShip records, so we don't need to clean them up separately.
    /// After deletion, we reload the full list to keep the UI in sync.
    /// </para>
    /// </summary>
    /// <param name="fleet">The fleet to delete (passed from the View's command parameter).</param>
    [RelayCommand]
    private async Task DeleteFleetAsync(Fleet fleet)
    {
        if (fleet is null)
            return;
        await _fleetRepository.DeleteFleetAsync(fleet.Id);
        await LoadFleetsAsync();
    }

    /// <summary>
    /// Navigates to the FleetDetailPage for the selected fleet.
    /// <para>
    /// The fleet's ID is passed as a query parameter. On the destination page,
    /// <c>[QueryProperty(nameof(FleetId), "fleetId")]</c> automatically deserializes
    /// the string "fleetId" into the ViewModel's <c>FleetId</c> int property.
    /// </para>
    /// </summary>
    /// <param name="fleet">The fleet to view (passed from the View's command parameter).</param>
    [RelayCommand]
    private async Task GoToFleetDetailAsync(Fleet fleet)
    {
        if (fleet is null)
            return;
        await Shell.Current.GoToAsync($"FleetDetailPage?fleetId={fleet.Id}");
    }
}
