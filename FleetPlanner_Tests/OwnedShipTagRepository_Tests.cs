using FleetPlanner.Models;
using FleetPlanner.Repositories;

using FluentAssertions;

namespace FleetPlanner_Tests;

/// <summary>
/// Tests for <see cref="OwnedShipTagRepository"/> — apply, remove, contextual query.
/// </summary>
public class OwnedShipTagRepository_Tests : IDisposable
{
    private readonly string _dbPath;
    private readonly OwnedShipTagRepository _repo;

    public OwnedShipTagRepository_Tests()
    {
        SQLitePCL.Batteries_V2.Init();
        _dbPath = Path.Combine(Path.GetTempPath(), $"ShipTagRepo_{Guid.NewGuid()}.db3");
        _repo = new OwnedShipTagRepository(_dbPath);
    }

    public void Dispose()
    {
        try { File.Delete(_dbPath); } catch { }
    }

    [Fact]
    public async Task ApplyTag_CreatesRecord()
    {
        var tag = new OwnedShipTag { OwnedShipId = 1, TagKey = "role:activity:escort", Weight = 1 };
        await _repo.ApplyTagAsync(tag);

        var tags = await _repo.GetTagsForOwnedShipAsync(1);
        tags.Should().HaveCount(1);
        tags[0].TagKey.Should().Be("role:activity:escort");
    }

    [Fact]
    public async Task RemoveTag_DeletesCorrectRecord()
    {
        await _repo.ApplyTagAsync(new OwnedShipTag { OwnedShipId = 1, TagKey = "role:activity:escort" });
        await _repo.ApplyTagAsync(new OwnedShipTag { OwnedShipId = 1, TagKey = "role:activity:haul" });

        await _repo.RemoveTagAsync(1, "role:activity:escort", null, null);

        var tags = await _repo.GetTagsForOwnedShipAsync(1);
        tags.Should().HaveCount(1);
        tags[0].TagKey.Should().Be("role:activity:haul");
    }

    [Fact]
    public async Task ContextualTags_FilterByGroupContext()
    {
        // Global tag
        await _repo.ApplyTagAsync(new OwnedShipTag { OwnedShipId = 1, TagKey = "role:activity:escort" });
        // Contextual tag for group 10
        await _repo.ApplyTagAsync(new OwnedShipTag
        {
            OwnedShipId = 1, TagKey = "role:activity:fight", ContextType = "group", ContextId = 10
        });
        // Contextual tag for group 20
        await _repo.ApplyTagAsync(new OwnedShipTag
        {
            OwnedShipId = 1, TagKey = "role:activity:haul", ContextType = "group", ContextId = 20
        });

        // All tags for ship
        var all = await _repo.GetTagsForOwnedShipAsync(1);
        all.Should().HaveCount(3);

        // Only group 10 context
        var group10 = await _repo.GetTagsForOwnedShipAsync(1, "group", 10);
        group10.Should().HaveCount(1);
        group10[0].TagKey.Should().Be("role:activity:fight");
    }

    [Fact]
    public async Task GetOwnedShipsForTag_ReturnsAllAssignments()
    {
        await _repo.ApplyTagAsync(new OwnedShipTag { OwnedShipId = 1, TagKey = "role:activity:escort" });
        await _repo.ApplyTagAsync(new OwnedShipTag { OwnedShipId = 2, TagKey = "role:activity:escort" });
        await _repo.ApplyTagAsync(new OwnedShipTag { OwnedShipId = 3, TagKey = "role:activity:haul" });

        var escorts = await _repo.GetOwnedShipsForTagAsync("role:activity:escort");
        escorts.Should().HaveCount(2);
    }

    [Fact]
    public async Task ReplaceTags_ReplacesGlobalTags_KeepsContextual()
    {
        // Apply global and contextual tags
        await _repo.ApplyTagAsync(new OwnedShipTag { OwnedShipId = 1, TagKey = "role:activity:escort" });
        await _repo.ApplyTagAsync(new OwnedShipTag
        {
            OwnedShipId = 1, TagKey = "role:activity:fight", ContextType = "group", ContextId = 10
        });

        // Replace global tags
        var newTags = new List<OwnedShipTag>
        {
            new() { TagKey = "role:activity:haul", Weight = 1 },
            new() { TagKey = "role:activity:mine", Weight = 2 }
        };
        await _repo.ReplaceTagsAsync(1, newTags);

        var all = await _repo.GetTagsForOwnedShipAsync(1);
        // Should have: 2 new global + 1 contextual
        all.Should().HaveCount(3);
        all.Where(t => t.ContextType == null).Should().HaveCount(2);
        all.Should().Contain(t => t.TagKey == "role:activity:fight" && t.ContextType == "group");
    }
}
