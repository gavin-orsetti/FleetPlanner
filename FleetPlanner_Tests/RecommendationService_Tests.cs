using FleetPlanner.Graph;
using FleetPlanner.Models;
using FleetPlanner.Services;

using FluentAssertions;

namespace FleetPlanner_Tests;

/// <summary>
/// Tests for <see cref="RecommendationService"/> — the graph-driven recommendation engine.
/// Each test builds a <see cref="FleetGraph"/> in-memory and verifies the expected
/// recommendation patterns are detected.
/// </summary>
public class RecommendationService_Tests
{
    private readonly RecommendationService _service = new();

    // ── Helpers ────────────────────────────────────────────────────────

    private static TagDefinition MakeTagDef(string key, string category) => new()
    {
        Key = key,
        DisplayName = key.Split(':').Last(),
        Category = category,
        AllowedScopes = "OwnedShip,UserFleetGroup"
    };

    private static TagNode MakeTagNode(string key, string category, int weight = 1) => new()
    {
        Definition = MakeTagDef(key, category),
        Weight = weight
    };

    private static ShipNode MakeShipNode(int ownedId, string name, int crewMin = 1, int daysOld = 30) => new()
    {
        OwnedShipId = ownedId,
        OwnedShip = new OwnedShip
        {
            Id = ownedId,
            ShipId = ownedId,
            CreatedUtc = DateTime.UtcNow.AddDays(-daysOld)
        },
        CatalogueShip = new Ship
        {
            Id = ownedId,
            Name = name,
            CrewMin = crewMin
        }
    };

    private static GroupNode MakeGroupNode(int id, string name, int crewTarget = 4) => new()
    {
        Group = new UserFleetGroup
        {
            Id = id,
            Name = name,
            CrewTarget = crewTarget
        }
    };

    // ── Pattern 1: CapabilityGap ──────────────────────────────────────

    [Fact]
    public void CapabilityGap_EarnerGroup_NoSalvageShips_Detected()
    {
        var group = MakeGroupNode(1, "Industrial Ops");
        group.DoctrineAndFocusTags.Add(MakeTagNode("doctrine:purpose:earner", "doctrine:purpose"));

        // Ship with mining but no salvage/cargo
        var miner = MakeShipNode(1, "MISC Prospector");
        miner.GlobalTags.Add(MakeTagNode("role:mining", "role"));
        miner.ContextualTags[1] = new List<TagNode> { MakeTagNode("role:mining", "role") };
        group.MemberShips.Add(miner);

        var graph = new FleetGraph { Ships = [miner], Groups = [group] };
        var recs = _service.GetRecommendations(graph);

        recs.Should().Contain(r =>
            r.Kind == RecommendationKind.CapabilityGap
            && r.ScopeId == 1);
    }

    // ── Pattern 2: Redundancy ─────────────────────────────────────────

    [Fact]
    public void Redundancy_TwoShipsSamePrimaryRole_Detected()
    {
        var group = MakeGroupNode(1, "Combat Wing");

        var ship1 = MakeShipNode(1, "Arrow");
        ship1.GlobalTags.Add(MakeTagNode("role:escort", "role", 1));
        ship1.ContextualTags[1] = new List<TagNode>();

        var ship2 = MakeShipNode(2, "Gladius");
        ship2.GlobalTags.Add(MakeTagNode("role:escort", "role", 1));
        ship2.ContextualTags[1] = new List<TagNode>();

        group.MemberShips.AddRange([ship1, ship2]);

        var graph = new FleetGraph { Ships = [ship1, ship2], Groups = [group] };
        var recs = _service.GetRecommendations(graph);

        recs.Should().Contain(r =>
            r.Kind == RecommendationKind.Redundancy
            && r.TargetTagKey == "role:escort");
    }

    // ── Pattern 4: UnderDescribedShip ─────────────────────────────────

    [Fact]
    public void UnderDescribedShip_ZeroTags_Detected()
    {
        var ship = MakeShipNode(1, "Mystery Ship");
        // No tags at all

        var graph = new FleetGraph { Ships = [ship], Groups = [] };
        var recs = _service.GetRecommendations(graph);

        recs.Should().Contain(r =>
            r.Kind == RecommendationKind.UnderDescribedShip
            && r.ScopeId == 1);
    }

    [Fact]
    public void UnderDescribedShip_TwoMeaningfulTags_NotDetected()
    {
        var ship = MakeShipNode(1, "Well-Tagged Ship");
        ship.GlobalTags.Add(MakeTagNode("role:escort", "role"));
        ship.GlobalTags.Add(MakeTagNode("ctx:solo", "ctx"));

        var graph = new FleetGraph { Ships = [ship], Groups = [] };
        var recs = _service.GetRecommendations(graph);

        recs.Should().NotContain(r => r.Kind == RecommendationKind.UnderDescribedShip);
    }

    // ── Pattern 5: UnassignedShip ─────────────────────────────────────

    [Fact]
    public void UnassignedShip_NotInAnyGroup_After7Days_Detected()
    {
        var ship = MakeShipNode(1, "Lonely Ship", daysOld: 14);

        var graph = new FleetGraph { Ships = [ship], Groups = [] };
        var recs = _service.GetRecommendations(graph);

        recs.Should().Contain(r =>
            r.Kind == RecommendationKind.UnassignedShip
            && r.ScopeId == 1);
    }

    [Fact]
    public void UnassignedShip_NewShip_UnderSevenDays_NotDetected()
    {
        var ship = MakeShipNode(1, "Fresh Ship", daysOld: 3);

        var graph = new FleetGraph { Ships = [ship], Groups = [] };
        var recs = _service.GetRecommendations(graph);

        recs.Should().NotContain(r => r.Kind == RecommendationKind.UnassignedShip);
    }

    // ── Pattern 8: DoctrineMismatch ───────────────────────────────────

    [Fact]
    public void DoctrineMismatch_ShipProtector_GroupEarner_Detected()
    {
        var group = MakeGroupNode(1, "Mining Group");
        group.DoctrineAndFocusTags.Add(MakeTagNode("doctrine:purpose:earner", "doctrine:purpose"));

        var ship = MakeShipNode(1, "F7C Hornet");
        ship.GlobalTags.Add(MakeTagNode("doctrine:purpose:protector", "doctrine:purpose", 1));
        ship.ContextualTags[1] = new List<TagNode>();
        group.MemberShips.Add(ship);

        var graph = new FleetGraph { Ships = [ship], Groups = [group] };
        var recs = _service.GetRecommendations(graph);

        recs.Should().Contain(r =>
            r.Kind == RecommendationKind.DoctrineMismatch
            && r.ScopeId == 1);
    }

    // ── Pattern 10: CrewEfficiency ────────────────────────────────────

    [Fact]
    public void CrewEfficiency_ExceedsTarget_Detected()
    {
        var group = MakeGroupNode(1, "Small Crew Ops", crewTarget: 2);

        // 3 ships each needing 2 crew = 6 total, target is 2 → ratio 3.0
        var s1 = MakeShipNode(1, "Ship A", crewMin: 2);
        s1.ContextualTags[1] = new List<TagNode>();
        var s2 = MakeShipNode(2, "Ship B", crewMin: 2);
        s2.ContextualTags[1] = new List<TagNode>();
        var s3 = MakeShipNode(3, "Ship C", crewMin: 2);
        s3.ContextualTags[1] = new List<TagNode>();

        group.MemberShips.AddRange([s1, s2, s3]);

        var graph = new FleetGraph { Ships = [s1, s2, s3], Groups = [group] };
        var recs = _service.GetRecommendations(graph);

        recs.Should().Contain(r =>
            r.Kind == RecommendationKind.CrewEfficiency
            && r.ScopeId == 1);
    }

    // ── Sorting ───────────────────────────────────────────────────────

    [Fact]
    public void Results_SortedByScore_Descending()
    {
        // Create a graph that triggers multiple recommendation kinds
        var ship = MakeShipNode(1, "Bare Ship", daysOld: 14);

        var graph = new FleetGraph { Ships = [ship], Groups = [] };
        var recs = _service.GetRecommendations(graph);

        // Should have at least UnderDescribedShip and UnassignedShip
        recs.Should().HaveCountGreaterOrEqualTo(2);
        recs.Should().BeInDescendingOrder(r => r.Score);
    }
}
