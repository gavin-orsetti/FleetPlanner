using FleetPlanner.Graph;
using FleetPlanner.Models;

namespace FleetPlanner.Services;

/// <summary>
/// Graph-driven recommendation engine implementing analytical patterns that analyse
/// the user's fleet composition and produce actionable <see cref="Recommendation"/> objects.
///
/// <para><b>Architecture:</b> Sits in the services layer, operating entirely on the
/// in-memory <see cref="FleetGraph"/> — no I/O, no database calls. The graph must be
/// pre-built by <see cref="IGraphBuildService"/> before calling <see cref="GetRecommendations"/>.
/// This service is stateless and safe to call from any thread.</para>
///
/// <para><b>5-pillar tag system:</b> The engine works with five tag pillars:
/// <list type="bullet">
///   <item><b>doctrine:</b> — fleet-scoped persistent beliefs (value, frequency, investment, identity, structural)</item>
///   <item><b>intent:</b> — group-scoped deployment commitment (activity, economy, crew, legal, org)</item>
///   <item><b>potency:</b> — group-scoped force multiplication (capacity, reach, resilience, footprint)</item>
///   <item><b>status:</b> — fleet-scoped lifecycle state (lifecycle, modifier)</item>
///   <item><b>tradeoff:</b> — group-scoped accepted downsides (suppress matching warnings)</item>
/// </list></para>
///
/// <para><b>Tradeoff suppression:</b> When a tradeoff tag is present, the matching warning
/// is suppressed and the ship is flagged as an opportunity target instead.</para>
/// </summary>
public class RecommendationService : IRecommendationService
{
    /// <summary>
    /// The intent:economy and intent:activity tag keys that define a "well-rounded" fleet.
    /// Missing any of these triggers a gap recommendation.
    /// </summary>
    private static readonly string[] MajorIntentCategories =
    [
        "intent:economy:combat-loop", "intent:economy:extraction-loop", "intent:economy:logistics-loop",
        "intent:economy:support-loop", "intent:economy:intel-loop",
        "intent:activity:fight", "intent:activity:mine", "intent:activity:salvage",
        "intent:activity:haul", "intent:activity:heal", "intent:activity:support"
    ];

    /// <summary>
    /// Maps group doctrine tag keys to the intent/capability tag keys that doctrine implies.
    /// Used by capability gap, coherence, and remove-from-group patterns.
    /// </summary>
    private static readonly Dictionary<string, string[]> DoctrineCapabilities = new()
    {
        ["doctrine:primary-arm"] = ["intent:activity:fight", "intent:activity:patrol", "intent:economy:combat-loop"],
        ["doctrine:support-echelon"] = ["intent:activity:support", "intent:activity:heal", "intent:economy:support-loop"],
        ["doctrine:specialist-detachment"] = ["intent:activity:scan", "intent:activity:hack", "intent:economy:intel-loop"],
        ["doctrine:carrier-element"] = ["intent:activity:command", "intent:activity:fight", "intent:activity:support"],
        ["doctrine:rapid-response"] = ["intent:activity:fight", "intent:activity:respond", "intent:activity:escort"],
        ["doctrine:reserve-force"] = ["intent:activity:fight", "intent:activity:support"]
    };

    /// <inheritdoc/>
    public List<Recommendation> GetRecommendations(FleetGraph graph)
    {
        var recs = new List<Recommendation>();

        // Run all analytical patterns
        recs.AddRange(AnalyseCapabilityGaps(graph));
        recs.AddRange(AnalyseRedundancy(graph));
        recs.AddRange(AnalyseComplement(graph));
        recs.AddRange(AnalyseUnderDescribedShips(graph));
        recs.AddRange(AnalyseUnassignedShips(graph));
        recs.AddRange(AnalyseGroupCoherence(graph));
        recs.AddRange(AnalyseAccountIntentDistribution(graph));
        recs.AddRange(AnalyseDoctrineMismatch(graph));
        recs.AddRange(AnalyseRemoveFromGroup(graph));
        recs.AddRange(AnalyseCrewEfficiency(graph));
        recs.AddRange(AnalyseTradeoffSuggestions(graph));
        recs.AddRange(AnalysePotencyMismatches(graph));

        // Sort by score descending (highest priority first)
        return recs.OrderByDescending(r => r.Score).ToList();
    }

    /// <summary>
    /// Pattern 1: CapabilityGap — a group's doctrine implies capabilities that no member ship provides.
    /// </summary>
    private static List<Recommendation> AnalyseCapabilityGaps(FleetGraph graph)
    {
        var recs = new List<Recommendation>();

        foreach (var group in graph.Groups)
        {
            foreach (var doctrineTag in group.DoctrineAndFocusTags)
            {
                if (!DoctrineCapabilities.TryGetValue(doctrineTag.Definition.Key, out var requiredTags))
                    continue;

                var memberTagKeys = new HashSet<string>();
                foreach (var ship in group.MemberShips)
                {
                    foreach (var t in ship.GlobalTags)
                        memberTagKeys.Add(t.Definition.Key);
                    if (ship.ContextualTags.TryGetValue(group.Group.Id, out var ctxTags))
                        foreach (var t in ctxTags)
                            memberTagKeys.Add(t.Definition.Key);
                }

                var missing = requiredTags.Where(r => !memberTagKeys.Contains(r)).ToList();
                if (missing.Count > 0)
                {
                    recs.Add(new Recommendation
                    {
                        ScopeType = RecommendationScope.Group,
                        ScopeId = group.Group.Id,
                        Kind = RecommendationKind.CapabilityGap,
                        TargetTagKey = missing.First(),
                        Score = RecommendationWeights.CapabilityGapWeight,
                        Priority = RecommendationPriority.High,
                        Summary = $"Group '{group.Group.Name}' is missing {string.Join(", ", missing.Select(FormatTagKey))} capability",
                        Explanation = $"The group's {doctrineTag.Definition.DisplayName} doctrine expects ships with {string.Join(", ", missing.Select(FormatTagKey))} but none of the {group.MemberShips.Count} member ships provide this.",
                        Evidence = missing.Select(m => $"Missing: {FormatTagKey(m)}").ToList(),
                        SuggestedActions = [$"Add a ship with the {FormatTagKey(missing.First())} tag to this group"]
                    });
                }
            }
        }

        return recs;
    }

    /// <summary>
    /// Pattern 2: Redundancy — multiple ships in a group share the same primary (weight 1) intent:activity tag.
    /// </summary>
    private static List<Recommendation> AnalyseRedundancy(FleetGraph graph)
    {
        var recs = new List<Recommendation>();

        foreach (var group in graph.Groups)
        {
            var primaryIntents = new Dictionary<string, List<ShipNode>>();

            foreach (var ship in group.MemberShips)
            {
                var intentTags = GetIntentTags(ship, group.Group.Id)
                    .Where(t => t.Weight == 1)
                    .ToList();

                foreach (var tag in intentTags)
                {
                    var key = tag.Definition.Key;
                    if (!primaryIntents.ContainsKey(key))
                        primaryIntents[key] = new List<ShipNode>();
                    primaryIntents[key].Add(ship);
                }
            }

            foreach (var (intentKey, ships) in primaryIntents.Where(kv => kv.Value.Count > 1))
            {
                var score = RecommendationWeights.RedundancyWeight;
                recs.Add(new Recommendation
                {
                    ScopeType = RecommendationScope.Group,
                    ScopeId = group.Group.Id,
                    Kind = RecommendationKind.Redundancy,
                    TargetTagKey = intentKey,
                    Score = score,
                    Priority = score >= 0.7 ? RecommendationPriority.Medium : RecommendationPriority.Low,
                    Summary = $"{ships.Count} ships share primary intent {FormatTagKey(intentKey)} in '{group.Group.Name}'",
                    Explanation = $"Ships {string.Join(", ", ships.Select(s => s.CatalogueShip.Name))} all have {FormatTagKey(intentKey)} as their primary intent. Consider diversifying roles.",
                    Evidence = ships.Select(s => $"{s.CatalogueShip.Name}: primary {FormatTagKey(intentKey)}").ToList(),
                    SuggestedActions = [$"Reassign one ship's primary intent or move it to a different group"]
                });
            }
        }

        return recs;
    }

    /// <summary>
    /// Pattern 3: Complement — a ship not yet in a group has intent tags that would fill a gap.
    /// </summary>
    private static List<Recommendation> AnalyseComplement(FleetGraph graph)
    {
        var recs = new List<Recommendation>();

        foreach (var group in graph.Groups)
        {
            var groupIntentKeys = new HashSet<string>();
            foreach (var ship in group.MemberShips)
                foreach (var t in GetIntentTags(ship, group.Group.Id))
                    groupIntentKeys.Add(t.Definition.Key);

            foreach (var ship in graph.Ships)
            {
                if (group.MemberShips.Any(m => m.OwnedShipId == ship.OwnedShipId))
                    continue;

                var shipIntents = ship.GlobalTags
                    .Where(t => t.Definition.Category.StartsWith("intent:", StringComparison.Ordinal))
                    .Select(t => t.Definition.Key)
                    .ToList();

                var complementary = shipIntents.Where(r => !groupIntentKeys.Contains(r)).ToList();
                if (complementary.Count > 0)
                {
                    recs.Add(new Recommendation
                    {
                        ScopeType = RecommendationScope.Group,
                        ScopeId = group.Group.Id,
                        Kind = RecommendationKind.Complement,
                        TargetShipId = ship.CatalogueShip.Id,
                        Score = RecommendationWeights.ComplementWeight,
                        Priority = RecommendationPriority.Medium,
                        Summary = $"{ship.CatalogueShip.Name} would add {FormatTagKey(complementary.First())} to '{group.Group.Name}'",
                        Explanation = $"This ship has {string.Join(", ", complementary.Select(FormatTagKey))} which the group currently lacks.",
                        Evidence = complementary.Select(c => $"Missing in group: {FormatTagKey(c)}").ToList(),
                        SuggestedActions = [$"Add {ship.CatalogueShip.Name} to group '{group.Group.Name}'"]
                    });
                }
            }
        }

        return recs;
    }

    /// <summary>
    /// Pattern 4: UnderDescribedShip — an owned ship has fewer than 2 meaningful global tags.
    /// </summary>
    private static List<Recommendation> AnalyseUnderDescribedShips(FleetGraph graph)
    {
        var recs = new List<Recommendation>();

        foreach (var ship in graph.Ships)
        {
            var meaningfulTags = ship.GlobalTags.Count;

            if (meaningfulTags < 2)
            {
                recs.Add(new Recommendation
                {
                    ScopeType = RecommendationScope.Ship,
                    ScopeId = ship.OwnedShipId,
                    Kind = RecommendationKind.UnderDescribedShip,
                    Score = RecommendationWeights.UnderDescribedWeight,
                    Priority = RecommendationPriority.Low,
                    Summary = $"{ship.CatalogueShip.Name} has only {meaningfulTags} tag(s)",
                    Explanation = "Ships with fewer than 2 tags may not be matched correctly by the recommendation engine. Add doctrine or status tags.",
                    Evidence = [$"Current tags: {meaningfulTags}"],
                    SuggestedActions = ["Add doctrine tags (e.g. doctrine:value:backbone)", "Add status tags (e.g. status:lifecycle:owned)"]
                });
            }
        }

        return recs;
    }

    /// <summary>
    /// Pattern 5: UnassignedShip — an owned ship has been in the collection for over 7 days
    /// but is not a member of any fleet group.
    /// </summary>
    private static List<Recommendation> AnalyseUnassignedShips(FleetGraph graph)
    {
        var recs = new List<Recommendation>();
        var cutoff = DateTime.UtcNow.AddDays(-7);

        foreach (var ship in graph.Ships)
        {
            if (ship.OwnedShip.CreatedUtc > cutoff)
                continue;

            var isInAnyGroup = graph.Groups.Any(g => g.MemberShips.Any(m => m.OwnedShipId == ship.OwnedShipId));
            if (!isInAnyGroup)
            {
                recs.Add(new Recommendation
                {
                    ScopeType = RecommendationScope.Ship,
                    ScopeId = ship.OwnedShipId,
                    Kind = RecommendationKind.UnassignedShip,
                    Score = RecommendationWeights.UnassignedShipWeight,
                    Priority = RecommendationPriority.Low,
                    Summary = $"{ship.CatalogueShip.Name} is not assigned to any group",
                    Explanation = $"This ship has been in your collection for over 7 days but hasn't been assigned to any fleet group. Consider adding it to a group for better fleet organisation.",
                    Evidence = [$"Added: {ship.OwnedShip.CreatedUtc:yyyy-MM-dd}"],
                    SuggestedActions = ["Assign this ship to a fleet group with matching doctrine"]
                });
            }
        }

        return recs;
    }

    /// <summary>
    /// Pattern 6: GroupCoherence — fewer than 50% of a group's ships have intent tags
    /// that align with the group's doctrine.
    /// </summary>
    private static List<Recommendation> AnalyseGroupCoherence(FleetGraph graph)
    {
        var recs = new List<Recommendation>();

        foreach (var group in graph.Groups)
        {
            if (group.MemberShips.Count == 0 || group.DoctrineAndFocusTags.Count == 0)
                continue;

            var doctrineKeys = group.DoctrineAndFocusTags.Select(t => t.Definition.Key).ToHashSet();

            foreach (var doctrineKey in doctrineKeys)
            {
                if (!DoctrineCapabilities.TryGetValue(doctrineKey, out var alignedIntents))
                    continue;

                var alignedCount = 0;
                foreach (var ship in group.MemberShips)
                {
                    var shipTags = GetAllTagKeys(ship, group.Group.Id);
                    if (alignedIntents.Any(r => shipTags.Contains(r)))
                        alignedCount++;
                }

                var coherenceRatio = (double)alignedCount / group.MemberShips.Count;
                if (coherenceRatio < 0.5)
                {
                    var score = (1.0 - coherenceRatio) * RecommendationWeights.CapabilityGapWeight * 0.8;
                    recs.Add(new Recommendation
                    {
                        ScopeType = RecommendationScope.Group,
                        ScopeId = group.Group.Id,
                        Kind = RecommendationKind.GroupCoherence,
                        Score = score,
                        Priority = score >= 0.7 ? RecommendationPriority.High : RecommendationPriority.Medium,
                        Summary = $"Low coherence in '{group.Group.Name}': {coherenceRatio:P0} of ships align with {FormatTagKey(doctrineKey)}",
                        Explanation = $"Only {alignedCount} of {group.MemberShips.Count} ships have intents that match the group's {FormatTagKey(doctrineKey)} doctrine.",
                        Evidence = [$"{alignedCount}/{group.MemberShips.Count} ships aligned"],
                        SuggestedActions = ["Add ships that match the group's doctrine", "Reassign misaligned ships to a different group"]
                    });
                }
            }
        }

        return recs;
    }

    /// <summary>
    /// Pattern 7: AccountIntentDistribution — the user's fleet is missing major intent categories.
    /// </summary>
    private static List<Recommendation> AnalyseAccountIntentDistribution(FleetGraph graph)
    {
        var recs = new List<Recommendation>();

        if (graph.Ships.Count == 0)
            return recs;

        var coveredIntents = new HashSet<string>();
        foreach (var ship in graph.Ships)
        {
            foreach (var t in ship.GlobalTags.Where(t => t.Definition.Category.StartsWith("intent:", StringComparison.Ordinal)))
                coveredIntents.Add(t.Definition.Key);
            foreach (var ctxTags in ship.ContextualTags.Values)
                foreach (var t in ctxTags.Where(t => t.Definition.Category.StartsWith("intent:", StringComparison.Ordinal)))
                    coveredIntents.Add(t.Definition.Key);
        }

        var missingIntents = MajorIntentCategories.Where(r => !coveredIntents.Contains(r)).ToList();
        if (missingIntents.Count > 0)
        {
            var score = 0.5 * ((double)missingIntents.Count / MajorIntentCategories.Length);
            recs.Add(new Recommendation
            {
                ScopeType = RecommendationScope.Account,
                Kind = RecommendationKind.AccountRoleDistribution,
                Score = score,
                Priority = missingIntents.Count >= 4 ? RecommendationPriority.High
                    : missingIntents.Count >= 2 ? RecommendationPriority.Medium
                    : RecommendationPriority.Low,
                Summary = $"Collection missing {missingIntents.Count} major intent(s): {string.Join(", ", missingIntents.Select(FormatTagKey))}",
                Explanation = "A well-rounded fleet benefits from coverage across major intent categories. Consider acquiring ships to fill these gaps.",
                Evidence = missingIntents.Select(r => $"Missing: {FormatTagKey(r)}").ToList(),
                SuggestedActions = missingIntents.Select(r => $"Acquire a ship for {FormatTagKey(r)}").ToList()
            });
        }

        return recs;
    }

    /// <summary>
    /// Pattern 8: DoctrineMismatch — a ship's primary (weight 1) doctrine tag conflicts with its group's doctrine.
    /// </summary>
    private static List<Recommendation> AnalyseDoctrineMismatch(FleetGraph graph)
    {
        var recs = new List<Recommendation>();

        foreach (var group in graph.Groups)
        {
            var groupDoctrine = group.DoctrineAndFocusTags
                .Where(t => t.Definition.Category.StartsWith("doctrine", StringComparison.Ordinal))
                .Select(t => t.Definition.Key)
                .ToHashSet();

            if (groupDoctrine.Count == 0)
                continue;

            foreach (var ship in group.MemberShips)
            {
                var shipDoctrine = GetAllTags(ship, group.Group.Id)
                    .Where(t => t.Definition.Category.StartsWith("doctrine", StringComparison.Ordinal) && t.Weight == 1)
                    .ToList();

                foreach (var dt in shipDoctrine)
                {
                    if (!groupDoctrine.Contains(dt.Definition.Key))
                    {
                        recs.Add(new Recommendation
                        {
                            ScopeType = RecommendationScope.Ship,
                            ScopeId = ship.OwnedShipId,
                            Kind = RecommendationKind.DoctrineMismatch,
                            TargetTagKey = dt.Definition.Key,
                            Score = RecommendationWeights.DoctrineMismatchWeight,
                            Priority = RecommendationPriority.High,
                            Summary = $"{ship.CatalogueShip.Name}'s doctrine ({dt.Definition.DisplayName}) conflicts with group '{group.Group.Name}'",
                            Explanation = $"This ship's primary doctrine is {dt.Definition.DisplayName} but the group focuses on {string.Join(", ", group.DoctrineAndFocusTags.Select(t => t.Definition.DisplayName))}.",
                            Evidence = [$"Ship doctrine: {dt.Definition.DisplayName}", $"Group doctrine: {string.Join(", ", groupDoctrine.Select(FormatTagKey))}"],
                            SuggestedActions = ["Move this ship to a group with matching doctrine", "Change the ship's doctrine tag to match the group"]
                        });
                    }
                }
            }
        }

        return recs;
    }

    /// <summary>
    /// Pattern 9: RemoveFromGroup — a member ship's tags don't match any of the group's
    /// doctrine-derived intent requirements.
    /// </summary>
    private static List<Recommendation> AnalyseRemoveFromGroup(FleetGraph graph)
    {
        var recs = new List<Recommendation>();

        foreach (var group in graph.Groups)
        {
            if (group.DoctrineAndFocusTags.Count == 0)
                continue;

            var desiredTagKeys = new HashSet<string>();
            foreach (var dt in group.DoctrineAndFocusTags)
            {
                if (DoctrineCapabilities.TryGetValue(dt.Definition.Key, out var caps))
                    foreach (var cap in caps)
                        desiredTagKeys.Add(cap);
            }

            if (desiredTagKeys.Count == 0)
                continue;

            foreach (var ship in group.MemberShips)
            {
                var shipTags = GetAllTagKeys(ship, group.Group.Id);
                var hasContributing = desiredTagKeys.Any(d => shipTags.Contains(d));

                if (!hasContributing)
                {
                    var score = RecommendationWeights.RedundancyWeight * 0.8;
                    recs.Add(new Recommendation
                    {
                        ScopeType = RecommendationScope.Ship,
                        ScopeId = ship.OwnedShipId,
                        Kind = RecommendationKind.RemoveFromGroup,
                        Score = score,
                        Priority = RecommendationPriority.Low,
                        Summary = $"{ship.CatalogueShip.Name} doesn't contribute to '{group.Group.Name}'",
                        Explanation = $"This ship's tags don't align with any of the group's doctrine capabilities. It may be better suited to a different group.",
                        Evidence = [$"Ship tags: {string.Join(", ", shipTags.Select(FormatTagKey))}", $"Group needs: {string.Join(", ", desiredTagKeys.Select(FormatTagKey))}"],
                        SuggestedActions = [$"Remove {ship.CatalogueShip.Name} from this group", "Add relevant intent tags to the ship"]
                    });
                }
            }
        }

        return recs;
    }

    /// <summary>
    /// Pattern 10: CrewEfficiency — group crew requirements significantly exceed or underutilise the target.
    /// </summary>
    private static List<Recommendation> AnalyseCrewEfficiency(FleetGraph graph)
    {
        var recs = new List<Recommendation>();

        foreach (var group in graph.Groups)
        {
            if (group.MemberShips.Count == 0)
                continue;

            var totalMinCrew = group.MemberShips.Sum(s => s.CatalogueShip.CrewMin);
            var crewTarget = group.Group.CrewTarget;

            if (crewTarget <= 0)
                continue;

            var ratio = (double)totalMinCrew / crewTarget;

            if (ratio > 1.5)
            {
                var score = RecommendationWeights.CrewEfficiencyWeight;
                recs.Add(new Recommendation
                {
                    ScopeType = RecommendationScope.Group,
                    ScopeId = group.Group.Id,
                    Kind = RecommendationKind.CrewEfficiency,
                    Score = score,
                    Priority = RecommendationPriority.Medium,
                    Summary = $"Group '{group.Group.Name}' needs {totalMinCrew} crew but target is {crewTarget}",
                    Explanation = $"The minimum crew required to operate all ships ({totalMinCrew}) significantly exceeds the crew target ({crewTarget}). Some ships won't have enough crew.",
                    Evidence = [$"Min crew needed: {totalMinCrew}", $"Crew target: {crewTarget}", $"Ratio: {ratio:F1}x"],
                    SuggestedActions = ["Remove ships with high crew requirements", "Increase the crew target", "Replace multi-crew ships with solo-operable alternatives"]
                });
            }
            else if (ratio < 0.3 && crewTarget >= 3)
            {
                var score = RecommendationWeights.CrewEfficiencyWeight * 0.7;
                recs.Add(new Recommendation
                {
                    ScopeType = RecommendationScope.Group,
                    ScopeId = group.Group.Id,
                    Kind = RecommendationKind.CrewEfficiency,
                    Score = score,
                    Priority = RecommendationPriority.Low,
                    Summary = $"Group '{group.Group.Name}' underutilises crew ({totalMinCrew} needed, {crewTarget} available)",
                    Explanation = $"The group's ships only require {totalMinCrew} minimum crew but you have {crewTarget} players available. Consider adding multi-crew ships.",
                    Evidence = [$"Min crew needed: {totalMinCrew}", $"Crew target: {crewTarget}"],
                    SuggestedActions = ["Add multi-crew ships to better utilise available crew", "Reduce crew target if players aren't available"]
                });
            }
        }

        return recs;
    }

    /// <summary>
    /// Pattern 11: TradeoffSuggestions — detect crew mismatches and suggest tradeoff tags.
    /// <para>When <c>intent:crew:solo</c> is set on a ship with <c>CrewMin &gt; 1</c>,
    /// suggest <c>tradeoff:undercrew</c> unless already present.</para>
    /// </summary>
    private static List<Recommendation> AnalyseTradeoffSuggestions(FleetGraph graph)
    {
        var recs = new List<Recommendation>();

        foreach (var group in graph.Groups)
        {
            foreach (var ship in group.MemberShips)
            {
                var allTags = GetAllTagKeys(ship, group.Group.Id);

                // intent:crew:solo on multi-crew ship → suggest tradeoff:undercrew
                if (allTags.Contains("intent:crew:solo") && ship.CatalogueShip.CrewMin > 1)
                {
                    if (!allTags.Contains("tradeoff:undercrew"))
                    {
                        recs.Add(new Recommendation
                        {
                            ScopeType = RecommendationScope.Ship,
                            ScopeId = ship.OwnedShipId,
                            Kind = RecommendationKind.UnderDescribedShip,
                            TargetTagKey = "tradeoff:undercrew",
                            Score = 0.5,
                            Priority = RecommendationPriority.Low,
                            Summary = $"{ship.CatalogueShip.Name} is solo-crewing a {ship.CatalogueShip.CrewMin}-crew ship in '{group.Group.Name}'",
                            Explanation = $"This ship requires {ship.CatalogueShip.CrewMin} crew minimum but you intend to fly solo. Consider adding the tradeoff:undercrew tag to acknowledge this.",
                            Evidence = [$"Crew min: {ship.CatalogueShip.CrewMin}", "Intent: solo"],
                            SuggestedActions = ["Add tradeoff:undercrew to acknowledge the efficiency loss"]
                        });
                    }
                }

                // status:lifecycle:concept or status:lifecycle:pledged + doctrine:value:backbone → acquisition gap
                if (allTags.Contains("doctrine:value:backbone"))
                {
                    if (allTags.Contains("status:lifecycle:concept") || allTags.Contains("status:lifecycle:pledged"))
                    {
                        var statusTag = allTags.Contains("status:lifecycle:concept") ? "Concept" : "Pledged";
                        recs.Add(new Recommendation
                        {
                            ScopeType = RecommendationScope.Ship,
                            ScopeId = ship.OwnedShipId,
                            Kind = RecommendationKind.DoctrineMismatch,
                            Score = 0.8,
                            Priority = RecommendationPriority.High,
                            Summary = $"{ship.CatalogueShip.Name} is a fleet backbone but only {statusTag}",
                            Explanation = $"This ship is marked as backbone (core to your fleet) but its lifecycle status is {statusTag}. Your fleet has a critical gap until this ship is fully acquired.",
                            Evidence = [$"Doctrine: backbone", $"Status: {statusTag}"],
                            SuggestedActions = ["Prioritize acquisition of this ship", "Identify a temporary substitute"]
                        });
                    }
                }
            }
        }

        // Group-level: status:undermanned + doctrine:primary-arm → readiness gap
        foreach (var group in graph.Groups)
        {
            var groupTags = group.DoctrineAndFocusTags.Select(t => t.Definition.Key).ToHashSet();
            if (groupTags.Contains("doctrine:primary-arm") && groupTags.Contains("status:undermanned"))
            {
                recs.Add(new Recommendation
                {
                    ScopeType = RecommendationScope.Group,
                    ScopeId = group.Group.Id,
                    Kind = RecommendationKind.CapabilityGap,
                    Score = 0.9,
                    Priority = RecommendationPriority.High,
                    Summary = $"Primary arm '{group.Group.Name}' is undermanned",
                    Explanation = "This group is the fleet's primary operational arm but is marked as undermanned. This is a critical readiness gap.",
                    Evidence = ["Doctrine: primary-arm", "Status: undermanned"],
                    SuggestedActions = ["Recruit crew for this group", "Reassign crew from reserve groups"]
                });
            }
        }

        return recs;
    }

    /// <summary>
    /// Pattern 12: PotencyMismatches — detect conflicting potency + intent combinations.
    /// <para><c>potency:footprint:dominant</c> + <c>intent:activity:recon</c> or <c>intent:activity:hack</c> → flag mismatch.</para>
    /// <para><c>potency:capacity:token</c> + <c>doctrine:value:backbone</c> → flag incongruence.</para>
    /// </summary>
    private static List<Recommendation> AnalysePotencyMismatches(FleetGraph graph)
    {
        var recs = new List<Recommendation>();

        foreach (var group in graph.Groups)
        {
            foreach (var ship in group.MemberShips)
            {
                var allTags = GetAllTagKeys(ship, group.Group.Id);

                // potency:footprint:dominant + intent:activity:recon or hack → stealth mismatch
                if (allTags.Contains("potency:footprint:dominant"))
                {
                    if (allTags.Contains("intent:activity:recon") || allTags.Contains("intent:activity:hack"))
                    {
                        // Check if tradeoff:high-footprint suppresses this
                        if (!allTags.Contains("tradeoff:high-footprint"))
                        {
                            var stealthActivity = allTags.Contains("intent:activity:recon") ? "recon" : "hack";
                            recs.Add(new Recommendation
                            {
                                ScopeType = RecommendationScope.Ship,
                                ScopeId = ship.OwnedShipId,
                                Kind = RecommendationKind.GroupCoherence,
                                Score = 0.6,
                                Priority = RecommendationPriority.Medium,
                                Summary = $"{ship.CatalogueShip.Name} has dominant footprint but assigned to {stealthActivity} in '{group.Group.Name}'",
                                Explanation = $"A dominant footprint is a liability for {stealthActivity} operations. Consider adding tradeoff:high-footprint if this is intentional.",
                                Evidence = ["Potency: dominant footprint", $"Intent: {stealthActivity}"],
                                SuggestedActions = ["Add tradeoff:high-footprint to acknowledge this", "Reassign to a non-stealth role"]
                            });
                        }
                    }
                }

                // potency:capacity:token + doctrine:value:backbone → incongruence
                if (allTags.Contains("potency:capacity:token") && allTags.Contains("doctrine:value:backbone"))
                {
                    recs.Add(new Recommendation
                    {
                        ScopeType = RecommendationScope.Ship,
                        ScopeId = ship.OwnedShipId,
                        Kind = RecommendationKind.DoctrineMismatch,
                        Score = 0.7,
                        Priority = RecommendationPriority.Medium,
                        Summary = $"{ship.CatalogueShip.Name} is backbone but only token capacity in '{group.Group.Name}'",
                        Explanation = "This ship is marked as core to the fleet (backbone) but contributes only token capacity in this group. Review whether doctrine or potency assessment needs updating.",
                        Evidence = ["Doctrine: backbone", "Potency: token capacity"],
                        SuggestedActions = ["Review doctrine:value assignment", "Review potency:capacity assessment for this group"]
                    });
                }
            }
        }

        return recs;
    }

    // ── Helpers ────────────────────────────────────────────────────────

    /// <summary>Gets intent-category tags (any intent:* sub-dimension) for a ship in a specific group context.</summary>
    private static List<TagNode> GetIntentTags(ShipNode ship, int groupId)
    {
        var tags = new List<TagNode>();
        tags.AddRange(ship.GlobalTags.Where(t => t.Definition.Category.StartsWith("intent:", StringComparison.Ordinal)));
        if (ship.ContextualTags.TryGetValue(groupId, out var ctxTags))
            tags.AddRange(ctxTags.Where(t => t.Definition.Category.StartsWith("intent:", StringComparison.Ordinal)));
        return tags;
    }

    /// <summary>Gets all tags for a ship (global + contextual for a specific group).</summary>
    private static List<TagNode> GetAllTags(ShipNode ship, int groupId)
    {
        var tags = new List<TagNode>(ship.GlobalTags);
        if (ship.ContextualTags.TryGetValue(groupId, out var ctxTags))
            tags.AddRange(ctxTags);
        return tags;
    }

    /// <summary>Gets all tag keys for a ship (global + contextual for a specific group).</summary>
    private static HashSet<string> GetAllTagKeys(ShipNode ship, int groupId)
    {
        var keys = new HashSet<string>(ship.GlobalTags.Select(t => t.Definition.Key));
        if (ship.ContextualTags.TryGetValue(groupId, out var ctxTags))
            foreach (var t in ctxTags)
                keys.Add(t.Definition.Key);
        return keys;
    }

    /// <summary>Formats a tag key for display (e.g. "intent:activity:fight" → "Fight").</summary>
    private static string FormatTagKey(string key)
    {
        var parts = key.Split(':');
        if (parts.Length < 2) return key;
        return System.Globalization.CultureInfo.CurrentCulture.TextInfo
            .ToTitleCase(parts[^1].Replace('-', ' '));
    }
}
