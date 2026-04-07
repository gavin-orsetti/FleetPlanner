using System.Collections.ObjectModel;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using FleetPlanner.Helpers;
using FleetPlanner.Models;
using FleetPlanner.Repositories;
using FleetPlanner.Services;

namespace FleetPlanner.ViewModels;

/// <summary>
/// ViewModel for the reusable tag picker page — presents all assignable tags as
/// selectable chips grouped by category, with optional weight selection for role tags.
///
/// <para><b>Navigation parameters:</b> Receives up to four query parameters via Shell navigation:
/// <list type="bullet">
///   <item><see cref="TagPickerOwnedShipId"/> — ship to edit tags for (0 if editing group tags only)</item>
///   <item><see cref="TagPickerGroupId"/> — group context (0 if editing global ship tags)</item>
///   <item><see cref="TagPickerContextType"/> — "group" for contextual tags, empty for global</item>
///   <item><see cref="TagPickerContextId"/> — group Id when context type is "group"</item>
/// </list></para>
///
/// <para><b>Save behaviour:</b> Depending on scope, calls the appropriate repository method to
/// replace tags, then invalidates the graph cache and navigates back.</para>
/// </summary>
[QueryProperty(nameof(TagPickerOwnedShipId), QueryParameters.TagPickerOwnedShipId)]
[QueryProperty(nameof(TagPickerGroupId), QueryParameters.TagPickerGroupId)]
[QueryProperty(nameof(TagPickerContextType), QueryParameters.TagPickerContextType)]
[QueryProperty(nameof(TagPickerContextId), QueryParameters.TagPickerContextId)]
public partial class TagPickerViewModel : ObservableObject
{
    private readonly ITagRepository _tagRepository;
    private readonly IOwnedShipTagRepository _ownedShipTagRepository;
    private readonly IUserFleetGroupTagRepository _groupTagRepository;
    private readonly IGraphBuildService _graphBuildService;

    /// <summary>OwnedShip Id (0 = not editing ship tags).</summary>
    [ObservableProperty]
    private int _tagPickerOwnedShipId;

    /// <summary>Group Id (0 = not editing group tags).</summary>
    [ObservableProperty]
    private int _tagPickerGroupId;

    /// <summary>Context type for scoped tags (e.g. "group"). Empty = global.</summary>
    [ObservableProperty]
    private string _tagPickerContextType = string.Empty;

    /// <summary>Context Id for scoped tags. 0 = global.</summary>
    [ObservableProperty]
    private int _tagPickerContextId;

    /// <summary>All selectable tag items grouped by category.</summary>
    [ObservableProperty]
    private ObservableCollection<SelectableTagItem> _allTags = [];

    /// <summary>Filtered tag items based on search text.</summary>
    [ObservableProperty]
    private ObservableCollection<TagCategoryGroup> _groupedTags = [];

    /// <summary>Search/filter text.</summary>
    [ObservableProperty]
    private string _searchText = string.Empty;

    /// <summary>True while loading.</summary>
    [ObservableProperty]
    private bool _isLoading;

    /// <summary>True while saving.</summary>
    [ObservableProperty]
    private bool _isSaving;

    /// <summary>Title reflecting the editing context.</summary>
    [ObservableProperty]
    private string _pageTitle = "Select Tags";

    /// <summary>
    /// Constructor — receives dependencies from the DI container.
    /// </summary>
    public TagPickerViewModel(
        ITagRepository tagRepository,
        IOwnedShipTagRepository ownedShipTagRepository,
        IUserFleetGroupTagRepository groupTagRepository,
        IGraphBuildService graphBuildService)
    {
        _tagRepository = tagRepository;
        _ownedShipTagRepository = ownedShipTagRepository;
        _groupTagRepository = groupTagRepository;
        _graphBuildService = graphBuildService;
    }

    /// <summary>Rebuilds the filtered/grouped tag list when search text changes.</summary>
    partial void OnSearchTextChanged(string value) => ApplyFilter();

    /// <summary>Loads available tags and pre-selects currently assigned ones.</summary>
    [RelayCommand]
    private async Task LoadTagsAsync()
    {
        IsLoading = true;
        try
        {
            // Determine scope for filtering tags
            string scope;
            bool isGroupTagEditing = TagPickerOwnedShipId <= 0 && TagPickerGroupId > 0;
            bool isContextualShipEditing = TagPickerOwnedShipId > 0 && !string.IsNullOrEmpty(TagPickerContextType);
            bool isGlobalShipEditing = TagPickerOwnedShipId > 0 && string.IsNullOrEmpty(TagPickerContextType);

            if (isGroupTagEditing)
            {
                scope = "UserFleetGroup";
                PageTitle = "Group Tags";
            }
            else if (isContextualShipEditing)
            {
                scope = "OwnedShip";
                PageTitle = "Ship Role in Group";
            }
            else
            {
                scope = "OwnedShip";
                PageTitle = "Ship Tags";
            }

            // Load all assignable tags for the determined scope
            var availableTags = await _tagRepository.GetAssignableTagsForScopeAsync(scope);

            // Build selectable items
            var selectableItems = availableTags
                .Select(td => new SelectableTagItem
                {
                    TagKey = td.Key,
                    DisplayName = td.DisplayName,
                    Category = td.Category,
                    Description = td.Description,
                    ColorHex = td.ColorHex,
                    Weight = 1
                })
                .ToList();

            // Pre-select currently applied tags
            if (isGroupTagEditing)
            {
                var currentGroupTags = await _groupTagRepository.GetTagsForGroupAsync(TagPickerGroupId);
                foreach (var gt in currentGroupTags)
                {
                    var match = selectableItems.FirstOrDefault(s => s.TagKey == gt.TagKey);
                    if (match is not null)
                    {
                        match.IsSelected = true;
                        match.Weight = gt.Weight;
                    }
                }
            }
            else if (isContextualShipEditing)
            {
                var currentContextTags = await _ownedShipTagRepository.GetTagsForOwnedShipAsync(
                    TagPickerOwnedShipId, TagPickerContextType, TagPickerContextId);
                foreach (var ct in currentContextTags)
                {
                    var match = selectableItems.FirstOrDefault(s => s.TagKey == ct.TagKey);
                    if (match is not null)
                    {
                        match.IsSelected = true;
                        match.Weight = ct.Weight;
                    }
                }
            }
            else if (isGlobalShipEditing)
            {
                // Load only global tags (no context)
                var allShipTags = await _ownedShipTagRepository.GetTagsForOwnedShipAsync(TagPickerOwnedShipId);
                var globalTags = allShipTags.Where(t => t.ContextType is null).ToList();
                foreach (var gt in globalTags)
                {
                    var match = selectableItems.FirstOrDefault(s => s.TagKey == gt.TagKey);
                    if (match is not null)
                    {
                        match.IsSelected = true;
                        match.Weight = gt.Weight;
                    }
                }
            }

            MainThread.BeginInvokeOnMainThread(() =>
                AllTags = new ObservableCollection<SelectableTagItem>(selectableItems));
            ApplyFilter();
        }
        finally
        {
            IsLoading = false;
        }
    }

    /// <summary>Toggles a tag's selected state.</summary>
    [RelayCommand]
    private void ToggleTag(SelectableTagItem item)
    {
        if (item is null) return;
        item.IsSelected = !item.IsSelected;

        // Reset weight when deselecting
        if (!item.IsSelected)
            item.Weight = 1;
    }

    /// <summary>Sets the weight on a role tag.</summary>
    [RelayCommand]
    private void SetWeight(SelectableTagItem item)
    {
        if (item is null) return;
        // Cycle through weights: 1 → 2 → 3 → 1
        item.Weight = item.Weight >= 3 ? 1 : item.Weight + 1;
    }

    /// <summary>Saves the selected tags to the appropriate repository and navigates back.</summary>
    [RelayCommand]
    private async Task SaveAsync()
    {
        if (IsSaving) return;
        IsSaving = true;
        try
        {
            var selected = AllTags.Where(t => t.IsSelected).ToList();

            bool isGroupTagEditing = TagPickerOwnedShipId <= 0 && TagPickerGroupId > 0;
            bool isContextualShipEditing = TagPickerOwnedShipId > 0 && !string.IsNullOrEmpty(TagPickerContextType);

            if (isGroupTagEditing)
            {
                // Replace all group tags
                var newGroupTags = selected.Select(s => new UserFleetGroupTag
                {
                    UserFleetGroupId = TagPickerGroupId,
                    TagKey = s.TagKey,
                    Weight = s.Weight
                });
                await _groupTagRepository.ReplaceTagsAsync(TagPickerGroupId, newGroupTags);
            }
            else if (isContextualShipEditing)
            {
                // Contextual tags: remove old contextual tags for this context, add new ones
                var currentContextTags = await _ownedShipTagRepository.GetTagsForOwnedShipAsync(
                    TagPickerOwnedShipId, TagPickerContextType, TagPickerContextId);

                // Remove old contextual tags
                foreach (var old in currentContextTags)
                {
                    await _ownedShipTagRepository.RemoveTagAsync(
                        TagPickerOwnedShipId, old.TagKey, TagPickerContextType, TagPickerContextId);
                }

                // Add new contextual tags
                foreach (var s in selected)
                {
                    await _ownedShipTagRepository.ApplyTagAsync(new OwnedShipTag
                    {
                        OwnedShipId = TagPickerOwnedShipId,
                        TagKey = s.TagKey,
                        ContextType = TagPickerContextType,
                        ContextId = TagPickerContextId,
                        Weight = s.Weight
                    });
                }
            }
            else
            {
                // Global ship tags: use ReplaceTagsAsync
                var newGlobalTags = selected.Select(s => new OwnedShipTag
                {
                    OwnedShipId = TagPickerOwnedShipId,
                    TagKey = s.TagKey,
                    Weight = s.Weight
                });
                await _ownedShipTagRepository.ReplaceTagsAsync(TagPickerOwnedShipId, newGlobalTags);
            }

            _graphBuildService.InvalidateCache();
            await Shell.Current.GoToAsync("..");
        }
        finally
        {
            IsSaving = false;
        }
    }

    /// <summary>Navigates back without saving.</summary>
    [RelayCommand]
    private async Task CancelAsync()
    {
        await Shell.Current.GoToAsync("..");
    }

    private void ApplyFilter()
    {
        // Guard: nothing to filter if tags haven't loaded yet
        if (AllTags is null || AllTags.Count == 0)
        {
            MainThread.BeginInvokeOnMainThread(() =>
                GroupedTags = new ObservableCollection<TagCategoryGroup>());
            return;
        }

        var filtered = AllTags.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(SearchText))
        {
            var search = SearchText.Trim();
            filtered = filtered.Where(t =>
                t.DisplayName.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                t.TagKey.Contains(search, StringComparison.OrdinalIgnoreCase));
        }

        var groups = filtered
            .GroupBy(t => t.Category)
            .OrderBy(g => g.Key)
            .Select(g => new TagCategoryGroup(g.Key, g.ToList()))
            .Where(g => g.Tags.Count > 0) // Skip empty groups
            .ToList();

        MainThread.BeginInvokeOnMainThread(() =>
            GroupedTags = new ObservableCollection<TagCategoryGroup>(groups));
    }
}

/// <summary>
/// A tag item that tracks selection state and weight for the tag picker.
/// Extends <see cref="ObservableObject"/> so the UI updates when
/// <see cref="IsSelected"/> or <see cref="Weight"/> changes.
/// </summary>
public partial class SelectableTagItem : ObservableObject
{
    /// <summary>Tag definition key.</summary>
    public string TagKey { get; set; } = string.Empty;

    /// <summary>Human-readable display name.</summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>Tag category (role, doctrine, etc.).</summary>
    public string Category { get; set; } = string.Empty;

    /// <summary>Description subtitle.</summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>Hex colour for the chip.</summary>
    public string ColorHex { get; set; } = "#8890a8";

    /// <summary>Whether this tag is currently selected.</summary>
    [ObservableProperty]
    private bool _isSelected;

    /// <summary>Priority weight (1 = Primary, 2 = Secondary, 3 = Tertiary).</summary>
    [ObservableProperty]
    private int _weight = 1;

    /// <summary>Whether this is a role tag (shows weight picker).</summary>
    public bool IsRoleTag => Category == "role";

    /// <summary>Human-readable weight label.</summary>
    public string WeightLabel => Weight switch { 1 => "● Primary", 2 => "◉ Secondary", 3 => "○ Tertiary", _ => "" };

    /// <summary>Resolved chip colour.</summary>
    public Color ChipColor => Color.FromArgb(ColorHex);

    /// <summary>Notifies dependent properties when weight changes.</summary>
    partial void OnWeightChanged(int value) => OnPropertyChanged(nameof(WeightLabel));
}

/// <summary>
/// Groups selectable tag items by category for display in the picker.
/// </summary>
public class TagCategoryGroup
{
    /// <summary>The category name (e.g. "role", "doctrine").</summary>
    public string CategoryName { get; }

    /// <summary>Display-friendly capitalised category name.</summary>
    public string DisplayCategory { get; }

    /// <summary>Tags in this category.</summary>
    public List<SelectableTagItem> Tags { get; }

    /// <summary>Creates a new category group.</summary>
    public TagCategoryGroup(string categoryName, List<SelectableTagItem> tags)
    {
        CategoryName = categoryName;
        DisplayCategory = char.ToUpperInvariant(categoryName[0]) + categoryName[1..];
        Tags = tags;
    }
}
