using FleetPlanner.Models;

namespace FleetPlanner.Repositories;

/// <summary>
/// Data access interface for <see cref="TagDefinition"/> records.
/// </summary>
public interface ITagRepository
{
    /// <summary>Returns all tag definitions, optionally including archived ones.</summary>
    Task<List<TagDefinition>> GetAllTagsAsync(bool includeArchived = false);

    /// <summary>Returns all tag definitions in a given category.</summary>
    Task<List<TagDefinition>> GetTagsByCategoryAsync(string category);

    /// <summary>Returns tags whose AllowedScopes contains the given scope ("OwnedShip" or "UserFleetGroup").</summary>
    Task<List<TagDefinition>> GetAssignableTagsForScopeAsync(string scope);

    /// <summary>Returns a single tag definition by key, or null.</summary>
    Task<TagDefinition?> GetTagAsync(string key);

    /// <summary>Upserts a tag definition.</summary>
    Task<int> SaveTagAsync(TagDefinition tag);

    /// <summary>Soft-deletes by setting IsArchived = true.</summary>
    Task ArchiveTagAsync(string key);
}
