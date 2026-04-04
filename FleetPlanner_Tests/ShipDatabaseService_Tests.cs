using FleetPlanner.Models;
using FleetPlanner.Services;

using FluentAssertions;

namespace FleetPlanner_Tests;

public class RecommendationService_Tests
{
    private readonly RecommendationService _service = new();

    [Fact]
    public void GetRecommendations_EmptyFleet_ReturnsEmptyList()
    {
        var recs = _service.GetRecommendations([], []);

        recs.Should().BeEmpty();
    }

    [Fact]
    public void GetRecommendations_MissingCombat_FlagsCombatRole()
    {
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

        var recs = _service.GetRecommendations(owned, all);

        recs.Should().Contain(r =>
            r.Category == RecommendationCategory.RoleCoverage &&
            r.Title.Contains("Combat", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void GetRecommendations_MissingHauler_FlagsHaulingRole()
    {
        var owned = new List<Ship>
        {
            new() { Id = 1, Name = "Arrow", Role = "Fighter", PriceUsd = 75 }
        };
        var all = new List<Ship>
        {
            new() { Id = 1, Name = "Arrow", Role = "Fighter", PriceUsd = 75 },
            new() { Id = 2, Name = "Hull C", Role = "Hauling", PriceUsd = 200, CargoCapacity = 4608 }
        };

        var recs = _service.GetRecommendations(owned, all);

        recs.Should().Contain(r =>
            r.Category == RecommendationCategory.RoleCoverage &&
            r.Title.Contains("Hauling", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void GetRecommendations_MissingMedical_FlagsMedicalRole()
    {
        var owned = new List<Ship>
        {
            new() { Id = 1, Name = "Arrow", Role = "Fighter", PriceUsd = 75 },
            new() { Id = 2, Name = "Hull C", Role = "Hauling", PriceUsd = 200 }
        };
        var all = new List<Ship>
        {
            new() { Id = 1, Name = "Arrow", Role = "Fighter", PriceUsd = 75 },
            new() { Id = 2, Name = "Hull C", Role = "Hauling", PriceUsd = 200 },
            new() { Id = 3, Name = "Apollo", Role = "Medical", PriceUsd = 275 }
        };

        var recs = _service.GetRecommendations(owned, all);

        recs.Should().Contain(r =>
            r.Category == RecommendationCategory.RoleCoverage &&
            r.Title.Contains("Medical", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void GetRecommendations_WellRoundedFleet_NoRoleCoverageGaps()
    {
        var owned = new List<Ship>
        {
            new() { Id = 1, Name = "Arrow", Role = "Fighter", PriceUsd = 75 },
            new() { Id = 2, Name = "Hull C", Role = "Hauling", PriceUsd = 200 },
            new() { Id = 3, Name = "Prospector", Role = "Mining", PriceUsd = 155 },
            new() { Id = 4, Name = "Apollo", Role = "Medical", PriceUsd = 275 },
            new() { Id = 5, Name = "Carrack", Role = "Exploration", PriceUsd = 600 },
            new() { Id = 6, Name = "Vulture", Role = "Salvage", PriceUsd = 140 },
            new() { Id = 7, Name = "Starfarer", Role = "Refueling", PriceUsd = 300 },
            new() { Id = 8, Name = "Herald", Role = "Data", PriceUsd = 85 }
        };

        var recs = _service.GetRecommendations(owned, owned);

        recs.Where(r => r.Category == RecommendationCategory.RoleCoverage)
            .Should().BeEmpty();
    }

    [Fact]
    public void GetRecommendations_HaulerWithoutEscort_GetsSynergyRecommendation()
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

        recs.Should().Contain(r =>
            r.Category == RecommendationCategory.FleetSynergy &&
            r.Title.Contains("escort", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void GetRecommendations_CheaperUpgradeInSameRole_FlagsUpgradePath()
    {
        var cheapFighter = new Ship { Id = 1, Name = "Aurora MR", Role = "Fighter", PriceUsd = 30 };
        var betterFighter = new Ship { Id = 2, Name = "Arrow", Role = "Fighter", PriceUsd = 75 };
        var owned = new List<Ship> { cheapFighter };
        var all = new List<Ship> { cheapFighter, betterFighter };

        var recs = _service.GetRecommendations(owned, all);

        recs.Should().Contain(r =>
            r.Category == RecommendationCategory.UpgradePath &&
            r.Title.Contains("Aurora MR", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void GetRecommendations_OverpricedShip_FlagsValueAnalysis()
    {
        var expensive = new Ship
        {
            Id = 1, Name = "Expensive Hauler", Role = "Transport",
            PriceUsd = 500, CrewMax = 2, CargoCapacity = 100
        };
        var cheap = new Ship
        {
            Id = 2, Name = "Budget Hauler", Role = "Transport",
            PriceUsd = 100, CrewMax = 3, CargoCapacity = 100
        };
        var owned = new List<Ship> { expensive };
        var all = new List<Ship> { expensive, cheap };

        var recs = _service.GetRecommendations(owned, all);

        recs.Should().Contain(r =>
            r.Category == RecommendationCategory.ValueAnalysis &&
            r.Title.Contains("Expensive Hauler", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void GetRecommendations_ResultsOrderedByPriority_HighBeforeLow()
    {
        var owned = new List<Ship>
        {
            new() { Id = 1, Name = "Hull C", Role = "Hauling", PriceUsd = 200, CargoCapacity = 4608 },
            new() { Id = 2, Name = "Aurora MR", Role = "Fighter", PriceUsd = 30 }
        };
        var all = new List<Ship>
        {
            new() { Id = 1, Name = "Hull C", Role = "Hauling", PriceUsd = 200, CargoCapacity = 4608 },
            new() { Id = 2, Name = "Aurora MR", Role = "Fighter", PriceUsd = 30 },
            new() { Id = 3, Name = "Arrow", Role = "Fighter", PriceUsd = 75 },
            new() { Id = 4, Name = "Prospector", Role = "Mining", PriceUsd = 155 },
            new() { Id = 5, Name = "Apollo", Role = "Medical", PriceUsd = 275 }
        };

        var recs = _service.GetRecommendations(owned, all);

        recs.Should().NotBeEmpty();
        recs.Should().BeInDescendingOrder(r => r.Priority);
    }

    [Fact]
    public void GetRecommendations_AllRecommendations_HaveNonEmptyTitleAndDescription()
    {
        var owned = new List<Ship>
        {
            new() { Id = 1, Name = "Hull C", Role = "Hauling", PriceUsd = 200, CargoCapacity = 4608 }
        };
        var all = new List<Ship>
        {
            new() { Id = 1, Name = "Hull C", Role = "Hauling", PriceUsd = 200, CargoCapacity = 4608 },
            new() { Id = 2, Name = "Arrow", Role = "Fighter", PriceUsd = 75 },
            new() { Id = 3, Name = "Apollo", Role = "Medical", PriceUsd = 275 },
            new() { Id = 4, Name = "Prospector", Role = "Mining", PriceUsd = 155 }
        };

        var recs = _service.GetRecommendations(owned, all);

        recs.Should().NotBeEmpty();
        recs.Should().AllSatisfy(r =>
        {
            r.Title.Should().NotBeNullOrWhiteSpace();
            r.Description.Should().NotBeNullOrWhiteSpace();
        });
    }
}
