using FleetPlanner.Models;
using FleetPlanner.Repositories;
using FleetPlanner.Services;
using FleetPlanner.ViewModels;

using FluentAssertions;

using NSubstitute;

namespace FleetPlanner_Tests;

/// <summary>
/// Unit tests for <see cref="RecommendationsViewModel"/> — tests the ViewModel layer in isolation.
/// <para>
/// <b>All three dependencies are mocked:</b> The ViewModel depends on IFleetRepository,
/// IShipDataService, and IRecommendationService. All three are NSubstitute mocks, so these
/// tests verify the ViewModel's logic (loading flow, state management, data transformation)
/// without hitting real databases or APIs.
/// </para>
/// <para>
/// <b>Testing CommunityToolkit.Mvvm commands:</b> <c>[RelayCommand]</c> generates an
/// <c>IAsyncRelayCommand</c> property (e.g., <c>LoadRecommendationsCommand</c>). Tests
/// invoke it via <c>ExecuteAsync(null)</c> and then inspect the ViewModel's observable properties.
/// </para>
/// </summary>
/// <see href="https://learn.microsoft.com/en-us/dotnet/communitytoolkit/mvvm/generators/relaycommand"/>
/// <see href="https://nsubstitute.github.io/help/creating-a-substitute/"/>
public class RecommendationsViewModel_Tests
{
    private readonly IFleetRepository _mockFleetRepo;
    private readonly IShipDataService _mockShipDataService;
    private readonly IRecommendationService _mockRecommendationService;
    private readonly RecommendationsViewModel _vm;

    /// <summary>
    /// Test constructor — creates fresh mocks and a new ViewModel for each test.
    /// </summary>
    public RecommendationsViewModel_Tests()
    {
        _mockFleetRepo = Substitute.For<IFleetRepository>();
        _mockShipDataService = Substitute.For<IShipDataService>();
        _mockRecommendationService = Substitute.For<IRecommendationService>();
        _vm = new RecommendationsViewModel(_mockFleetRepo, _mockShipDataService, _mockRecommendationService);
    }

    [Fact]
    public async Task LoadRecommendations_ResultsSortedByPriority()
    {
        var fleet = new Fleet { Id = 1, Name = "TestFleet" };
        var fleetShip = new FleetShip { Id = 1, FleetId = 1, ShipId = 10 };
        var ship = new Ship { Id = 10, Name = "Arrow", Role = "Fighter", PriceUsd = 75 };

        _mockFleetRepo.GetAllFleetsAsync().Returns(new List<Fleet> { fleet });
        _mockFleetRepo.GetFleetShipsAsync(1).Returns(new List<FleetShip> { fleetShip });
        _mockShipDataService.GetAllShipsAsync(Arg.Any<bool>()).Returns(new List<Ship> { ship });

        var recs = new List<Recommendation>
        {
            new()
            {
                Title = "Low priority",
                Description = "desc",
                Category = RecommendationCategory.UpgradePath,
                Priority = RecommendationPriority.Low
            },
            new()
            {
                Title = "High priority",
                Description = "desc",
                Category = RecommendationCategory.RoleCoverage,
                Priority = RecommendationPriority.High
            },
            new()
            {
                Title = "Medium priority",
                Description = "desc",
                Category = RecommendationCategory.FleetSynergy,
                Priority = RecommendationPriority.Medium
            }
        };

        _mockRecommendationService
            .GetRecommendations(Arg.Any<Fleet>(), Arg.Any<List<Ship>>(), Arg.Any<List<Ship>>())
            .Returns(recs);

        // Set the selected fleet directly to avoid double-load
        _vm.SelectedFleet = fleet;

        await _vm.LoadRecommendationsCommand.ExecuteAsync(null);

        _vm.Recommendations.Should().HaveCount(3);
        // The ViewModel sets recommendations in the order returned by the service
        _vm.Recommendations.Select(r => r.Priority)
            .Should().ContainInOrder(
                RecommendationPriority.Low,
                RecommendationPriority.High,
                RecommendationPriority.Medium);
    }

    [Fact]
    public async Task LoadRecommendations_NoFleets_SetsHasNoFleet()
    {
        _mockFleetRepo.GetAllFleetsAsync().Returns(new List<Fleet>());

        await _vm.LoadRecommendationsCommand.ExecuteAsync(null);

        _vm.HasNoFleet.Should().BeTrue();
        _vm.IsEmpty.Should().BeTrue();
        _vm.Recommendations.Should().BeEmpty();
    }
}
