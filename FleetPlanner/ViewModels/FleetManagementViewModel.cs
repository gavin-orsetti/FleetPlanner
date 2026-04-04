using System.Collections.ObjectModel;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using FleetPlanner.Models;
using FleetPlanner.Repositories;

namespace FleetPlanner.ViewModels;

/// <summary>
/// ViewModel for the Fleet Management page — handles creating and editing fleet configurations.
/// <para>
/// <b>Dual mode:</b> This page serves as both "create" and "edit" depending on the <c>fleetId</c>
/// query parameter:
/// <list type="bullet">
///   <item><c>fleetId=0</c> — Create mode: form is blank, <see cref="IsNewFleet"/> is true.</item>
///   <item><c>fleetId=42</c> — Edit mode: form is populated from the existing fleet.</item>
/// </list>
/// This dual-mode pattern avoids duplicating pages for create vs. edit.
/// </para>
/// <para>
/// <b>Fleet intent properties:</b> The fleet's focus, scale, crew count, and declared roles
/// define its "intent" — what the fleet is designed to do. These properties drive the
/// <see cref="IRecommendationService"/> engine: it compares the fleet's intent against
/// the actual ships to find gaps and suggest improvements.
/// </para>
/// </summary>
/// <see href="https://learn.microsoft.com/en-us/dotnet/maui/fundamentals/shell/navigation#process-navigation-data-using-query-property-attributes"/>
/// <see href="https://learn.microsoft.com/en-us/dotnet/communitytoolkit/mvvm/"/>
[QueryProperty(nameof(FleetId), "fleetId")]
public partial class FleetManagementViewModel : ObservableObject
{
    /// <summary>For loading and saving fleet data in SQLite.</summary>
    private readonly IFleetRepository _fleetRepository;

    /// <summary>
    /// The fleet ID from Shell navigation. 0 means "create new"; &gt;0 means "edit existing".
    /// </summary>
    [ObservableProperty]
    private int _fleetId;

    // ── Form fields ──────────────────────────────────────────────────
    // These are bound two-way to UI controls (Entry, Editor, Picker, Stepper).

    /// <summary>Fleet name — bound to an Entry control.</summary>
    [ObservableProperty]
    private string _fleetName = string.Empty;

    /// <summary>Fleet description — bound to an Editor control.</summary>
    [ObservableProperty]
    private string _description = string.Empty;

    /// <summary>
    /// Selected index in the focus Picker. Maps to <see cref="FleetFocus"/> enum values.
    /// <para>
    /// MAUI Pickers bind to integer indices, not enum values directly. The index corresponds
    /// to the enum's underlying int value (e.g., 0 = Multipurpose, 1 = Combat, etc.).
    /// </para>
    /// </summary>
    [ObservableProperty]
    private int _selectedFocusIndex;

    /// <summary>Selected index in the operating scale Picker. Maps to <see cref="FleetOperatingScale"/>.</summary>
    [ObservableProperty]
    private int _selectedScaleIndex;

    /// <summary>Number of available crew/players — bound to a Stepper control.</summary>
    [ObservableProperty]
    private int _availableCrewCount = 1;

    /// <summary>
    /// Display names for the focus Picker. Generated from the <see cref="FleetFocus"/> enum
    /// using <c>Enum.GetNames&lt;T&gt;()</c> — a C# reflection helper that returns enum member names as strings.
    /// </summary>
    [ObservableProperty]
    private ObservableCollection<string> _focusOptions = new(Enum.GetNames<FleetFocus>());

    /// <summary>Display names for the operating scale Picker.</summary>
    [ObservableProperty]
    private ObservableCollection<string> _scaleOptions = new(Enum.GetNames<FleetOperatingScale>());

    /// <summary>
    /// Checkable list of fleet roles. Each role can be toggled on/off independently.
    /// The selected roles are serialized to <see cref="Fleet.DeclaredRolesRaw"/> on save.
    /// </summary>
    [ObservableProperty]
    private ObservableCollection<FleetRoleSelection> _roleSelections = [];

    /// <summary>True while loading fleet data.</summary>
    [ObservableProperty]
    private bool _isLoading;

    /// <summary>True when creating a new fleet (fleetId=0) — hides the Delete button in the View.</summary>
    [ObservableProperty]
    private bool _isNewFleet;

    /// <summary>
    /// Constructor — receives the fleet repository and initializes the role selection checkboxes.
    /// </summary>
    /// <param name="fleetRepository">For loading and saving fleet data.</param>
    public FleetManagementViewModel(IFleetRepository fleetRepository)
    {
        _fleetRepository = fleetRepository;
        InitializeRoleSelections();
    }

    /// <summary>
    /// Creates a <see cref="FleetRoleSelection"/> for each <see cref="FleetRole"/> enum value.
    /// <para>
    /// <c>Enum.GetValues&lt;T&gt;()</c> returns all values of a .NET enum. Each value
    /// gets a checkbox item; when loading an existing fleet, we'll set <c>IsSelected</c>
    /// for roles that are already declared.
    /// </para>
    /// </summary>
    private void InitializeRoleSelections()
    {
        var roles = new ObservableCollection<FleetRoleSelection>();
        foreach (var role in Enum.GetValues<FleetRole>())
        {
            roles.Add(new FleetRoleSelection { Role = role, IsSelected = false });
        }
        RoleSelections = roles;
    }

    /// <summary>
    /// Loads the fleet for editing, or sets defaults for a new fleet.
    /// <para>
    /// <b>Create vs. Edit branching:</b> If <c>FleetId &gt; 0</c>, we load the existing fleet
    /// and populate all form fields. If <c>FleetId == 0</c>, we set sensible defaults
    /// (Multipurpose focus, Solo scale, 1 crew).
    /// </para>
    /// </summary>
    [RelayCommand]
    private async Task LoadAsync()
    {
        IsLoading = true;
        try
        {
            if (FleetId > 0)
            {
                // Edit mode — load existing fleet and populate form fields.
                var fleet = await _fleetRepository.GetFleetAsync(FleetId);
                if (fleet is null) return;

                IsNewFleet = false;
                FleetName = fleet.Name;
                Description = fleet.Description;
                SelectedFocusIndex = fleet.PrimaryFocus;
                SelectedScaleIndex = fleet.OperatingScale;
                AvailableCrewCount = fleet.AvailableCrewCount;

                // Mark the roles that are already declared on this fleet.
                var declaredRoles = fleet.DeclaredRoles;
                foreach (var rs in RoleSelections)
                    rs.IsSelected = declaredRoles.Contains(rs.Role);
            }
            else
            {
                // Create mode — set sensible defaults for a new fleet.
                IsNewFleet = true;
                FleetName = string.Empty;
                Description = string.Empty;
                SelectedFocusIndex = (int)FleetFocus.Multipurpose;
                SelectedScaleIndex = (int)FleetOperatingScale.Solo;
                AvailableCrewCount = 1;
            }
        }
        finally
        {
            IsLoading = false;
        }
    }

    /// <summary>
    /// Saves the fleet (create or update) and navigates back.
    /// <para>
    /// <b>Save pattern:</b> For edits, we reload the fleet from SQLite (to avoid overwriting
    /// fields we didn't display in the form), then apply the form values. For creates, we
    /// start with a fresh <c>new Fleet()</c> whose <c>Id == 0</c> — the repository's
    /// <c>SaveFleetAsync</c> uses InsertOrReplace, which inserts when Id is 0.
    /// </para>
    /// <para>
    /// <b>DeclaredRoles serialization:</b> The selected <see cref="FleetRoleSelection"/> items
    /// are converted to a <c>List&lt;FleetRole&gt;</c> and assigned to <see cref="Fleet.DeclaredRoles"/>,
    /// which internally serializes them to a comma-separated string in <see cref="Fleet.DeclaredRolesRaw"/>.
    /// </para>
    /// </summary>
    [RelayCommand]
    private async Task SaveAsync()
    {
        // Basic validation — don't save a fleet with no name.
        if (string.IsNullOrWhiteSpace(FleetName))
            return;

        Fleet fleet;
        if (FleetId > 0)
        {
            // Re-fetch from DB to preserve any fields not shown in the form.
            fleet = await _fleetRepository.GetFleetAsync(FleetId) ?? new Fleet();
        }
        else
        {
            fleet = new Fleet();
        }

        // Apply form values to the fleet entity.
        fleet.Name = FleetName.Trim();
        fleet.Description = Description?.Trim() ?? string.Empty;
        fleet.PrimaryFocus = SelectedFocusIndex;
        fleet.OperatingScale = SelectedScaleIndex;
        fleet.AvailableCrewCount = AvailableCrewCount;
        // Convert checked role selections back to the Fleet's DeclaredRoles list.
        fleet.DeclaredRoles = RoleSelections
            .Where(rs => rs.IsSelected)
            .Select(rs => rs.Role)
            .ToList();

        await _fleetRepository.SaveFleetAsync(fleet);
        await Shell.Current.GoToAsync("..");
    }

    /// <summary>
    /// Deletes the fleet after user confirmation via a platform-native alert dialog.
    /// <para>
    /// <b>DisplayAlert:</b> <c>Shell.Current.DisplayAlert()</c> shows a platform-native
    /// confirmation dialog. It returns <c>true</c> if the user tapped "Delete", <c>false</c>
    /// if they tapped "Cancel". This is the MAUI equivalent of <c>MessageBox.Show()</c>.
    /// </para>
    /// </summary>
    [RelayCommand]
    private async Task DeleteAsync()
    {
        if (FleetId <= 0) return;

        var confirm = await Shell.Current.DisplayAlert(
            "Delete Fleet",
            $"Are you sure you want to delete '{FleetName}'? All ships in this fleet will be removed.",
            "Delete", "Cancel");

        if (!confirm) return;

        await _fleetRepository.DeleteFleetAsync(FleetId);
        await Shell.Current.GoToAsync("..");
    }

    /// <summary>Navigates back without saving — discards any form changes.</summary>
    [RelayCommand]
    private async Task CancelAsync()
    {
        await Shell.Current.GoToAsync("..");
    }
}

/// <summary>
/// A selectable role item used in the fleet management form's role checklist.
/// <para>
/// <b>Why extend ObservableObject?</b> This class needs <see cref="SetProperty"/> for
/// <see cref="IsSelected"/> so the MAUI binding engine updates the checkbox state in real time.
/// Unlike the ViewModel's fields that use <c>[ObservableProperty]</c>, here we implement the
/// property manually because this is a simple helper class, not a source-generated ViewModel.
/// </para>
/// </summary>
public class FleetRoleSelection : ObservableObject
{
    /// <summary>The fleet role this item represents (e.g., Combat, Mining, Transport).</summary>
    public FleetRole Role { get; set; }

    private bool _isSelected;
    /// <summary>
    /// Whether this role is checked in the UI. Uses <see cref="ObservableObject.SetProperty"/>
    /// to fire <see cref="System.ComponentModel.INotifyPropertyChanged.PropertyChanged"/> when toggled.
    /// </summary>
    public bool IsSelected
    {
        get => _isSelected;
        set => SetProperty(ref _isSelected, value);
    }

    /// <summary>The role name as a string — used as the checkbox label in the View.</summary>
    public string DisplayName => Role.ToString();
}
