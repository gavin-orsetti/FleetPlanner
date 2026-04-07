using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using FleetPlanner.Helpers;
using FleetPlanner.Models;
using FleetPlanner.Repositories;
using FleetPlanner.Services;

namespace FleetPlanner.ViewModels;

/// <summary>
/// ViewModel for the Tag Editor page — a form for creating or editing a
/// user-defined <see cref="TagDefinition"/>. System tags
/// (<see cref="TagDefinition.IsSystemDefined"/> = <see langword="true"/>)
/// are displayed read-only with no save or archive buttons.
///
/// <para><b>Create mode:</b> When <see cref="TagEditorTagKey"/> is null or empty
/// the page is in create mode. The user fills in display name, category,
/// description, and optional colour. The key is auto-generated as
/// <c>"category:slug"</c>.</para>
///
/// <para><b>Edit mode:</b> When <see cref="TagEditorTagKey"/> is set the tag
/// is loaded from the repository. Key and category are immutable after creation;
/// only display name, description, and colour are editable (for non-system tags).</para>
/// </summary>
[QueryProperty(nameof(TagEditorTagKey), QueryParameters.TagEditorTagKey)]
[QueryProperty(nameof(TagEditorIsGroupContext), QueryParameters.TagEditorIsGroupContext)]
public partial class TagEditorViewModel : ObservableObject
{
    private readonly ITagRepository _tagRepository;
    private readonly IGroupTagRepository _groupTagRepository;
    private readonly IGraphBuildService _graphBuildService;

    /// <summary>Tag key passed via navigation — null/empty = create mode.</summary>
    [ObservableProperty]
    private string? _tagEditorTagKey;

    /// <summary>Whether editing/creating a group tag (from GroupTagDefinition table) rather than a ship tag.</summary>
    [ObservableProperty]
    private string _tagEditorIsGroupContext = "false";

    /// <summary>Parsed boolean: true when editing/creating group tags.</summary>
    private bool IsGroupContext => string.Equals(TagEditorIsGroupContext, "true", StringComparison.OrdinalIgnoreCase);

    /// <summary>Human-readable display name.</summary>
    [ObservableProperty]
    private string _displayName = string.Empty;

    /// <summary>Tag category (role, ctx, doctrine, status, custom).</summary>
    [ObservableProperty]
    private string _category = string.Empty;

    /// <summary>Tag description.</summary>
    [ObservableProperty]
    private string _description = string.Empty;

    /// <summary>Optional hex colour for chip display.</summary>
    [ObservableProperty]
    private string _colorHex = "#7A8499";

    /// <summary>Auto-generated preview of the tag key.</summary>
    [ObservableProperty]
    private string _keyPreview = string.Empty;

    /// <summary>Page title reflecting create or edit mode.</summary>
    [ObservableProperty]
    private string _pageTitle = "Create Tag";

    /// <summary>True when editing an existing tag (not create mode).</summary>
    [ObservableProperty]
    private bool _isEditMode;

    /// <summary>True when the tag is system-defined (all fields read-only).</summary>
    [ObservableProperty]
    private bool _isSystemTag;

    /// <summary>True when the form is editable (not a system tag).</summary>
    [ObservableProperty]
    private bool _isEditable = true;

    /// <summary>True while loading.</summary>
    [ObservableProperty]
    private bool _isLoading;

    /// <summary>True while saving.</summary>
    [ObservableProperty]
    private bool _isSaving;

    /// <summary>Validation error message.</summary>
    [ObservableProperty]
    private string _errorMessage = string.Empty;

    /// <summary>True when there is an error message to display.</summary>
    public bool HasError => !string.IsNullOrEmpty(ErrorMessage);

    /// <summary>Notifies the UI when error state changes.</summary>
    partial void OnErrorMessageChanged(string value) => OnPropertyChanged(nameof(HasError));

    /// <summary>Index of the selected category in the picker.</summary>
    [ObservableProperty]
    private int _selectedCategoryIndex = -1;

    /// <summary>Available category values for the picker.</summary>
    public List<string> Categories { get; } =
        ["role", "ctx", "doctrine", "status", "custom"];

    /// <summary>
    /// Constructor — receives dependencies from the DI container.
    /// </summary>
    public TagEditorViewModel(ITagRepository tagRepository, IGroupTagRepository groupTagRepository, IGraphBuildService graphBuildService)
    {
        _tagRepository = tagRepository;
        _groupTagRepository = groupTagRepository;
        _graphBuildService = graphBuildService;
    }

    /// <summary>Regenerates key preview when display name changes.</summary>
    partial void OnDisplayNameChanged(string value) => UpdateKeyPreview();

    /// <summary>Regenerates key preview when category changes.</summary>
    partial void OnCategoryChanged(string value) => UpdateKeyPreview();

    /// <summary>Updates category string when the picker index changes.</summary>
    partial void OnSelectedCategoryIndexChanged(int value)
    {
        if (value >= 0 && value < Categories.Count)
            Category = Categories[value];
    }

    /// <summary>Loads the tag for editing, or initialises a blank form for creation.</summary>
    [RelayCommand]
    private async Task LoadAsync()
    {
        IsLoading = true;
        try
        {
            if (!string.IsNullOrEmpty(TagEditorTagKey))
            {
                // Edit mode — load from the appropriate table
                string? displayName = null, category = null, description = null, colorHex = null, key = null;
                bool isSystem = false;

                if (IsGroupContext)
                {
                    var groupTag = await _groupTagRepository.GetTagByKeyAsync(TagEditorTagKey);
                    if (groupTag is not null)
                    {
                        displayName = groupTag.DisplayName;
                        category = groupTag.Category;
                        description = groupTag.Description;
                        colorHex = groupTag.ColorHex;
                        key = groupTag.Key;
                        isSystem = groupTag.IsSystemDefined;
                    }
                }
                else
                {
                    var tag = await _tagRepository.GetTagAsync(TagEditorTagKey);
                    if (tag is not null)
                    {
                        displayName = tag.DisplayName;
                        category = tag.Category;
                        description = tag.Description;
                        colorHex = tag.ColorHex;
                        key = tag.Key;
                        isSystem = tag.IsSystemDefined;
                    }
                }

                if (displayName is null)
                {
                    ErrorMessage = $"Tag \"{TagEditorTagKey}\" not found.";
                    return;
                }

                IsEditMode = true;
                IsSystemTag = isSystem;
                IsEditable = !isSystem;
                PageTitle = isSystem ? $"View: {displayName}" : $"Edit: {displayName}";

                DisplayName = displayName;
                Category = category!;
                Description = description ?? string.Empty;
                ColorHex = colorHex ?? "#7A8499";
                KeyPreview = key!;

                var idx = Categories.IndexOf(category!);
                SelectedCategoryIndex = idx >= 0 ? idx : -1;
            }
            else
            {
                // Create mode
                IsEditMode = false;
                IsSystemTag = false;
                IsEditable = true;
                PageTitle = IsGroupContext ? "Create Group Tag" : "Create Tag";
                SelectedCategoryIndex = Categories.IndexOf("custom");
            }
        }
        finally
        {
            IsLoading = false;
        }
    }

    /// <summary>Validates and saves the tag definition.</summary>
    [RelayCommand]
    private async Task SaveAsync()
    {
        if (IsSaving || !IsEditable) return;

        // Validate
        ErrorMessage = string.Empty;
        if (string.IsNullOrWhiteSpace(DisplayName))
        {
            ErrorMessage = "Display name is required.";
            return;
        }

        if (string.IsNullOrWhiteSpace(Category))
        {
            ErrorMessage = "Please select a category.";
            return;
        }

        IsSaving = true;
        try
        {
            if (IsEditMode)
            {
                // Edit mode: update only mutable fields in the appropriate table
                if (IsGroupContext)
                {
                    var existing = await _groupTagRepository.GetTagByKeyAsync(TagEditorTagKey!);
                    if (existing is null)
                    {
                        ErrorMessage = "Tag no longer exists.";
                        return;
                    }
                    existing.DisplayName = DisplayName.Trim();
                    existing.Description = Description.Trim();
                    existing.ColorHex = string.IsNullOrWhiteSpace(ColorHex) ? "#7A8499" : ColorHex.Trim();
                    await _groupTagRepository.SaveTagAsync(existing);
                }
                else
                {
                    var existing = await _tagRepository.GetTagAsync(TagEditorTagKey!);
                    if (existing is null)
                    {
                        ErrorMessage = "Tag no longer exists.";
                        return;
                    }
                    existing.DisplayName = DisplayName.Trim();
                    existing.Description = Description.Trim();
                    existing.ColorHex = string.IsNullOrWhiteSpace(ColorHex) ? "#7A8499" : ColorHex.Trim();
                    await _tagRepository.SaveTagAsync(existing);
                }
            }
            else
            {
                var slug = StringHelpers.Slugify(DisplayName);
                var key = $"{Category}:{slug}";

                if (IsGroupContext)
                {
                    // Create group tag
                    var existingTag = await _groupTagRepository.GetTagByKeyAsync(key);
                    if (existingTag is not null)
                    {
                        ErrorMessage = $"A tag with key \"{key}\" already exists.";
                        return;
                    }

                    var newTag = new GroupTagDefinition
                    {
                        Key = key,
                        DisplayName = DisplayName.Trim(),
                        Category = Category,
                        Description = Description.Trim(),
                        ColorHex = string.IsNullOrWhiteSpace(ColorHex) ? CategoryColor(Category) : ColorHex.Trim(),
                        SortOrder = 999,
                        IsSystemDefined = false,
                        IsUserEditable = true,
                        IsArchived = false,
                        AllowedScopes = "UserFleetGroup"
                    };
                    await _groupTagRepository.SaveTagAsync(newTag);
                }
                else
                {
                    // Create ship tag
                    var existingTag = await _tagRepository.GetTagAsync(key);
                    if (existingTag is not null)
                    {
                        ErrorMessage = $"A tag with key \"{key}\" already exists.";
                        return;
                    }

                    var newTag = new TagDefinition
                    {
                        Key = key,
                        DisplayName = DisplayName.Trim(),
                        Category = Category,
                        Description = Description.Trim(),
                        ColorHex = string.IsNullOrWhiteSpace(ColorHex) ? CategoryColor(Category) : ColorHex.Trim(),
                        SortOrder = 999,
                        IsSystemDefined = false,
                        IsUserEditable = true,
                        IsArchived = false,
                        AllowedScopes = "OwnedShip,UserFleetGroup"
                    };
                    await _tagRepository.SaveTagAsync(newTag);
                }
            }

            _graphBuildService.InvalidateCache();
            await Shell.Current.GoToAsync("..");
        }
        finally
        {
            IsSaving = false;
        }
    }

    /// <summary>Archives the current tag and navigates back. Only available in edit mode for non-system tags.</summary>
    [RelayCommand]
    private async Task ArchiveAsync()
    {
        if (!IsEditMode || IsSystemTag || string.IsNullOrEmpty(TagEditorTagKey)) return;

        var page = Shell.Current.CurrentPage;
        var confirmed = await page.DisplayAlert(
            "Archive Tag",
            $"Are you sure you want to archive \"{DisplayName}\"? It will be hidden from pickers but existing assignments will remain.",
            "Archive", "Cancel");

        if (!confirmed) return;

        if (IsGroupContext)
            await _groupTagRepository.ArchiveTagAsync(TagEditorTagKey);
        else
            await _tagRepository.ArchiveTagAsync(TagEditorTagKey);
        _graphBuildService.InvalidateCache();
        await Shell.Current.GoToAsync("..");
    }

    /// <summary>Navigates back without saving.</summary>
    [RelayCommand]
    private async Task CancelAsync()
    {
        await Shell.Current.GoToAsync("..");
    }

    private void UpdateKeyPreview()
    {
        if (IsEditMode)
            return; // Key is immutable in edit mode

        if (!string.IsNullOrWhiteSpace(DisplayName) && !string.IsNullOrWhiteSpace(Category))
        {
            var slug = StringHelpers.Slugify(DisplayName);
            KeyPreview = $"{Category}:{slug}";
        }
        else
        {
            KeyPreview = string.Empty;
        }
    }

    /// <summary>
    /// Returns the standard hex colour for a given tag category, matching the seeded palette.
    /// </summary>
    private static string CategoryColor(string category) => category switch
    {
        "role"     => "#C4706A",
        "ctx"      => "#3A9CB8",
        "doctrine" => "#8B66B8",
        "status"   => "#B8913A",
        _          => "#6B7A8B"   // custom / unknown
    };
}
