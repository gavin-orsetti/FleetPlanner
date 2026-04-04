using FleetPlanner.Models;
using FleetPlanner.Repositories;
using FleetPlanner.Services;
using FleetPlanner.ViewModels;

using FluentAssertions;

using NSubstitute;

namespace FleetPlanner_Tests;

/// <summary>
/// Unit tests for <see cref="ShipBrowserViewModel"/> — tests the real-time filtering logic.
/// <para>
/// <b>Testing reactive filtering:</b> The ShipBrowserViewModel uses <c>[ObservableProperty]</c>
/// partial method hooks (OnSearchTextChanged, OnSelectedRoleChanged, etc.) to re-filter the
/// ship list whenever a filter changes. These tests verify that setting filter properties
/// (SearchText, SelectedRole) immediately produces the correct filtered subset.
/// </para>
/// <para>
/// <b>Mock setup:</b> Both IShipDataService and IFleetRepository are mocked. The ship data
/// service returns a predefined list of ships; the fleet repository is unused in these
/// filter-focused tests but required by the ViewModel's constructor.
/// </para>
/// </summary>
/// <see href="https://learn.microsoft.com/en-us/dotnet/communitytoolkit/mvvm/generators/observableproperty"/>
public class ShipBrowserViewModel_Tests
{
    private readonly IShipDataService _mockShipDataService;
    private readonly IFleetRepository _mockFleetRepo;
    private readonly ShipBrowserViewModel _vm;

    /// <summary>
    /// Test constructor — creates fresh mocks and a new ViewModel for each test.
    /// </summary>
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
