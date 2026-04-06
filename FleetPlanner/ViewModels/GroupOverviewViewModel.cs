using System.Collections.ObjectModel;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using FleetPlanner.Models;
using FleetPlanner.Repositories;
using FleetPlanner.Services;
using FleetPlanner.Views;

namespace FleetPlanner.ViewModels;

/// <summary>
/// ViewModel for the Group Overview page — lists all non-archived fleet groups and supports
/// group creation and deletion.
///
/// <para><b>Page lifecycle:</b> <see cref="LoadGroupsCommand"/> runs on every <c>OnAppearing</c>,
/// replacing the <see cref="Groups"/> collection with fresh data from the repository.</para>
///
/// <para><b>Destructive commands:</b> <see cref="DeleteGroupCommand"/> hard-deletes a group
/// after a confirmation dialog ("Permanently delete '[name]'?"). This does NOT archive —
/// it permanently removes the group record. Orphaned contextual tags on ships are not cleaned
/// up (they become dangling references until the next graph build silently ignores them).</para>
///
/// <para><b>Navigation:</b> <see cref="CreateGroupCommand"/> creates a new group and immediately
/// navigates to <c>GroupDetailPage</c> with <see cref="Helpers.QueryParameters.GroupId"/>.
/// <see cref="NavigateToGroupCommand"/> does the same for existing groups.</para>
/// </summary>
public partial class GroupOverviewViewModel : ObservableObject
{
    private readonly IUserFleetGroupRepository _groupRepository;
    private readonly IGraphBuildService _graphBuildService;

    /// <summary>The list of groups currently displayed.</summary>
    [ObservableProperty]
    private ObservableCollection<UserFleetGroup> _groups = [];

    /// <summary>True while loading.</summary>
    [ObservableProperty]
    private bool _isLoading;

    /// <summary>True when the user has no groups.</summary>
    [ObservableProperty]
    private bool _isEmpty;

    /// <summary>
    /// Constructor — receives dependencies from the DI container.
    /// </summary>
    public GroupOverviewViewModel(IUserFleetGroupRepository groupRepository, IGraphBuildService graphBuildService)
    {
        _groupRepository = groupRepository;
        _graphBuildService = graphBuildService;
    }

    /// <summary>Loads all non-archived groups.</summary>
    [RelayCommand]
    private async Task LoadGroupsAsync()
    {
        IsLoading = true;
        try
        {
            var groups = await _groupRepository.GetAllGroupsAsync();
            Groups = new ObservableCollection<UserFleetGroup>(groups);
            IsEmpty = groups.Count == 0;
        }
        finally
        {
            IsLoading = false;
        }
    }

    /// <summary>Creates a new group and navigates to its detail page.</summary>
    [RelayCommand]
    private async Task CreateGroupAsync()
    {
        var name = await Shell.Current.DisplayPromptAsync(
            "New Group", "Enter a name for the new group:",
            accept: "Create", cancel: "Cancel", placeholder: "e.g. Mining Operation");

        if (string.IsNullOrWhiteSpace(name)) return;

        var group = new UserFleetGroup { Name = name };
        await _groupRepository.SaveGroupAsync(group);
        _graphBuildService.InvalidateCache();

        await Shell.Current.GoToAsync(nameof(GroupDetailPage), new Dictionary<string, object>
        {
            { Helpers.QueryParameters.GroupId, group.Id }
        });
    }

    /// <summary>Navigates to a group's detail page.</summary>
    [RelayCommand]
    private async Task NavigateToGroupAsync(UserFleetGroup group)
    {
        if (group is null) return;
        await Shell.Current.GoToAsync(nameof(GroupDetailPage), new Dictionary<string, object>
        {
            { Helpers.QueryParameters.GroupId, group.Id }
        });
    }

    /// <summary>Deletes a group after confirmation and invalidates the graph cache.</summary>
    [RelayCommand]
    private async Task DeleteGroupAsync(UserFleetGroup group)
    {
        if (group is null) return;
        var confirm = await Shell.Current.DisplayAlert("Delete Group",
            $"Permanently delete '{group.Name}'?", "Delete", "Cancel");
        if (!confirm) return;
        await _groupRepository.DeleteGroupAsync(group.Id);
        _graphBuildService.InvalidateCache();
        await LoadGroupsAsync();
    }
}
