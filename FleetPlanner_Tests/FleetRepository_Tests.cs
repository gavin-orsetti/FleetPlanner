using FleetPlanner.Models;
using FleetPlanner.Repositories;

using FluentAssertions;

namespace FleetPlanner_Tests;

public class FleetRepository_Tests : IDisposable
{
    private readonly FleetRepository _repo;
    private readonly string _dbPath;

    public FleetRepository_Tests()
    {
        SQLitePCL.Batteries_V2.Init();
        _dbPath = Path.Combine(Path.GetTempPath(), $"FleetPlannerTest_{Guid.NewGuid()}.db3");
        _repo = new FleetRepository(_dbPath);
    }

    public void Dispose()
    {
        try { File.Delete(_dbPath); } catch { }
    }

    [Fact]
    public async Task SaveFleetAsync_NewFleet_AssignsIdAndPersists()
    {
        var fleet = new Fleet { Name = "Alpha Fleet" };

        await _repo.SaveFleetAsync(fleet);

        fleet.Id.Should().BeGreaterThan(0);
        var retrieved = await _repo.GetFleetAsync(fleet.Id);
        retrieved.Should().NotBeNull();
        retrieved!.Name.Should().Be("Alpha Fleet");
    }

    [Fact]
    public async Task GetAllFleetsAsync_MultipleFleetsInserted_ReturnsAll()
    {
        await _repo.SaveFleetAsync(new Fleet { Name = "Bravo" });
        await _repo.SaveFleetAsync(new Fleet { Name = "Alpha" });

        var fleets = await _repo.GetAllFleetsAsync();

        fleets.Should().HaveCount(2);
        fleets.Select(f => f.Name).Should().ContainInOrder("Alpha", "Bravo");
    }

    [Fact]
    public async Task SaveFleetAsync_ExistingFleet_Updates()
    {
        var fleet = new Fleet { Name = "Original" };
        await _repo.SaveFleetAsync(fleet);

        fleet.Name = "Updated";
        await _repo.SaveFleetAsync(fleet);

        var retrieved = await _repo.GetFleetAsync(fleet.Id);
        retrieved!.Name.Should().Be("Updated");
    }

    [Fact]
    public async Task DeleteFleetAsync_ExistingFleet_RemovesIt()
    {
        var fleet = new Fleet { Name = "ToDelete" };
        await _repo.SaveFleetAsync(fleet);

        await _repo.DeleteFleetAsync(fleet.Id);

        var retrieved = await _repo.GetFleetAsync(fleet.Id);
        retrieved.Should().BeNull();
    }

    [Fact]
    public async Task GetFleetAsync_NonExistentId_ReturnsNull()
    {
        var result = await _repo.GetFleetAsync(99999);

        result.Should().BeNull();
    }

    [Fact]
    public async Task DeleteFleetAsync_CascadesToFleetShips()
    {
        var fleet = new Fleet { Name = "WithShips" };
        await _repo.SaveFleetAsync(fleet);

        var ship1 = new FleetShip { FleetId = fleet.Id, ShipId = 100, Callsign = "Wing1" };
        var ship2 = new FleetShip { FleetId = fleet.Id, ShipId = 200, Callsign = "Wing2" };
        await _repo.SaveFleetShipAsync(ship1);
        await _repo.SaveFleetShipAsync(ship2);

        var shipsBefore = await _repo.GetFleetShipsAsync(fleet.Id);
        shipsBefore.Should().HaveCount(2);

        await _repo.DeleteFleetAsync(fleet.Id);

        var shipsAfter = await _repo.GetFleetShipsAsync(fleet.Id);
        shipsAfter.Should().BeEmpty();
    }
}
