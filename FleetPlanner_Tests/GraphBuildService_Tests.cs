using FleetPlanner.Models;
using FleetPlanner.Repositories;
using FleetPlanner.Services;

using FluentAssertions;

using NSubstitute;

namespace FleetPlanner_Tests;

/// <summary>
/// Integration tests for <see cref="GraphBuildService"/> — verifies that the in-memory
/// <see cref="FleetPlanner.Graph.FleetGraph"/> is built correctly from repository data.
/// Uses real SQLite repositories with temp databases and a mocked <see cref="IShipDataService"/>.
/// </summary>
public class GraphBuildService_Tests : IDisposable
{
    private readonly string _dbPath;
    private readonly OwnedShipRepository _ownedShipRepo;
    private readonly OwnedShipTagRepository _shipTagRepo;
    private readonly TagRepository _tagRepo;
    private readonly GroupTagRepository _groupTagDefRepo;
    private readonly UserFleetGroupRepository _groupRepo;
    private readonly UserFleetGroupTagRepository _groupTagRepo;
    private readonly IShipDataService _shipDataService;
    private readonly GraphBuildService _service;

    public GraphBuildService_Tests()
    {
        SQLitePCL.Batteries_V2.Init();
        _dbPath = Path.Combine(Path.GetTempPath(), $"GraphBuild_{Guid.NewGuid()}.db3");
        _ownedShipRepo = new OwnedShipRepository(_dbPath);
        _shipTagRepo = new OwnedShipTagRepository(_dbPath);
        _tagRepo = new TagRepository(_dbPath);
        _groupTagDefRepo = new GroupTagRepository(_dbPath);
        _groupRepo = new UserFleetGroupRepository(_dbPath);
        _groupTagRepo = new UserFleetGroupTagRepository(_dbPath);
        _shipDataService = Substitute.For<IShipDataService>();

        _service = new GraphBuildService(
            _ownedShipRepo, _shipTagRepo, _tagRepo,
            _groupTagDefRepo, _groupRepo, _groupTagRepo, _shipDataService);
    }

    public void Dispose()
    {
        try { File.Delete(_dbPath); } catch { }
    }

    private async Task SeedTags()
    {
        var bootstrap = new DatabaseBootstrapService(_dbPath);
        await bootstrap.InitialiseAsync();
    }

    private void SetupCatalogue(params Ship[] ships)
    {
        _shipDataService.GetAllShipsAsync(Arg.Any<bool>())
            .Returns(ships.ToList());
    }

    [Fact]
    public async Task ShipWithNoTags_CreatesShipNodeWithEmptyTagLists()
    {
        await SeedTags();
        SetupCatalogue(new Ship { Id = 42, Name = "Arrow", CrewMin = 1 });

        var ship = new OwnedShip { ShipId = 42, Callsign = "My Arrow" };
        await _ownedShipRepo.SaveOwnedShipAsync(ship);

        var graph = await _service.BuildGraphAsync();

        graph.Ships.Should().HaveCount(1);
        graph.Ships[0].GlobalTags.Should().BeEmpty();
        graph.Ships[0].ContextualTags.Should().BeEmpty();
    }

    [Fact]
    public async Task ShipInTwoGroups_AppearsInBothGroupNodes()
    {
        await SeedTags();
        SetupCatalogue(new Ship { Id = 42, Name = "Cutlass Black", CrewMin = 2 });

        var ship = new OwnedShip { ShipId = 42 };
        await _ownedShipRepo.SaveOwnedShipAsync(ship);

        var group1 = new UserFleetGroup { Name = "Combat" };
        await _groupRepo.SaveGroupAsync(group1);
        var group2 = new UserFleetGroup { Name = "Hauling" };
        await _groupRepo.SaveGroupAsync(group2);

        // Contextual tags that establish membership in both groups
        await _shipTagRepo.ApplyTagAsync(new OwnedShipTag
        {
            OwnedShipId = ship.Id, TagKey = "intent:activity:escort",
            ContextType = "group", ContextId = group1.Id
        });
        await _shipTagRepo.ApplyTagAsync(new OwnedShipTag
        {
            OwnedShipId = ship.Id, TagKey = "intent:activity:haul",
            ContextType = "group", ContextId = group2.Id
        });

        var graph = await _service.BuildGraphAsync();

        graph.Groups.Should().HaveCount(2);
        graph.Groups[0].MemberShips.Should().HaveCount(1);
        graph.Groups[1].MemberShips.Should().HaveCount(1);
        graph.Groups[0].MemberShips[0].OwnedShipId.Should().Be(ship.Id);
        graph.Groups[1].MemberShips[0].OwnedShipId.Should().Be(ship.Id);
    }

    [Fact]
    public async Task ContextualRoleTags_ResolveDifferentlyPerGroup()
    {
        await SeedTags();
        SetupCatalogue(new Ship { Id = 42, Name = "Cutlass Black", CrewMin = 2 });

        var ship = new OwnedShip { ShipId = 42 };
        await _ownedShipRepo.SaveOwnedShipAsync(ship);

        var group1 = new UserFleetGroup { Name = "Combat" };
        await _groupRepo.SaveGroupAsync(group1);
        var group2 = new UserFleetGroup { Name = "Hauling" };
        await _groupRepo.SaveGroupAsync(group2);

        await _shipTagRepo.ApplyTagAsync(new OwnedShipTag
        {
            OwnedShipId = ship.Id, TagKey = "intent:activity:escort",
            ContextType = "group", ContextId = group1.Id
        });
        await _shipTagRepo.ApplyTagAsync(new OwnedShipTag
        {
            OwnedShipId = ship.Id, TagKey = "intent:activity:haul",
            ContextType = "group", ContextId = group2.Id
        });

        var graph = await _service.BuildGraphAsync();

        var shipNode = graph.Ships.Single();
        shipNode.ContextualTags.Should().ContainKey(group1.Id);
        shipNode.ContextualTags.Should().ContainKey(group2.Id);
        shipNode.ContextualTags[group1.Id].Should().Contain(t => t.Definition.Key == "intent:activity:escort");
        shipNode.ContextualTags[group2.Id].Should().Contain(t => t.Definition.Key == "intent:activity:haul");
    }

    [Fact]
    public async Task ArchivedGroup_DoesNotAppearInGraph()
    {
        await SeedTags();
        SetupCatalogue();

        var active = new UserFleetGroup { Name = "Active" };
        await _groupRepo.SaveGroupAsync(active);
        var archived = new UserFleetGroup { Name = "Archived" };
        await _groupRepo.SaveGroupAsync(archived);
        await _groupRepo.ArchiveGroupAsync(archived.Id);

        var graph = await _service.BuildGraphAsync();

        graph.Groups.Should().HaveCount(1);
        graph.Groups[0].Group.Name.Should().Be("Active");
    }

    [Fact]
    public async Task GroupSpecificTags_ResolveFromGroupTagDefinition()
    {
        await SeedTags();
        SetupCatalogue(new Ship { Id = 42, Name = "Javelin", CrewMin = 80 });

        var ship = new OwnedShip { ShipId = 42 };
        await _ownedShipRepo.SaveOwnedShipAsync(ship);

        var group = new UserFleetGroup { Name = "Main Fleet" };
        await _groupRepo.SaveGroupAsync(group);

        // Add ship to group via sentinel tag
        await _shipTagRepo.ApplyTagAsync(new OwnedShipTag
        {
            OwnedShipId = ship.Id,
            TagKey = "status:placeholder",
            ContextType = "group",
            ContextId = group.Id,
            AppliedBySystem = true
        });

        // Assign a group-specific doctrine tag (exists only in GroupTagDefinition)
        await _groupTagRepo.ApplyTagAsync(new UserFleetGroupTag
        {
            UserFleetGroupId = group.Id,
            TagKey = "doctrine:primary-arm",
            Weight = 1
        });

        var graph = await _service.BuildGraphAsync();

        graph.Groups.Should().HaveCount(1);
        var groupNode = graph.Groups[0];
        groupNode.DoctrineAndFocusTags.Should().Contain(t => t.Definition.Key == "doctrine:primary-arm");
    }

    [Fact]
    public async Task CacheInvalidation_RebuildsGraph()
    {
        await SeedTags();
        SetupCatalogue(new Ship { Id = 1, Name = "Arrow", CrewMin = 1 });

        await _ownedShipRepo.SaveOwnedShipAsync(new OwnedShip { ShipId = 1 });

        var g1 = await _service.GetOrRebuildAsync();
        g1.Ships.Should().HaveCount(1);

        // Add another ship
        await _ownedShipRepo.SaveOwnedShipAsync(new OwnedShip { ShipId = 1 });

        // Cached version should still have 1
        var g2 = await _service.GetOrRebuildAsync();
        g2.Ships.Should().HaveCount(1);

        // Invalidate and rebuild
        _service.InvalidateCache();
        var g3 = await _service.GetOrRebuildAsync();
        g3.Ships.Should().HaveCount(2);
    }
}
