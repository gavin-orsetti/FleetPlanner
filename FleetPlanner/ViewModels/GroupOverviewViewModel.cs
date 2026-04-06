using System.Collections.ObjectModel;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using FleetPlanner.Models;
using FleetPlanner.Repositories;
using FleetPlanner.Views;

namespace FleetPlanner.ViewModels;

/// <summary>
/// ViewModel for the Group Overview page — lists all user-created fleet groups.
/// </summary>
public partial class GroupOverviewViewModel : ObservableObject
{
    private readonly IUserFleetGroupRepository _groupRepository;

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
    public GroupOverviewViewModel(IUserFleetGroupRepository groupRepository)
    {
        _groupRepository = groupRepository;
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

    /// <summary>Deletes a group after confirmation.</summary>
    [RelayCommand]
    private async Task DeleteGroupAsync(UserFleetGroup group)
    {
        if (group is null) return;
        var confirm = await Shell.Current.DisplayAlert("Delete Group",
            $"Permanently delete '{group.Name}'?", "Delete", "Cancel");
        if (!confirm) return;
        await _groupRepository.DeleteGroupAsync(group.Id);
        await LoadGroupsAsync();
    }
}
