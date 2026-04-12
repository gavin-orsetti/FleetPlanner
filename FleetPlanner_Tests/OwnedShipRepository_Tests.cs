using FleetPlanner.Models;
using FleetPlanner.Repositories;

using FluentAssertions;

namespace FleetPlanner_Tests;

/// <summary>
/// Integration tests for <see cref="OwnedShipRepository"/> against a real SQLite database.
/// Each test gets its own temp DB file via <see cref="IDisposable"/>.
/// </summary>
public class OwnedShipRepository_Tests : IDisposable
{
    private readonly string _dbPath;
    private readonly OwnedShipRepository _repo;

    public OwnedShipRepository_Tests()
    {
        SQLitePCL.Batteries_V2.Init();
        _dbPath = Path.Combine(Path.GetTempPath(), $"OwnedShipRepo_{Guid.NewGuid()}.db3");
        _repo = new OwnedShipRepository(_dbPath);
    }

    public void Dispose()
    {
        try { File.Delete(_dbPath); } catch { }
    }

    [Fact]
    public async Task SaveAndGet_RoundTrip()
    {
        var ship = new OwnedShip { ShipId = 42, Callsign = "Phoenix", AcquisitionType = AcquisitionType.RealMoney };
        await _repo.SaveOwnedShipAsync(ship);

        ship.Id.Should().BeGreaterThan(0);

        var loaded = await _repo.GetOwnedShipAsync(ship.Id);
        loaded.Should().NotBeNull();
        loaded!.ShipId.Should().Be(42);
        loaded.Callsign.Should().Be("Phoenix");
        loaded.AcquisitionType.Should().Be(AcquisitionType.RealMoney);
    }

    [Fact]
    public async Task GetAll_ExcludesArchived_ByDefault()
    {
        await _repo.SaveOwnedShipAsync(new OwnedShip { ShipId = 1, Callsign = "Active" });
        await _repo.SaveOwnedShipAsync(new OwnedShip { ShipId = 2, Callsign = "Hidden", IsArchived = true });

        var active = await _repo.GetAllOwnedShipsAsync();
        active.Should().HaveCount(1);
        active[0].Callsign.Should().Be("Active");

        var all = await _repo.GetAllOwnedShipsAsync(includeArchived: true);
        all.Should().HaveCount(2);
    }

    [Fact]
    public async Task Archive_SetsFlag()
    {
        var ship = new OwnedShip { ShipId = 1, Callsign = "Soon Gone" };
        await _repo.SaveOwnedShipAsync(ship);

        await _repo.ArchiveOwnedShipAsync(ship.Id);

        var loaded = await _repo.GetOwnedShipAsync(ship.Id);
        loaded!.IsArchived.Should().BeTrue();
    }

    [Fact]
    public async Task Delete_RemovesRecord()
    {
        var ship = new OwnedShip { ShipId = 1, Callsign = "Doomed" };
        await _repo.SaveOwnedShipAsync(ship);

        await _repo.DeleteOwnedShipAsync(ship.Id);

        var loaded = await _repo.GetOwnedShipAsync(ship.Id);
        loaded.Should().BeNull();
    }

    [Fact]
    public async Task Update_ExistingShip_PreservesId()
    {
        var ship = new OwnedShip { ShipId = 1, Callsign = "Original" };
        await _repo.SaveOwnedShipAsync(ship);
        var originalId = ship.Id;

        ship.Callsign = "Updated";
        await _repo.SaveOwnedShipAsync(ship);

        var loaded = await _repo.GetOwnedShipAsync(originalId);
        loaded!.Callsign.Should().Be("Updated");
    }
}
