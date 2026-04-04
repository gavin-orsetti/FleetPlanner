using FleetPlanner.Models;
using FleetPlanner.Services;

using FluentAssertions;

namespace FleetPlanner_Tests;

public class RecommendationService_Tests
{
    private readonly RecommendationService _service = new();

    private static Fleet CreateDefaultFleet(
        FleetFocus focus = FleetFocus.Multipurpose,
        FleetOperatingScale scale = FleetOperatingScale.Large,
        int crewCount = 10)
    {
        return new Fleet
        {
            Id = 1,
            Name = "Test Fleet",
            PrimaryFocus = (int)focus,
            OperatingScale = (int)scale,
            AvailableCrewCount = crewCount
        };
    }

    [Fact]
    public void GetRecommendations_EmptyFleet_ReturnsEmptyList()
    {
        var fleet = CreateDefaultFleet();
        var recs = _service.GetRecommendations(fleet, [], []);

        recs.Should().BeEmpty();
    }

    [Fact]
    public void GetRecommendations_MissingCombat_FlagsCombatRole()
    {
        var fleet = CreateDefaultFleet();
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

        var recs = _service.GetRecommendations(fleet, owned, all);

        recs.Should().Contain(r =>
            r.Category == RecommendationCategory.RoleCoverage &&
            r.Title.Contains("Combat", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void GetRecommendations_MissingHauler_FlagsHaulingRole()
    {
        var fleet = CreateDefaultFleet();
        var owned = new List<Ship>
        {
            new() { Id = 1, Name = "Arrow", Role = "Fighter", PriceUsd = 75 }
        };
        var all = new List<Ship>
        {
            new() { Id = 1, Name = "Arrow", Role = "Fighter", PriceUsd = 75 },
            new() { Id = 2, Name = "Hull C", Role = "Hauling", PriceUsd = 200, CargoCapacity = 4608 }
        };

        var recs = _service.GetRecommendations(fleet, owned, all);

        recs.Should().Contain(r =>
            r.Category == RecommendationCategory.RoleCoverage &&
            r.Title.Contains("Hauling", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void GetRecommendations_MissingMedical_FlagsMedicalRole()
    {
        var fleet = CreateDefaultFleet();
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

        var recs = _service.GetRecommendations(fleet, owned, all);

        recs.Should().Contain(r =>
            r.Category == RecommendationCategory.RoleCoverage &&
            r.Title.Contains("Medical", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void GetRecommendations_WellRoundedFleet_NoRoleCoverageGaps()
    {
        var fleet = CreateDefaultFleet();
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

        var recs = _service.GetRecommendations(fleet, owned, owned);

        recs.Where(r => r.Category == RecommendationCategory.RoleCoverage)
            .Should().BeEmpty();
    }

    [Fact]
    public void GetRecommendations_HaulerWithoutEscort_GetsSynergyRecommendation()
    {
        var fleet = CreateDefaultFleet();
        var owned = new List<Ship>
        {
            new() { Id = 1, Name = "Hull C", Role = "Hauling", PriceUsd = 200, CargoCapacity = 4608 }
        };
        var all = new List<Ship>
        {
            new() { Id = 1, Name = "Hull C", Role = "Hauling", PriceUsd = 200, CargoCapacity = 4608 },
            new() { Id = 2, Name = "Arrow", Role = "Fighter", PriceUsd = 75, CrewMin = 1, CrewMax = 1 }
        };

        var recs = _service.GetRecommendations(fleet, owned, all);

        recs.Should().Contain(r =>
            r.Category == RecommendationCategory.FleetSynergy &&
            r.Title.Contains("escort", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void GetRecommendations_CheaperUpgradeInSameRole_FlagsUpgradePath()
    {
        var fleet = CreateDefaultFleet();
        var cheapFighter = new Ship { Id = 1, Name = "Aurora MR", Role = "Fighter", PriceUsd = 30 };
        var betterFighter = new Ship { Id = 2, Name = "Arrow", Role = "Fighter", PriceUsd = 75 };
        var owned = new List<Ship> { cheapFighter };
        var all = new List<Ship> { cheapFighter, betterFighter };

        var recs = _service.GetRecommendations(fleet, owned, all);

        recs.Should().Contain(r =>
            r.Category == RecommendationCategory.UpgradePath &&
            r.Title.Contains("Aurora MR", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void GetRecommendations_OverpricedShip_FlagsValueAnalysis()
    {
        var fleet = CreateDefaultFleet();
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

        var recs = _service.GetRecommendations(fleet, owned, all);

        recs.Should().Contain(r =>
            r.Category == RecommendationCategory.ValueAnalysis &&
            r.Title.Contains("Expensive Hauler", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void GetRecommendations_ResultsOrderedByPriority_HighBeforeLow()
    {
        var fleet = CreateDefaultFleet();
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

        var recs = _service.GetRecommendations(fleet, owned, all);

        recs.Should().NotBeEmpty();
        recs.Should().BeInDescendingOrder(r => r.Priority);
    }

    [Fact]
    public void GetRecommendations_AllRecommendations_HaveNonEmptyTitleAndDescription()
    {
        var fleet = CreateDefaultFleet();
        var owned = new List<Ship>
        {
            new() { Id = 1, Name = "Hull C", Role = "Hauling", PriceUsd = 200, CargoCapacity = 4608 }
        };
        var all = new List<Ship>
        {
            new() { Id = 1, Name = "Hull C", Role = "Hauling", PriceUsd = 200, CargoCapacity = 4608 },
            new() { Id = 2, Name = "Arrow", Role = "Fighter", PriceUsd = 75, CrewMin = 1, CrewMax = 1 },
            new() { Id = 3, Name = "Apollo", Role = "Medical", PriceUsd = 275 },
            new() { Id = 4, Name = "Prospector", Role = "Mining", PriceUsd = 155 }
        };

        var recs = _service.GetRecommendations(fleet, owned, all);

        recs.Should().NotBeEmpty();
        recs.Should().AllSatisfy(r =>
        {
            r.Title.Should().NotBeNullOrWhiteSpace();
            r.Description.Should().NotBeNullOrWhiteSpace();
        });
    }

    // NEW: Crew Efficiency Tests

    [Fact]
    public void GetRecommendations_UnderstaffedFleet_FlagsCrewEfficiency()
    {
        var fleet = CreateDefaultFleet(crewCount: 2);
        var owned = new List<Ship>
        {
            new() { Id = 1, Name = "Hammerhead", Role = "Combat", PriceUsd = 725, CrewMin = 6, CrewMax = 8 },
            new() { Id = 2, Name = "Carrack", Role = "Exploration", PriceUsd = 600, CrewMin = 4, CrewMax = 6 }
        };
        var all = new List<Ship>(owned)
        {
            new() { Id = 3, Name = "Arrow", Role = "Fighter", PriceUsd = 75, CrewMin = 1, CrewMax = 1 }
        };

        var recs = _service.GetRecommendations(fleet, owned, all);

        recs.Should().Contain(r =>
            r.Category == RecommendationCategory.CrewEfficiency &&
            r.Title.Contains("understaffed", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void GetRecommendations_OvercapacityFleet_FlagsCrewEfficiency()
    {
        var fleet = CreateDefaultFleet(crewCount: 20);
        var owned = new List<Ship>
        {
            new() { Id = 1, Name = "Arrow", Role = "Fighter", PriceUsd = 75, CrewMin = 1, CrewMax = 1 },
            new() { Id = 2, Name = "Gladius", Role = "Fighter", PriceUsd = 90, CrewMin = 1, CrewMax = 1 }
        };
        var all = new List<Ship>(owned)
        {
            new() { Id = 3, Name = "Hammerhead", Role = "Combat", PriceUsd = 725, CrewMin = 6, CrewMax = 8 }
        };

        var recs = _service.GetRecommendations(fleet, owned, all);

        recs.Should().Contain(r =>
            r.Category == RecommendationCategory.CrewEfficiency &&
            r.Title.Contains("overcapacity", StringComparison.OrdinalIgnoreCase));
    }

    // NEW: Focus-Aware Role Gap Tests

    [Fact]
    public void GetRecommendations_CombatFleetMissingMedical_MediumPriority()
    {
        var fleet = CreateDefaultFleet(focus: FleetFocus.Combat);
        var owned = new List<Ship>
        {
            new() { Id = 1, Name = "Arrow", Role = "Fighter", PriceUsd = 75, CrewMin = 1, CrewMax = 1 }
        };
        var all = new List<Ship>
        {
            new() { Id = 1, Name = "Arrow", Role = "Fighter", PriceUsd = 75, CrewMin = 1, CrewMax = 1 },
            new() { Id = 2, Name = "Apollo", Role = "Medical", PriceUsd = 275, CrewMin = 1, CrewMax = 2 }
        };

        var recs = _service.GetRecommendations(fleet, owned, all);

        var medicalRec = recs.FirstOrDefault(r =>
            r.Category == RecommendationCategory.RoleCoverage &&
            r.Title.Contains("Medical", StringComparison.OrdinalIgnoreCase));

        medicalRec.Should().NotBeNull();
        medicalRec!.Priority.Should().Be(RecommendationPriority.Medium,
            "medical should be Medium priority for a combat fleet, not High");
    }

    [Fact]
    public void GetRecommendations_CombatFleetMissingHauling_LowPriority()
    {
        var fleet = CreateDefaultFleet(focus: FleetFocus.Combat);
        var owned = new List<Ship>
        {
            new() { Id = 1, Name = "Arrow", Role = "Fighter", PriceUsd = 75, CrewMin = 1, CrewMax = 1 }
        };
        var all = new List<Ship>
        {
            new() { Id = 1, Name = "Arrow", Role = "Fighter", PriceUsd = 75, CrewMin = 1, CrewMax = 1 },
            new() { Id = 2, Name = "Hull C", Role = "Hauling", PriceUsd = 200, CrewMin = 1, CrewMax = 3 }
        };

        var recs = _service.GetRecommendations(fleet, owned, all);

        var haulingRec = recs.FirstOrDefault(r =>
            r.Category == RecommendationCategory.RoleCoverage &&
            r.Title.Contains("Hauling", StringComparison.OrdinalIgnoreCase));

        haulingRec.Should().NotBeNull();
        haulingRec!.Priority.Should().Be(RecommendationPriority.Low,
            "hauling should be Low priority for a combat fleet");
    }

    // NEW: Scale-Appropriate Suggestion Tests

    [Fact]
    public void GetRecommendations_SoloFleet_NeverSuggestsLargeCrewShips()
    {
        var fleet = CreateDefaultFleet(
            focus: FleetFocus.Multipurpose,
            scale: FleetOperatingScale.Solo,
            crewCount: 1);
        var owned = new List<Ship>
        {
            new() { Id = 1, Name = "Aurora MR", Role = "Fighter", PriceUsd = 30, CrewMin = 1, CrewMax = 1 }
        };
        var all = new List<Ship>
        {
            new() { Id = 1, Name = "Aurora MR", Role = "Fighter", PriceUsd = 30, CrewMin = 1, CrewMax = 1 },
            new() { Id = 2, Name = "Hull C", Role = "Hauling", PriceUsd = 200, CrewMin = 1, CrewMax = 3 },
            new() { Id = 3, Name = "Hammerhead", Role = "Combat", PriceUsd = 725, CrewMin = 6, CrewMax = 8 },
            new() { Id = 4, Name = "Prospector", Role = "Mining", PriceUsd = 155, CrewMin = 1, CrewMax = 1 },
            new() { Id = 5, Name = "Apollo", Role = "Medical", PriceUsd = 275, CrewMin = 1, CrewMax = 2 }
        };

        var recs = _service.GetRecommendations(fleet, owned, all);

        var suggestedShips = recs.SelectMany(r => r.SuggestedShips).ToList();
        suggestedShips.Should().NotContain(s => s.Name == "Hammerhead",
            "a solo fleet should never be suggested a 6+ crew ship");
    }

    // NEW: Multi-Fleet Isolation Tests

    [Fact]
    public void GetRecommendations_RecommendationsTaggedWithFleetId()
    {
        var fleet = CreateDefaultFleet();
        fleet.Id = 42;
        var owned = new List<Ship>
        {
            new() { Id = 1, Name = "Arrow", Role = "Fighter", PriceUsd = 75, CrewMin = 1, CrewMax = 1 }
        };
        var all = new List<Ship>
        {
            new() { Id = 1, Name = "Arrow", Role = "Fighter", PriceUsd = 75, CrewMin = 1, CrewMax = 1 },
            new() { Id = 2, Name = "Hull C", Role = "Hauling", PriceUsd = 200, CrewMin = 1, CrewMax = 3 }
        };

        var recs = _service.GetRecommendations(fleet, owned, all);

        recs.Should().NotBeEmpty();
        recs.Should().AllSatisfy(r => r.FleetId.Should().Be(42));
    }

    [Fact]
    public void GetRecommendations_TwoFleets_ProduceDifferentRecommendations()
    {
        var combatFleet = CreateDefaultFleet(focus: FleetFocus.Combat);
        combatFleet.Id = 1;
        var tradingFleet = CreateDefaultFleet(focus: FleetFocus.Trading);
        tradingFleet.Id = 2;

        var ownedShips = new List<Ship>
        {
            new() { Id = 1, Name = "Arrow", Role = "Fighter", PriceUsd = 75, CrewMin = 1, CrewMax = 1 }
        };
        var allShips = new List<Ship>
        {
            new() { Id = 1, Name = "Arrow", Role = "Fighter", PriceUsd = 75, CrewMin = 1, CrewMax = 1 },
            new() { Id = 2, Name = "Hull C", Role = "Hauling", PriceUsd = 200, CrewMin = 1, CrewMax = 3 },
            new() { Id = 3, Name = "Apollo", Role = "Medical", PriceUsd = 275, CrewMin = 1, CrewMax = 2 }
        };

        var combatRecs = _service.GetRecommendations(combatFleet, ownedShips, allShips);
        var tradingRecs = _service.GetRecommendations(tradingFleet, ownedShips, allShips);

        combatRecs.Should().AllSatisfy(r => r.FleetId.Should().Be(1));
        tradingRecs.Should().AllSatisfy(r => r.FleetId.Should().Be(2));

        // Trading fleet should have High priority for Hauling gap
        var tradingHauling = tradingRecs.FirstOrDefault(r =>
            r.Category == RecommendationCategory.RoleCoverage &&
            r.Title.Contains("Hauling", StringComparison.OrdinalIgnoreCase));
        tradingHauling.Should().NotBeNull();
        tradingHauling!.Priority.Should().Be(RecommendationPriority.High);

        // Combat fleet should have Low priority for Hauling gap
        var combatHauling = combatRecs.FirstOrDefault(r =>
            r.Category == RecommendationCategory.RoleCoverage &&
            r.Title.Contains("Hauling", StringComparison.OrdinalIgnoreCase));
        combatHauling.Should().NotBeNull();
        combatHauling!.Priority.Should().Be(RecommendationPriority.Low);
    }
}
