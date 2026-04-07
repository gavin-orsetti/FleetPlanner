using System.Collections.ObjectModel;
using System.Text.RegularExpressions;

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
[QueryProperty(nameof(TagPickerIsGroupContext), QueryParameters.TagPickerIsGroupContext)]
public partial class TagPickerViewModel : ObservableObject
{
    private readonly ITagRepository _tagRepository;
    private readonly IGroupTagRepository _groupTagDefinitionRepository;
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

    /// <summary>Whether the picker is selecting group-level tags (from GroupTagDefinition table) rather than ship tags.</summary>
    [ObservableProperty]
    private string _tagPickerIsGroupContext = "false";

    /// <summary>Parsed boolean: true when picking group tags from the GroupTagDefinition table.</summary>
    private bool IsGroupContext => string.Equals(TagPickerIsGroupContext, "true", StringComparison.OrdinalIgnoreCase);

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

    /// <summary>True when the search text doesn't exactly match any existing tag, enabling the "Create tag" affordance.</summary>
    [ObservableProperty]
    private bool _showCreateTag;

    /// <summary>
    /// Constructor — receives dependencies from the DI container.
    /// </summary>
    public TagPickerViewModel(
        ITagRepository tagRepository,
        IGroupTagRepository groupTagDefinitionRepository,
        IOwnedShipTagRepository ownedShipTagRepository,
        IUserFleetGroupTagRepository groupTagRepository,
        IGraphBuildService graphBuildService)
    {
        _tagRepository = tagRepository;
        _groupTagDefinitionRepository = groupTagDefinitionRepository;
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
            bool isGroupTagEditing = TagPickerOwnedShipId <= 0 && TagPickerGroupId > 0;
            bool isContextualShipEditing = TagPickerOwnedShipId > 0 && !string.IsNullOrEmpty(TagPickerContextType);
            bool isGlobalShipEditing = TagPickerOwnedShipId > 0 && string.IsNullOrEmpty(TagPickerContextType);

            if (isGroupTagEditing)
                PageTitle = "Group Tags";
            else if (isContextualShipEditing)
                PageTitle = "Ship Role in Group";
            else
                PageTitle = "Ship Tags";

            // Load tags from the appropriate table based on context.
            // When IsGroupContext is true, load from GroupTagDefinition (group-level tags).
            // Otherwise load from TagDefinition (ship-level tags).
            List<SelectableTagItem> selectableItems;
            if (IsGroupContext)
            {
                var groupTags = await _groupTagDefinitionRepository.GetAllTagsAsync(includeArchived: false);
                selectableItems = groupTags
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
            }
            else
            {
                var availableTags = await _tagRepository.GetAllTagsAsync(includeArchived: false);
                selectableItems = availableTags
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
            }

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

            // Populate AllTags synchronously on main thread, THEN apply filter.
            // The previous code used BeginInvokeOnMainThread (fire-and-forget)
            // followed by ApplyFilter(), so ApplyFilter ran against an empty
            // collection before the UI thread callback executed.
            AllTags = new ObservableCollection<SelectableTagItem>(selectableItems);
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
            {
                GroupedTags = new ObservableCollection<TagCategoryGroup>();
                ShowCreateTag = false;
            });
            return;
        }

        var filtered = AllTags.AsEnumerable();
        bool hasExactMatch = true;

        if (!string.IsNullOrWhiteSpace(SearchText))
        {
            var search = SearchText.Trim();
            filtered = filtered.Where(t =>
                t.DisplayName.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                t.TagKey.Contains(search, StringComparison.OrdinalIgnoreCase));

            // Show "Create tag" when no tag display name matches exactly
            hasExactMatch = AllTags.Any(t =>
                t.DisplayName.Equals(search, StringComparison.OrdinalIgnoreCase));
        }

        var groups = filtered
            .GroupBy(t => t.Category)
            .OrderBy(g => CategorySortOrder(g.Key))
            .Select(g => new TagCategoryGroup(g.Key, g.ToList()))
            .Where(g => g.Count > 0) // Skip empty groups
            .ToList();

        // Insert a doctrine section divider before the first doctrine:* group
        var firstDoctrineIdx = groups.FindIndex(g => g.CategoryName.StartsWith("doctrine:", StringComparison.Ordinal));
        if (firstDoctrineIdx >= 0)
        {
            groups.Insert(firstDoctrineIdx, new TagCategoryGroup("__doctrine_header__", []));
        }

        MainThread.BeginInvokeOnMainThread(() =>
        {
            GroupedTags = new ObservableCollection<TagCategoryGroup>(groups);
            ShowCreateTag = !string.IsNullOrWhiteSpace(SearchText) && !hasExactMatch;
        });
    }

    /// <summary>
    /// Opens an inline creation flow to create a custom tag from the current search text.
    /// Collects category and optional description via platform prompts, validates uniqueness,
    /// persists the new <see cref="TagDefinition"/>, and adds it to the picker pre-selected.
    /// </summary>
    [RelayCommand]
    private async Task CreateTagAsync()
    {
        var page = Shell.Current.CurrentPage;
        if (page is null || string.IsNullOrWhiteSpace(SearchText)) return;

        var displayName = SearchText.Trim();

        // --- Step 1: Confirm / edit display name ---
        var editedName = await page.DisplayPromptAsync(
            "Create Custom Tag",
            "Custom tags work best when they describe intent, role, or constraint — not ship names or one-off notes. System tags use stable keys; yours will too once created.",
            accept: "Next",
            cancel: "Cancel",
            initialValue: displayName,
            maxLength: 60);

        if (string.IsNullOrWhiteSpace(editedName)) return;
        displayName = editedName.Trim();

        // --- Step 2: Pick category ---
        var categories = IsGroupContext
            ? new[] { "role", "ctx", "doctrine:weight", "doctrine:frequency", "doctrine:purpose",
                      "doctrine:autonomy", "doctrine:flexibility", "doctrine:lifecycle", "status", "custom" }
            : new[] { "role", "ctx", "doctrine:weight", "doctrine:frequency", "doctrine:purpose",
                      "doctrine:autonomy", "doctrine:flexibility", "doctrine:retention", "doctrine:lifecycle", "status", "custom" };
        var chosenCategory = await page.DisplayActionSheet(
            "Choose a category", "Cancel", null, categories);

        if (string.IsNullOrWhiteSpace(chosenCategory) || chosenCategory == "Cancel") return;

        // --- Step 3: Optional description ---
        var description = await page.DisplayPromptAsync(
            "Description (optional)",
            "A short description for this tag.",
            accept: "Create",
            cancel: "Skip",
            maxLength: 120) ?? string.Empty;

        // --- Step 4: Generate key and validate uniqueness ---
        var slug = Slugify(displayName);
        var key = $"{chosenCategory}:{slug}";

        var existingTag = AllTags.FirstOrDefault(t =>
            t.TagKey.Equals(key, StringComparison.OrdinalIgnoreCase));
        if (existingTag is not null)
        {
            await page.DisplayAlert("Duplicate",
                $"A tag with key \"{key}\" already exists. Adjust the display name and try again.", "OK");
            return;
        }

        // Double-check against the appropriate database table
        if (IsGroupContext)
        {
            var dbGroupTag = await _groupTagDefinitionRepository.GetTagByKeyAsync(key);
            if (dbGroupTag is not null)
            {
                await page.DisplayAlert("Duplicate",
                    $"A tag with key \"{key}\" already exists in the database. Adjust the display name and try again.", "OK");
                return;
            }
        }
        else
        {
            var dbTag = await _tagRepository.GetTagAsync(key);
            if (dbTag is not null)
            {
                await page.DisplayAlert("Duplicate",
                    $"A tag with key \"{key}\" already exists in the database. Adjust the display name and try again.", "OK");
                return;
            }
        }

        // --- Step 5: Resolve color from category ---
        var colorHex = CategoryColor(chosenCategory);

        // --- Step 6: Create and save to the appropriate table ---
        if (IsGroupContext)
        {
            var newGroupTag = new GroupTagDefinition
            {
                Key = key,
                DisplayName = displayName,
                Category = chosenCategory,
                Description = description,
                ColorHex = colorHex,
                SortOrder = 999,
                IsSystemDefined = false,
                IsUserEditable = true,
                IsArchived = false,
                AllowedScopes = "UserFleetGroup"
            };
            await _groupTagDefinitionRepository.SaveTagAsync(newGroupTag);
        }
        else
        {
            var newTag = new TagDefinition
            {
                Key = key,
                DisplayName = displayName,
                Category = chosenCategory,
                Description = description,
                ColorHex = colorHex,
                SortOrder = 999,
                IsSystemDefined = false,
                IsUserEditable = true,
                IsArchived = false,
                AllowedScopes = "OwnedShip,UserFleetGroup"
            };
            await _tagRepository.SaveTagAsync(newTag);
        }
        _graphBuildService.InvalidateCache();

        // --- Step 7: Add to picker, pre-selected ---
        var selectableItem = new SelectableTagItem
        {
            TagKey = key,
            DisplayName = displayName,
            Category = chosenCategory,
            Description = description,
            ColorHex = colorHex,
            IsSelected = true,
            Weight = 1
        };

        MainThread.BeginInvokeOnMainThread(() =>
        {
            AllTags.Add(selectableItem);
            SearchText = string.Empty; // clears filter, shows all tags including the new one
        });

        await page.DisplayAlert("Tag Created", $"Tag \"{displayName}\" created and applied.", "OK");
    }

    /// <summary>
    /// Converts a display name into a lowercase hyphenated slug suitable for tag keys.
    /// Strips all characters except letters, digits, and hyphens.
    /// </summary>
    /// <example><c>Slugify("My Ship Role")</c> → <c>"my-ship-role"</c></example>
    internal static string Slugify(string input) =>
        Regex.Replace(
            input.Trim().ToLowerInvariant().Replace(" ", "-"),
            @"[^a-z0-9\-]", "");

    /// <summary>
    /// Returns the standard hex colour for a given tag category, matching the seeded palette.
    /// Falls back to gray for unknown categories.
    /// </summary>
    private static string CategoryColor(string category) => category switch
    {
        "role"                 => "#C4706A",
        "ctx"                  => "#3A9CB8",
        "doctrine:weight"      => "#B87040",
        "doctrine:frequency"   => "#7A8499",
        "doctrine:purpose"     => "#8B66B8",
        "doctrine:autonomy"    => "#3A9CB8",
        "doctrine:flexibility" => "#4A9E6B",
        "doctrine:retention"   => "#C4706A",
        "doctrine:lifecycle"   => "#6B7A8B",
        "status"               => "#B8913A",
        _                      => "#6B7A8B"   // custom / unknown
    };

    /// <summary>
    /// Returns a stable sort index so categories appear in a logical order:
    /// role → ctx → doctrine sub-dimensions → status → custom.
    /// </summary>
    private static int CategorySortOrder(string category) => category switch
    {
        "role"                 => 0,
        "ctx"                  => 1,
        "doctrine:weight"      => 10,
        "doctrine:frequency"   => 11,
        "doctrine:purpose"     => 12,
        "doctrine:autonomy"    => 13,
        "doctrine:flexibility" => 14,
        "doctrine:retention"   => 15,
        "doctrine:lifecycle"   => 16,
        "status"               => 20,
        _                      => 30 // custom / unknown
    };

    /// <summary>
    /// Returns a display-friendly name for a tag category.
    /// </summary>
    private static string CategoryDisplayName(string category) => category switch
    {
        "role"                 => "Role",
        "ctx"                  => "Context",
        "doctrine:weight"      => "Weight",
        "doctrine:frequency"   => "Frequency",
        "doctrine:purpose"     => "Purpose",
        "doctrine:autonomy"    => "Autonomy",
        "doctrine:flexibility" => "Flexibility",
        "doctrine:retention"   => "Retention",
        "doctrine:lifecycle"   => "Lifecycle",
        "status"               => "Status",
        _                      => "Custom"
    };
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
    public string ColorHex { get; set; } = "#7A8499";

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

    /// <summary>Resolved chip colour (full opacity, used for border/stroke).</summary>
    public Color ChipColor => Color.FromArgb(ColorHex);

    /// <summary>Chip fill colour with 30% alpha for unselected background.</summary>
    public Color ChipFillColor => Color.FromArgb("4D" + (ColorHex?.TrimStart('#') ?? "6B7A8B"));

    /// <summary>Notifies dependent properties when weight changes.</summary>
    partial void OnWeightChanged(int value) => OnPropertyChanged(nameof(WeightLabel));
}

/// <summary>
/// Groups selectable tag items by category for display in the picker.
/// Extends <see cref="List{T}"/> so it can serve as a native MAUI
/// <see cref="CollectionView"/> grouped data source (each group *is* the item list).
/// </summary>
public class TagCategoryGroup : List<SelectableTagItem>
{
    /// <summary>The category name (e.g. "role", "doctrine:weight").</summary>
    public string CategoryName { get; }

    /// <summary>Display-friendly capitalised category name.</summary>
    public string DisplayCategory { get; }

    /// <summary>Creates a new category group from an existing list of tags.</summary>
    public TagCategoryGroup(string categoryName, List<SelectableTagItem> tags) : base(tags)
    {
        CategoryName = categoryName;
        DisplayCategory = FormatCategoryDisplay(categoryName);
    }

    private static string FormatCategoryDisplay(string category) => category switch
    {
        "ctx"                    => "Context",
        "__doctrine_header__"    => "— Doctrine —",
        "doctrine:weight"        => "Weight",
        "doctrine:frequency"     => "Frequency",
        "doctrine:purpose"       => "Purpose",
        "doctrine:autonomy"      => "Autonomy",
        "doctrine:flexibility"   => "Flexibility",
        "doctrine:retention"     => "Retention",
        "doctrine:lifecycle"     => "Lifecycle",
        _                        => char.ToUpperInvariant(category[0]) + category[1..]
    };

    /// <summary>Whether this group is the doctrine section divider header (no items).</summary>
    public bool IsDoctrineSectionHeader => CategoryName == "__doctrine_header__";

    /// <summary>Whether this group is a doctrine sub-dimension group.</summary>
    public bool IsDoctrineSubDimension => CategoryName.StartsWith("doctrine:", StringComparison.Ordinal);
}
