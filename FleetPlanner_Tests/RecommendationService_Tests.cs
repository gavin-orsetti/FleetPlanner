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
    public void CapabilityGap_PrimaryArmGroup_NoFightShips_Detected()
    {
        var group = MakeGroupNode(1, "Combat Ops");
        group.DoctrineAndFocusTags.Add(MakeTagNode("doctrine:primary-arm", "doctrine"));

        // Ship with mining but no fight/patrol/combat-loop
        var miner = MakeShipNode(1, "MISC Prospector");
        miner.GlobalTags.Add(MakeTagNode("intent:activity:mine", "intent:activity"));
        miner.ContextualTags[1] = new List<TagNode> { MakeTagNode("intent:activity:mine", "intent:activity") };
        group.MemberShips.Add(miner);

        var graph = new FleetGraph { Ships = [miner], Groups = [group] };
        var recs = _service.GetRecommendations(graph);

        recs.Should().Contain(r =>
            r.Kind == RecommendationKind.CapabilityGap
            && r.ScopeId == 1);
    }

    // ── Pattern 2: Redundancy ─────────────────────────────────────────

    [Fact]
    public void Redundancy_TwoShipsSamePrimaryIntent_Detected()
    {
        var group = MakeGroupNode(1, "Combat Wing");

        var ship1 = MakeShipNode(1, "Arrow");
        ship1.GlobalTags.Add(MakeTagNode("intent:activity:escort", "intent:activity", 1));
        ship1.ContextualTags[1] = new List<TagNode>();

        var ship2 = MakeShipNode(2, "Gladius");
        ship2.GlobalTags.Add(MakeTagNode("intent:activity:escort", "intent:activity", 1));
        ship2.ContextualTags[1] = new List<TagNode>();

        group.MemberShips.AddRange([ship1, ship2]);

        var graph = new FleetGraph { Ships = [ship1, ship2], Groups = [group] };
        var recs = _service.GetRecommendations(graph);

        recs.Should().Contain(r =>
            r.Kind == RecommendationKind.Redundancy
            && r.TargetTagKey == "intent:activity:escort");
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
        ship.GlobalTags.Add(MakeTagNode("doctrine:value:backbone", "doctrine:value"));
        ship.GlobalTags.Add(MakeTagNode("status:lifecycle:owned", "status:lifecycle"));

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
    public void DoctrineMismatch_ShipReserve_GroupPrimaryArm_Detected()
    {
        var group = MakeGroupNode(1, "Combat Group");
        group.DoctrineAndFocusTags.Add(MakeTagNode("doctrine:primary-arm", "doctrine"));

        var ship = MakeShipNode(1, "F7C Hornet");
        ship.GlobalTags.Add(MakeTagNode("doctrine:reserve-force", "doctrine", 1));
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

    // ── Pattern 3: Complement ────────────────────────────────────────

    [Fact]
    public void Complement_UnassignedShipFillsGroupGap_Detected()
    {
        var group = MakeGroupNode(1, "Combat Wing");

        var member = MakeShipNode(1, "Arrow");
        member.GlobalTags.Add(MakeTagNode("intent:activity:fight", "intent:activity"));
        member.ContextualTags[1] = new List<TagNode> { MakeTagNode("intent:activity:fight", "intent:activity") };
        group.MemberShips.Add(member);

        // Unassigned ship has escort intent (missing from the group)
        var candidate = MakeShipNode(2, "Vanguard");
        candidate.GlobalTags.Add(MakeTagNode("intent:activity:escort", "intent:activity"));

        var graph = new FleetGraph { Ships = [member, candidate], Groups = [group] };
        var recs = _service.GetRecommendations(graph);

        recs.Should().Contain(r =>
            r.Kind == RecommendationKind.Complement
            && r.TargetShipId == 2);
    }

    // ── Pattern 6: GroupCoherence ────────────────────────────────────

    [Fact]
    public void GroupCoherence_LessThanHalfAligned_Detected()
    {
        var group = MakeGroupNode(1, "Combat Group");
        group.DoctrineAndFocusTags.Add(MakeTagNode("doctrine:primary-arm", "doctrine"));

        // 3 ships: only 1 has a combat intent → 33% coherence < 50%
        var fighter = MakeShipNode(1, "Gladius");
        fighter.ContextualTags[1] = new List<TagNode> { MakeTagNode("intent:activity:fight", "intent:activity") };
        group.MemberShips.Add(fighter);

        var miner = MakeShipNode(2, "Prospector");
        miner.ContextualTags[1] = new List<TagNode> { MakeTagNode("intent:activity:mine", "intent:activity") };
        group.MemberShips.Add(miner);

        var hauler = MakeShipNode(3, "Hull A");
        hauler.ContextualTags[1] = new List<TagNode> { MakeTagNode("intent:activity:haul", "intent:activity") };
        group.MemberShips.Add(hauler);

        var graph = new FleetGraph { Ships = [fighter, miner, hauler], Groups = [group] };
        var recs = _service.GetRecommendations(graph);

        recs.Should().Contain(r =>
            r.Kind == RecommendationKind.GroupCoherence
            && r.ScopeId == 1);
    }

    // ── Pattern 7: AccountIntentDistribution ─────────────────────────

    [Fact]
    public void AccountIntentDistribution_MissingMajorIntents_Detected()
    {
        // Only a fighter — missing mine, salvage, haul, heal, support, and economy intents
        var ship = MakeShipNode(1, "Gladius", daysOld: 1);
        ship.GlobalTags.Add(MakeTagNode("intent:activity:fight", "intent:activity"));
        ship.GlobalTags.Add(MakeTagNode("intent:economy:combat-loop", "intent:economy"));
        ship.GlobalTags.Add(MakeTagNode("doctrine:value:backbone", "doctrine:value"));
        ship.GlobalTags.Add(MakeTagNode("status:lifecycle:owned", "status:lifecycle"));

        var graph = new FleetGraph { Ships = [ship], Groups = [] };
        var recs = _service.GetRecommendations(graph);

        recs.Should().Contain(r =>
            r.Kind == RecommendationKind.AccountRoleDistribution);
    }

    // ── Pattern 9: RemoveFromGroup ───────────────────────────────────

    [Fact]
    public void RemoveFromGroup_ShipDoesNotContribute_Detected()
    {
        var group = MakeGroupNode(1, "Combat Group");
        group.DoctrineAndFocusTags.Add(MakeTagNode("doctrine:primary-arm", "doctrine"));

        // Ship with only mining intent — doesn't match combat doctrine capabilities
        var miner = MakeShipNode(1, "Prospector");
        miner.ContextualTags[1] = new List<TagNode> { MakeTagNode("intent:activity:mine", "intent:activity") };
        group.MemberShips.Add(miner);

        var graph = new FleetGraph { Ships = [miner], Groups = [group] };
        var recs = _service.GetRecommendations(graph);

        recs.Should().Contain(r =>
            r.Kind == RecommendationKind.RemoveFromGroup
            && r.ScopeId == 1);
    }

    // ── Pattern 11: TradeoffSuggestions ───────────────────────────────

    [Fact]
    public void TradeoffSuggestion_SoloOnMulticrew_SuggestsUndercrew()
    {
        var group = MakeGroupNode(1, "Solo Ops");

        var ship = MakeShipNode(1, "MOLE", crewMin: 4);
        ship.ContextualTags[1] = new List<TagNode>
        {
            MakeTagNode("intent:crew:solo", "intent:crew")
        };
        group.MemberShips.Add(ship);

        var graph = new FleetGraph { Ships = [ship], Groups = [group] };
        var recs = _service.GetRecommendations(graph);

        recs.Should().Contain(r =>
            r.TargetTagKey == "tradeoff:undercrew"
            && r.ScopeId == 1);
    }

    [Fact]
    public void TradeoffSuggestion_BackboneConceptOrPledged_FleetGap()
    {
        var group = MakeGroupNode(1, "Main Fleet");

        var ship = MakeShipNode(1, "Polaris", crewMin: 14);
        ship.GlobalTags.Add(MakeTagNode("doctrine:value:backbone", "doctrine:value"));
        ship.GlobalTags.Add(MakeTagNode("status:lifecycle:concept", "status:lifecycle"));
        ship.ContextualTags[1] = new List<TagNode>();
        group.MemberShips.Add(ship);

        var graph = new FleetGraph { Ships = [ship], Groups = [group] };
        var recs = _service.GetRecommendations(graph);

        recs.Should().Contain(r =>
            r.Kind == RecommendationKind.DoctrineMismatch
            && r.Summary.Contains("backbone") && r.Summary.Contains("Concept"));
    }

    [Fact]
    public void TradeoffSuggestion_UndermmannedPrimaryArm_ReadinessGap()
    {
        var group = MakeGroupNode(1, "Alpha Squad");
        group.DoctrineAndFocusTags.Add(MakeTagNode("doctrine:primary-arm", "doctrine"));
        group.DoctrineAndFocusTags.Add(MakeTagNode("status:undermanned", "status"));

        var graph = new FleetGraph { Ships = [], Groups = [group] };
        var recs = _service.GetRecommendations(graph);

        recs.Should().Contain(r =>
            r.Kind == RecommendationKind.CapabilityGap
            && r.Score >= 0.9
            && r.ScopeId == 1);
    }

    [Fact]
    public void TradeoffSuppression_UndercrewPresent_EmitsOpportunityTarget()
    {
        var group = MakeGroupNode(1, "Solo Mining");

        var ship = MakeShipNode(1, "MOLE", crewMin: 4);
        ship.ContextualTags[1] = new List<TagNode>
        {
            MakeTagNode("intent:crew:solo", "intent:crew"),
            MakeTagNode("tradeoff:undercrew", "tradeoff")
        };
        group.MemberShips.Add(ship);

        var graph = new FleetGraph { Ships = [ship], Groups = [group] };
        var recs = _service.GetRecommendations(graph);

        recs.Should().Contain(r =>
            r.Kind == RecommendationKind.OpportunityTarget
            && r.TargetTagKey == "tradeoff:undercrew"
            && r.ScopeId == 1);

        // Should NOT have the warning suggestion (suppressed)
        recs.Should().NotContain(r =>
            r.Kind == RecommendationKind.UnderDescribedShip
            && r.TargetTagKey == "tradeoff:undercrew");
    }

    // ── Pattern 12: PotencyMismatches ────────────────────────────────

    [Fact]
    public void PotencyMismatch_DominantFootprintRecon_Detected()
    {
        var group = MakeGroupNode(1, "Recon Wing");

        var ship = MakeShipNode(1, "Javelin");
        ship.ContextualTags[1] = new List<TagNode>
        {
            MakeTagNode("potency:footprint:dominant", "potency:footprint"),
            MakeTagNode("intent:activity:recon", "intent:activity")
        };
        group.MemberShips.Add(ship);

        var graph = new FleetGraph { Ships = [ship], Groups = [group] };
        var recs = _service.GetRecommendations(graph);

        recs.Should().Contain(r =>
            r.Kind == RecommendationKind.GroupCoherence
            && r.Summary.Contains("dominant footprint")
            && r.Summary.Contains("recon"));
    }

    [Fact]
    public void PotencyMismatch_TokenCapacityBackbone_Detected()
    {
        var group = MakeGroupNode(1, "Main Group");

        var ship = MakeShipNode(1, "Aurora MR");
        ship.GlobalTags.Add(MakeTagNode("doctrine:value:backbone", "doctrine:value"));
        ship.ContextualTags[1] = new List<TagNode>
        {
            MakeTagNode("potency:capacity:token", "potency:capacity")
        };
        group.MemberShips.Add(ship);

        var graph = new FleetGraph { Ships = [ship], Groups = [group] };
        var recs = _service.GetRecommendations(graph);

        recs.Should().Contain(r =>
            r.Kind == RecommendationKind.DoctrineMismatch
            && r.Summary.Contains("backbone")
            && r.Summary.Contains("token capacity"));
    }

    [Fact]
    public void PotencyMismatch_HighFootprintTradeoff_EmitsOpportunityTarget()
    {
        var group = MakeGroupNode(1, "Stealth Recon");

        var ship = MakeShipNode(1, "Javelin");
        ship.ContextualTags[1] = new List<TagNode>
        {
            MakeTagNode("potency:footprint:dominant", "potency:footprint"),
            MakeTagNode("intent:activity:hack", "intent:activity"),
            MakeTagNode("tradeoff:high-footprint", "tradeoff")
        };
        group.MemberShips.Add(ship);

        var graph = new FleetGraph { Ships = [ship], Groups = [group] };
        var recs = _service.GetRecommendations(graph);

        recs.Should().Contain(r =>
            r.Kind == RecommendationKind.OpportunityTarget
            && r.TargetTagKey == "tradeoff:high-footprint");

        // Should NOT have the warning (suppressed)
        recs.Should().NotContain(r =>
            r.Kind == RecommendationKind.GroupCoherence
            && r.Summary.Contains("dominant footprint"));
    }

    // ── Pattern 13: OrphanedTradeoffs ────────────────────────────────

    [Fact]
    public void OrphanedTradeoff_TradeoffWithoutIntent_Detected()
    {
        var group = MakeGroupNode(1, "Test Group");

        var ship = MakeShipNode(1, "Mystery Ship");
        ship.ContextualTags[1] = new List<TagNode>
        {
            MakeTagNode("tradeoff:undercrew", "tradeoff")
        };
        group.MemberShips.Add(ship);

        var graph = new FleetGraph { Ships = [ship], Groups = [group] };
        var recs = _service.GetRecommendations(graph);

        recs.Should().Contain(r =>
            r.Kind == RecommendationKind.OrphanedTradeoff
            && r.ScopeId == 1);
    }

    // ── Pattern 14: VersatilityHighlight ─────────────────────────────

    [Fact]
    public void VersatilityHighlight_DailyDriverMultipleGroupsDifferentIntents_Detected()
    {
        var group1 = MakeGroupNode(1, "Combat Wing");
        var group2 = MakeGroupNode(2, "Hauling Ops");

        var ship = MakeShipNode(1, "Cutlass Black");
        ship.GlobalTags.Add(MakeTagNode("doctrine:frequency:daily-driver", "doctrine:frequency"));
        ship.ContextualTags[1] = new List<TagNode>
        {
            MakeTagNode("intent:activity:fight", "intent:activity")
        };
        ship.ContextualTags[2] = new List<TagNode>
        {
            MakeTagNode("intent:activity:haul", "intent:activity")
        };

        group1.MemberShips.Add(ship);
        group2.MemberShips.Add(ship);

        var graph = new FleetGraph { Ships = [ship], Groups = [group1, group2] };
        var recs = _service.GetRecommendations(graph);

        recs.Should().Contain(r =>
            r.Kind == RecommendationKind.VersatilityHighlight
            && r.ScopeId == 1);
    }
}
