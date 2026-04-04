using FleetPlanner.Models;
using FleetPlanner.Repositories;

using FluentAssertions;

namespace FleetPlanner_Tests;

/// <summary>
/// Integration tests for <see cref="FleetRepository"/> — tests the SQLite repository against
/// a real (temporary) SQLite database.
/// <para>
/// <b>Why integration tests, not mocks?</b> The repository IS the data access layer — mocking
/// it would just test a mock. Instead, these tests use a real SQLite database to verify that
/// SQL operations (insert, update, delete, cascade) work correctly. Each test gets its own
/// temp database via the <see cref="IDisposable"/> pattern.
/// </para>
/// <para>
/// <b>Test naming convention:</b> <c>MethodName_Scenario_ExpectedBehavior</c>.
/// Example: <c>SaveFleetAsync_NewFleet_AssignsIdAndPersists</c>.
/// </para>
/// </summary>
/// <see href="https://xunit.net/docs/shared-context#constructor"/>
/// <see href="https://fluentassertions.com/introduction"/>
public class FleetRepository_Tests : IDisposable
{
    /// <summary>The system under test — a real FleetRepository backed by a temp SQLite DB.</summary>
    private readonly FleetRepository _repo;

    /// <summary>Path to the temporary SQLite database for this test run.</summary>
    private readonly string _dbPath;

    /// <summary>
    /// Test constructor — creates a fresh temp database for each test.
    /// Uses the <c>FleetRepository(string dbPath)</c> constructor overload designed for testing.
    /// </summary>
    public FleetRepository_Tests()
    {
        SQLitePCL.Batteries_V2.Init();
        _dbPath = Path.Combine(Path.GetTempPath(), $"FleetPlannerTest_{Guid.NewGuid()}.db3");
        _repo = new FleetRepository(_dbPath);
    }

    /// <summary>Cleanup — delete the temporary SQLite database file.</summary>
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
