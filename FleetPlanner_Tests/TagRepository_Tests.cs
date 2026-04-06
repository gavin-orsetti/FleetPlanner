using FleetPlanner.Models;
using FleetPlanner.Repositories;
using FleetPlanner.Services;

using FluentAssertions;

namespace FleetPlanner_Tests;

/// <summary>
/// Tests for <see cref="TagRepository"/> and <see cref="DatabaseBootstrapService"/> tag seeding.
/// </summary>
public class TagRepository_Tests : IDisposable
{
    private readonly string _dbPath;
    private readonly TagRepository _repo;

    public TagRepository_Tests()
    {
        SQLitePCL.Batteries_V2.Init();
        _dbPath = Path.Combine(Path.GetTempPath(), $"TagRepo_{Guid.NewGuid()}.db3");
        _repo = new TagRepository(_dbPath);
    }

    public void Dispose()
    {
        try { File.Delete(_dbPath); } catch { }
    }

    [Fact]
    public async Task Bootstrap_SeedsAllSystemTags()
    {
        var bootstrap = new DatabaseBootstrapService(_dbPath);
        await bootstrap.InitialiseAsync();

        var tags = await _repo.GetAllTagsAsync(includeArchived: true);
        var expectedCount = DatabaseBootstrapService.BuildSystemTags().Count;

        tags.Should().HaveCount(expectedCount);
        tags.Should().OnlyContain(t => t.IsSystemDefined);
    }

    [Fact]
    public async Task Bootstrap_IsIdempotent()
    {
        var bootstrap = new DatabaseBootstrapService(_dbPath);
        await bootstrap.InitialiseAsync();
        await bootstrap.InitialiseAsync(); // second call should be a no-op

        var tags = await _repo.GetAllTagsAsync(includeArchived: true);
        var expectedCount = DatabaseBootstrapService.BuildSystemTags().Count;
        tags.Should().HaveCount(expectedCount);
    }

    [Fact]
    public async Task GetTagsByCategory_ReturnsCorrectSubset()
    {
        var bootstrap = new DatabaseBootstrapService(_dbPath);
        await bootstrap.InitialiseAsync();

        var roleTags = await _repo.GetTagsByCategoryAsync("role");
        roleTags.Should().HaveCount(15);
        roleTags.Should().OnlyContain(t => t.Key.StartsWith("role:"));
    }

    [Fact]
    public async Task GetAssignableTagsForScope_FiltersCorrectly()
    {
        var bootstrap = new DatabaseBootstrapService(_dbPath);
        await bootstrap.InitialiseAsync();

        var groupTags = await _repo.GetAssignableTagsForScopeAsync("UserFleetGroup");
        // Only doctrine and constraint tags have UserFleetGroup in AllowedScopes
        groupTags.Should().OnlyContain(t =>
            t.Category == "doctrine" || t.Category == "constraint");
    }

    [Fact]
    public async Task ArchiveTag_HidesFromDefaultQuery()
    {
        var bootstrap = new DatabaseBootstrapService(_dbPath);
        await bootstrap.InitialiseAsync();

        await _repo.ArchiveTagAsync("role:escort");

        var activeRoles = await _repo.GetTagsByCategoryAsync("role");
        activeRoles.Should().NotContain(t => t.Key == "role:escort");

        var allTags = await _repo.GetAllTagsAsync(includeArchived: true);
        allTags.Should().Contain(t => t.Key == "role:escort" && t.IsArchived);
    }

    [Fact]
    public async Task SaveTag_CustomUserTag_CanBeRetrieved()
    {
        // Ensure table exists
        var bootstrap = new DatabaseBootstrapService(_dbPath);
        await bootstrap.InitialiseAsync();

        var custom = new TagDefinition
        {
            Key = "custom:pvp-only",
            DisplayName = "PvP Only",
            Category = "custom",
            Description = "Ship used exclusively in PvP",
            IsSystemDefined = false,
            IsUserEditable = true,
            AllowedScopes = "OwnedShip"
        };
        await _repo.SaveTagAsync(custom);

        var loaded = await _repo.GetTagAsync("custom:pvp-only");
        loaded.Should().NotBeNull();
        loaded!.DisplayName.Should().Be("PvP Only");
        loaded.IsSystemDefined.Should().BeFalse();
    }
}
