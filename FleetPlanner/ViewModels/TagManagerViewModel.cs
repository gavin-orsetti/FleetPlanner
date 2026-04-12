using System.Collections.ObjectModel;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using FleetPlanner.Helpers;
using FleetPlanner.Models;
using FleetPlanner.Repositories;
using FleetPlanner.Services;
using FleetPlanner.Views;

namespace FleetPlanner.ViewModels;

/// <summary>
/// ViewModel for the Tag Manager page — a browsable, searchable list of all
/// <see cref="TagDefinition"/> records grouped by category. This is the authoritative
/// place for users to inspect, create, and archive tag definitions.
///
/// <para><b>Groups:</b> Tags are displayed in collapsible category groups via
/// <see cref="GroupedTags"/>. Empty groups are omitted.</para>
///
/// <para><b>Archive toggle:</b> By default archived tags are hidden. The user can
/// toggle <see cref="ShowArchived"/> to include them.</para>
/// </summary>
public partial class TagManagerViewModel : ObservableObject
{
    private readonly ITagRepository _tagRepository;
    private readonly IGroupTagRepository _groupTagRepository;
    private readonly IGraphBuildService _graphBuildService;

    private List<TagManagerItem> _allItems = [];

    /// <summary>Tags grouped by category for display.</summary>
    [ObservableProperty]
    private ObservableCollection<TagManagerCategoryGroup> _groupedTags = [];

    /// <summary>Search/filter text — filters by display name or key.</summary>
    [ObservableProperty]
    private string _searchText = string.Empty;

    /// <summary>Whether to include archived tags in the listing.</summary>
    [ObservableProperty]
    private bool _showArchived;

    /// <summary>When true, shows group tags from GroupTagDefinition table; otherwise ship tags from TagDefinition.</summary>
    [ObservableProperty]
    private bool _showGroupTags;

    /// <summary>True while loading data.</summary>
    [ObservableProperty]
    private bool _isLoading;

    /// <summary>
    /// Constructor — receives dependencies from the DI container.
    /// </summary>
    public TagManagerViewModel(ITagRepository tagRepository, IGroupTagRepository groupTagRepository, IGraphBuildService graphBuildService)
    {
        _tagRepository = tagRepository;
        _groupTagRepository = groupTagRepository;
        _graphBuildService = graphBuildService;
    }

    /// <summary>Rebuilds the filtered/grouped list when search text changes.</summary>
    partial void OnSearchTextChanged(string value) => ApplyFilter();

    /// <summary>Reloads tags when the archived toggle changes.</summary>
    partial void OnShowArchivedChanged(bool value) => LoadTagsCommand.Execute(null);

    /// <summary>Reloads tags when the group toggle changes.</summary>
    partial void OnShowGroupTagsChanged(bool value) => LoadTagsCommand.Execute(null);

    /// <summary>Toggles between ship tags and group tags and reloads.</summary>
    [RelayCommand]
    private void ToggleTagType()
    {
        ShowGroupTags = !ShowGroupTags;
    }

    /// <summary>Loads all tag definitions from the appropriate repository.</summary>
    [RelayCommand]
    private async Task LoadTagsAsync()
    {
        IsLoading = true;
        try
        {
            if (ShowGroupTags)
            {
                var groupTags = await _groupTagRepository.GetAllTagsAsync(includeArchived: ShowArchived);
                _allItems = groupTags.Select(td => new TagManagerItem
                {
                    TagKey = td.Key,
                    DisplayName = td.DisplayName,
                    Category = td.Category,
                    Description = td.Description,
                    ColorHex = td.ColorHex,
                    IsSystemDefined = td.IsSystemDefined,
                    IsArchived = td.IsArchived
                }).ToList();
            }
            else
            {
                var tags = await _tagRepository.GetAllTagsAsync(includeArchived: ShowArchived);
                _allItems = tags.Select(td => new TagManagerItem
                {
                    TagKey = td.Key,
                    DisplayName = td.DisplayName,
                    Category = td.Category,
                    Description = td.Description,
                    ColorHex = td.ColorHex,
                    IsSystemDefined = td.IsSystemDefined,
                    IsArchived = td.IsArchived
                }).ToList();
            }

            ApplyFilter();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error loading tags: {ex}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    /// <summary>Navigates to TagEditorPage in create mode (no key).</summary>
    [RelayCommand]
    private async Task CreateTagAsync()
    {
        await Shell.Current.GoToAsync(nameof(TagEditorPage), new Dictionary<string, object>
        {
            [QueryParameters.TagEditorIsGroupContext] = ShowGroupTags ? "true" : "false"
        });
    }

    /// <summary>Navigates to TagEditorPage in edit mode with the given tag key.</summary>
    [RelayCommand]
    private async Task EditTagAsync(TagManagerItem item)
    {
        if (item is null) return;
        await Shell.Current.GoToAsync(nameof(TagEditorPage), new Dictionary<string, object>
        {
            [QueryParameters.TagEditorTagKey] = item.TagKey,
            [QueryParameters.TagEditorIsGroupContext] = ShowGroupTags ? "true" : "false"
        });
    }

    /// <summary>Archives a tag (soft-delete) and reloads the list.</summary>
    [RelayCommand]
    private async Task ArchiveTagAsync(TagManagerItem item)
    {
        if (item is null) return;

        var page = Shell.Current.CurrentPage;
        var action = item.IsArchived ? "restore" : "archive";
        var confirmed = await page.DisplayAlert(
            $"Confirm {char.ToUpperInvariant(action[0])}{action[1..]}",
            $"Are you sure you want to {action} \"{item.DisplayName}\"?",
            "Yes", "Cancel");

        if (!confirmed) return;

        if (ShowGroupTags)
        {
            if (item.IsArchived)
            {
                var tag = await _groupTagRepository.GetTagByKeyAsync(item.TagKey);
                if (tag is not null)
                {
                    tag.IsArchived = false;
                    await _groupTagRepository.SaveTagAsync(tag);
                }
            }
            else
            {
                await _groupTagRepository.ArchiveTagAsync(item.TagKey);
            }
        }
        else
        {
            if (item.IsArchived)
            {
                var tag = await _tagRepository.GetTagAsync(item.TagKey);
                if (tag is not null)
                {
                    tag.IsArchived = false;
                    await _tagRepository.SaveTagAsync(tag);
                }
            }
            else
            {
                await _tagRepository.ArchiveTagAsync(item.TagKey);
            }
        }

        _graphBuildService.InvalidateCache();
        await LoadTagsAsync();
    }

    private void ApplyFilter()
    {
        var filtered = _allItems.AsEnumerable();

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
            .Select(g => new TagManagerCategoryGroup(g.Key, g.ToList()))
            .Where(g => g.Tags.Count > 0)
            .ToList();

        GroupedTags = new ObservableCollection<TagManagerCategoryGroup>(groups);
    }
}

/// <summary>
/// Display item for a tag definition in the Tag Manager list.
/// </summary>
public class TagManagerItem
{
    /// <summary>Tag definition key (e.g. "intent:activity:escort").</summary>
    public string TagKey { get; set; } = string.Empty;

    /// <summary>Human-readable display name.</summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>Tag category.</summary>
    public string Category { get; set; } = string.Empty;

    /// <summary>Description text.</summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>Hex colour for chip display.</summary>
    public string ColorHex { get; set; } = "#7A8499";

    /// <summary>Whether this is a system-defined tag.</summary>
    public bool IsSystemDefined { get; set; }

    /// <summary>Whether this tag is archived.</summary>
    public bool IsArchived { get; set; }

    /// <summary>Whether this is a user-created tag (not system-defined).</summary>
    public bool IsUserDefined => !IsSystemDefined;

    /// <summary>Badge text: "System" or "Custom".</summary>
    public string BadgeText => IsSystemDefined ? "System" : "Custom";

    /// <summary>Resolved chip colour (full opacity, used for border/stroke).</summary>
    public Color ChipColor => Color.FromArgb(ColorHex);

    /// <summary>Chip fill colour with 30% alpha for background.</summary>
    public Color ChipFillColor => Color.FromArgb("4D" + (ColorHex?.TrimStart('#') ?? "6B7A8B"));

    /// <summary>Truncated description for list display.</summary>
    public string ShortDescription => Description.Length > 60 ? Description[..57] + "..." : Description;
}

/// <summary>
/// Groups tag manager items by category for display.
/// </summary>
public class TagManagerCategoryGroup
{
    /// <summary>The category name (e.g. "doctrine:value", "intent:activity").</summary>
    public string CategoryName { get; }

    /// <summary>Display-friendly capitalised category name.</summary>
    public string DisplayCategory { get; }

    /// <summary>Tags in this category.</summary>
    public List<TagManagerItem> Tags { get; }

    /// <summary>Creates a new category group.</summary>
    public TagManagerCategoryGroup(string categoryName, List<TagManagerItem> tags)
    {
        CategoryName = categoryName;
        DisplayCategory = FormatCategoryDisplay(categoryName);
        Tags = tags;
    }

    private static string FormatCategoryDisplay(string category) => category switch
    {
        "doctrine"               => "Doctrine",
        "doctrine:value"         => "Doctrine: Fleet Value",
        "doctrine:frequency"     => "Doctrine: Frequency",
        "doctrine:investment"    => "Doctrine: Investment",
        "doctrine:identity"      => "Doctrine: Identity",
        "doctrine:structural"    => "Doctrine: Structural Role",
        "intent:activity"        => "Intent: Activity",
        "intent:economy"         => "Intent: Economy",
        "intent:crew"            => "Intent: Crew Commitment",
        "intent:legal"           => "Intent: Legal Stance",
        "intent:org"             => "Intent: Org Context",
        "intent:mission"         => "Intent: Mission",
        "potency:capacity"       => "Potency: Capacity",
        "potency:reach"          => "Potency: Reach",
        "potency:resilience"     => "Potency: Resilience",
        "potency:footprint"      => "Potency: Footprint",
        "status"                 => "Status",
        "status:lifecycle"       => "Status: Lifecycle",
        "status:modifier"        => "Status: Modifier",
        "tradeoff"               => "Tradeoff",
        _                        => char.ToUpperInvariant(category[0]) + category[1..]
    };
}
