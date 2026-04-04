using FleetPlanner.Models;
using FleetPlanner.Repositories;
using FleetPlanner.Services;
using FleetPlanner.ViewModels;

using FluentAssertions;

using NSubstitute;

namespace FleetPlanner_Tests;

public class ShipBrowserViewModel_Tests
{
    private readonly IShipDataService _mockShipDataService;
    private readonly IFleetRepository _mockFleetRepo;
    private readonly ShipBrowserViewModel _vm;

    public ShipBrowserViewModel_Tests()
    {
        _mockShipDataService = Substitute.For<IShipDataService>();
        _mockFleetRepo = Substitute.For<IFleetRepository>();
        _vm = new ShipBrowserViewModel(_mockShipDataService, _mockFleetRepo);
    }

    [Fact]
    public async Task FilterByRole_ReturnsCorrectSubset()
    {
        var ships = new List<Ship>
        {
            new() { Id = 1, Name = "Arrow", Role = "Fighter", Manufacturer = "Anvil" },
            new() { Id = 2, Name = "Hull C", Role = "Hauling", Manufacturer = "MISC" },
            new() { Id = 3, Name = "Gladius", Role = "Fighter", Manufacturer = "Aegis" }
        };
        _mockShipDataService.GetAllShipsAsync(Arg.Any<bool>()).Returns(ships);
        _mockShipDataService.GetLastUpdatedAsync().Returns(DateTime.UtcNow);

        await _vm.LoadShipsCommand.ExecuteAsync(null);

        _vm.SelectedRole = "Fighter";

        _vm.Ships.Should().HaveCount(2);
        _vm.Ships.Should().AllSatisfy(s => s.Role.Should().Be("Fighter"));
    }

    [Fact]
    public async Task SearchByName_FiltersCorrectly()
    {
        var ships = new List<Ship>
        {
            new() { Id = 1, Name = "Arrow", Role = "Fighter", Manufacturer = "Anvil" },
            new() { Id = 2, Name = "Hull C", Role = "Hauling", Manufacturer = "MISC" },
            new() { Id = 3, Name = "Gladius", Role = "Fighter", Manufacturer = "Aegis" }
        };
        _mockShipDataService.GetAllShipsAsync(Arg.Any<bool>()).Returns(ships);
        _mockShipDataService.GetLastUpdatedAsync().Returns(DateTime.UtcNow);

        await _vm.LoadShipsCommand.ExecuteAsync(null);

        _vm.SearchText = "arrow";

        _vm.Ships.Should().HaveCount(1);
        _vm.Ships.First().Name.Should().Be("Arrow");
    }
}
