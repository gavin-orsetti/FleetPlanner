using FleetPlanner.Models;
using FleetPlanner.Repositories;

using FluentAssertions;

namespace FleetPlanner_Tests;

/// <summary>
/// Tests for <see cref="UserFleetGroupRepository"/> CRUD operations.
/// </summary>
public class UserFleetGroupRepository_Tests : IDisposable
{
    private readonly string _dbPath;
    private readonly UserFleetGroupRepository _repo;

    public UserFleetGroupRepository_Tests()
    {
        SQLitePCL.Batteries_V2.Init();
        _dbPath = Path.Combine(Path.GetTempPath(), $"GroupRepo_{Guid.NewGuid()}.db3");
        _repo = new UserFleetGroupRepository(_dbPath);
    }

    public void Dispose()
    {
        try { File.Delete(_dbPath); } catch { }
    }

    [Fact]
    public async Task SaveAndGet_RoundTrip()
    {
        var group = new UserFleetGroup { Name = "Mining Ops", Description = "Deep space mining", CrewTarget = 6 };
        await _repo.SaveGroupAsync(group);

        group.Id.Should().BeGreaterThan(0);

        var loaded = await _repo.GetGroupAsync(group.Id);
        loaded.Should().NotBeNull();
        loaded!.Name.Should().Be("Mining Ops");
        loaded.CrewTarget.Should().Be(6);
    }

    [Fact]
    public async Task GetAll_ExcludesArchived_ByDefault()
    {
        await _repo.SaveGroupAsync(new UserFleetGroup { Name = "Active" });
        await _repo.SaveGroupAsync(new UserFleetGroup { Name = "Hidden", IsArchived = true });

        var active = await _repo.GetAllGroupsAsync();
        active.Should().HaveCount(1);
        active[0].Name.Should().Be("Active");

        var all = await _repo.GetAllGroupsAsync(includeArchived: true);
        all.Should().HaveCount(2);
    }

    [Fact]
    public async Task Archive_SetsFlag()
    {
        var group = new UserFleetGroup { Name = "Temp" };
        await _repo.SaveGroupAsync(group);

        await _repo.ArchiveGroupAsync(group.Id);

        var loaded = await _repo.GetGroupAsync(group.Id);
        loaded!.IsArchived.Should().BeTrue();
    }

    [Fact]
    public async Task Delete_RemovesRecord()
    {
        var group = new UserFleetGroup { Name = "Doomed" };
        await _repo.SaveGroupAsync(group);

        await _repo.DeleteGroupAsync(group.Id);

        var loaded = await _repo.GetGroupAsync(group.Id);
        loaded.Should().BeNull();
    }
}
