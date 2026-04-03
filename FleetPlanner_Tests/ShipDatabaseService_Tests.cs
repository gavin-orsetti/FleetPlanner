using FleetPlanner.Models;
using FleetPlanner.Services;

namespace FleetPlanner_Tests;

public class RecommendationService_Tests
{
    private readonly RecommendationService _service = new();

    [Fact]
    public void GetRecommendations_ReturnsRoleCoverage_WhenMissingCombat()
    {
        // Arrange — fleet with only a hauler, no combat ship
        var owned = new List<Ship>
        {
            new() { Id = 1, Name = "Hull C", Role = "Hauling", PriceUsd = 200, CargoCapacity = 4608 }
        };
        var all = new List<Ship>
        {
            new() { Id = 1, Name = "Hull C", Role = "Hauling", PriceUsd = 200, CargoCapacity = 4608 },
            new() { Id = 2, Name = "Arrow", Role = "Fighter", PriceUsd = 75, CrewMin = 1, CrewMax = 1 },
            new() { Id = 3, Name = "Cutlass Black", Role = "Combat", PriceUsd = 100, CrewMin = 1, CrewMax = 3 }
        };

        // Act
        var recs = _service.GetRecommendations(owned, all);

        // Assert
        Assert.Contains(recs, r => r.Category == RecommendationCategory.RoleCoverage
                                   && r.Title.Contains("Combat", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void GetRecommendations_ReturnsSynergy_WhenHaulerButNoEscort()
    {
        var owned = new List<Ship>
        {
            new() { Id = 1, Name = "Hull C", Role = "Hauling", PriceUsd = 200, CargoCapacity = 4608 }
        };
        var all = new List<Ship>
        {
            new() { Id = 1, Name = "Hull C", Role = "Hauling", PriceUsd = 200, CargoCapacity = 4608 },
            new() { Id = 2, Name = "Arrow", Role = "Fighter", PriceUsd = 75 }
        };

        var recs = _service.GetRecommendations(owned, all);

        Assert.Contains(recs, r => r.Category == RecommendationCategory.FleetSynergy
                                   && r.Title.Contains("escort", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void GetRecommendations_ReturnsEmpty_WhenNoShipsOwned()
    {
        var recs = _service.GetRecommendations([], []);
        Assert.Empty(recs);
    }
}
