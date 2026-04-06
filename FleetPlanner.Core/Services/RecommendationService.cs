using FleetPlanner.Graph;
using FleetPlanner.Models;

namespace FleetPlanner.Services;

/// <summary>
/// Graph-driven recommendation engine implementing 10 analytical patterns.
/// Operates entirely on the in-memory <see cref="FleetGraph"/> — no I/O.
/// </summary>
public class RecommendationService : IRecommendationService
{
    /// <summary>Major role categories that a well-rounded collection should cover.</summary>
    private static readonly string[] MajorRoleCategories =
    [
        "role:escort", "role:frontline", "role:hauling", "role:mining",
        "role:salvage", "role:exploration", "role:medical", "role:repair"
    ];

    /// <summary>Doctrine-to-capability mappings for gap detection.</summary>
    private static readonly Dictionary<string, string[]> DoctrineCapabilities = new()
    {
        ["doctrine:industrial"] = ["role:mining", "role:salvage", "role:hauling", "role:refinery", "capability:cargo"],
        ["doctrine:combat"] = ["role:escort", "role:frontline", "role:interdiction", "role:bomber"],
        ["doctrine:exploration"] = ["role:exploration", "role:scanning"],
        ["doctrine:trade"] = ["role:hauling", "capability:cargo"],
        ["doctrine:support"] = ["role:medical", "role:repair", "role:refuel", "capability:medical", "capability:repair", "capability:refuel"]
    };

    /// <inheritdoc/>
    public List<Recommendation> GetRecommendations(FleetGraph graph)
    {
        var recs = new List<Recommendation>();

        // Run all 10 analytical patterns
        recs.AddRange(AnalyseCapabilityGaps(graph));
        recs.AddRange(AnalyseRedundancy(graph));
        recs.AddRange(AnalyseComplement(graph));
        recs.AddRange(AnalyseUnderDescribedShips(graph));
        recs.AddRange(AnalyseUnassignedShips(graph));
        recs.AddRange(AnalyseGroupCoherence(graph));
        recs.AddRange(AnalyseAccountRoleDistribution(graph));
        recs.AddRange(AnalyseDoctrineMismatch(graph));
        recs.AddRange(AnalyseRemoveFromGroup(graph));
        recs.AddRange(AnalyseCrewEfficiency(graph));

        // Sort by score descending (highest priority first)
        return recs.OrderByDescending(r => r.Score).ToList();
    }

    /// <summary>
    /// Pattern 1: CapabilityGap — group has doctrine tag X but lacks ships with matching capability/role tags.
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

                // Collect all tag keys from member ships (global + contextual for this group)
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
    /// Pattern 2: Redundancy — multiple ships in a group carry the same primary role tag.
    /// </summary>
    private static List<Recommendation> AnalyseRedundancy(FleetGraph graph)
    {
        var recs = new List<Recommendation>();

        foreach (var group in graph.Groups)
        {
            var primaryRoles = new Dictionary<string, List<ShipNode>>();

            foreach (var ship in group.MemberShips)
            {
                var roleTags = GetRoleTags(ship, group.Group.Id)
                    .Where(t => t.Weight == 1)
                    .ToList();

                foreach (var roleTag in roleTags)
                {
                    var key = roleTag.Definition.Key;
                    if (!primaryRoles.ContainsKey(key))
                        primaryRoles[key] = new List<ShipNode>();
                    primaryRoles[key].Add(ship);
                }
            }

            foreach (var (roleKey, ships) in primaryRoles.Where(kv => kv.Value.Count > 1))
            {
                var score = RecommendationWeights.RedundancyWeight;
                recs.Add(new Recommendation
                {
                    ScopeType = RecommendationScope.Group,
                    ScopeId = group.Group.Id,
                    Kind = RecommendationKind.Redundancy,
                    TargetTagKey = roleKey,
                    Score = score,
                    Priority = score >= 0.7 ? RecommendationPriority.Medium : RecommendationPriority.Low,
                    Summary = $"{ships.Count} ships share primary role {FormatTagKey(roleKey)} in '{group.Group.Name}'",
                    Explanation = $"Ships {string.Join(", ", ships.Select(s => s.CatalogueShip.Name))} all have {FormatTagKey(roleKey)} as their primary role. Consider diversifying roles.",
                    Evidence = ships.Select(s => $"{s.CatalogueShip.Name}: primary {FormatTagKey(roleKey)}").ToList(),
                    SuggestedActions = [$"Reassign one ship's primary role or move it to a different group"]
                });
            }
        }

        return recs;
    }

    /// <summary>
    /// Pattern 3: Complement — a ship's role tags suggest it would strengthen a group that lacks that role.
    /// </summary>
    private static List<Recommendation> AnalyseComplement(FleetGraph graph)
    {
        var recs = new List<Recommendation>();

        foreach (var group in graph.Groups)
        {
            var groupRoleKeys = new HashSet<string>();
            foreach (var ship in group.MemberShips)
                foreach (var t in GetRoleTags(ship, group.Group.Id))
                    groupRoleKeys.Add(t.Definition.Key);

            // Find unassigned ships that could fill missing roles
            foreach (var ship in graph.Ships)
            {
                if (group.MemberShips.Any(m => m.OwnedShipId == ship.OwnedShipId))
                    continue; // already a member

                var shipRoles = ship.GlobalTags
                    .Where(t => t.Definition.Category == "role")
                    .Select(t => t.Definition.Key)
                    .ToList();

                var complementary = shipRoles.Where(r => !groupRoleKeys.Contains(r)).ToList();
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
    /// Pattern 4: UnderDescribedShip — owned ship has fewer than 2 tags (excluding acquisition tags).
    /// </summary>
    private static List<Recommendation> AnalyseUnderDescribedShips(FleetGraph graph)
    {
        var recs = new List<Recommendation>();

        foreach (var ship in graph.Ships)
        {
            var meaningfulTags = ship.GlobalTags
                .Where(t => t.Definition.Category != "acquisition")
                .Count();

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
                    Explanation = "Ships with fewer than 2 tags may not be matched correctly by the recommendation engine. Add role, doctrine, or crew tags.",
                    Evidence = [$"Current tags: {meaningfulTags}"],
                    SuggestedActions = ["Add role tags (e.g. role:escort)", "Add crew tags (e.g. crew:solo)", "Add doctrine tags"]
                });
            }
        }

        return recs;
    }

    /// <summary>
    /// Pattern 5: UnassignedShip — owned ship not in any group and in collection > 7 days.
    /// </summary>
    private static List<Recommendation> AnalyseUnassignedShips(FleetGraph graph)
    {
        var recs = new List<Recommendation>();
        var cutoff = DateTime.UtcNow.AddDays(-7);

        foreach (var ship in graph.Ships)
        {
            if (ship.OwnedShip.CreatedUtc > cutoff)
                continue; // too new

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
    /// Pattern 6: GroupCoherence — group's ship role tags align poorly with group's doctrine tags.
    /// </summary>
    private static List<Recommendation> AnalyseGroupCoherence(FleetGraph graph)
    {
        var recs = new List<Recommendation>();

        foreach (var group in graph.Groups)
        {
            if (group.MemberShips.Count == 0 || group.DoctrineAndFocusTags.Count == 0)
                continue;

            var doctrineKeys = group.DoctrineAndFocusTags.Select(t => t.Definition.Key).ToHashSet();

            // For each doctrine, check what % of ships have aligned roles
            foreach (var doctrineKey in doctrineKeys)
            {
                if (!DoctrineCapabilities.TryGetValue(doctrineKey, out var alignedRoles))
                    continue;

                var alignedCount = 0;
                foreach (var ship in group.MemberShips)
                {
                    var shipTags = GetAllTagKeys(ship, group.Group.Id);
                    if (alignedRoles.Any(r => shipTags.Contains(r)))
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
                        Explanation = $"Only {alignedCount} of {group.MemberShips.Count} ships have roles that match the group's {FormatTagKey(doctrineKey)} doctrine.",
                        Evidence = [$"{alignedCount}/{group.MemberShips.Count} ships aligned"],
                        SuggestedActions = ["Add ships that match the group's doctrine", "Reassign misaligned ships to a different group"]
                    });
                }
            }
        }

        return recs;
    }

    /// <summary>
    /// Pattern 7: AccountRoleDistribution — whole collection lacks ships in major role categories.
    /// </summary>
    private static List<Recommendation> AnalyseAccountRoleDistribution(FleetGraph graph)
    {
        var recs = new List<Recommendation>();

        if (graph.Ships.Count == 0)
            return recs;

        var coveredRoles = new HashSet<string>();
        foreach (var ship in graph.Ships)
            foreach (var t in ship.GlobalTags.Where(t => t.Definition.Category == "role"))
                coveredRoles.Add(t.Definition.Key);

        var missingRoles = MajorRoleCategories.Where(r => !coveredRoles.Contains(r)).ToList();
        if (missingRoles.Count > 0)
        {
            var score = 0.5 * ((double)missingRoles.Count / MajorRoleCategories.Length);
            recs.Add(new Recommendation
            {
                ScopeType = RecommendationScope.Account,
                Kind = RecommendationKind.AccountRoleDistribution,
                Score = score,
                Priority = missingRoles.Count >= 4 ? RecommendationPriority.High
                    : missingRoles.Count >= 2 ? RecommendationPriority.Medium
                    : RecommendationPriority.Low,
                Summary = $"Collection missing {missingRoles.Count} major role(s): {string.Join(", ", missingRoles.Select(FormatTagKey))}",
                Explanation = "A well-rounded fleet benefits from coverage across major role categories. Consider acquiring ships to fill these gaps.",
                Evidence = missingRoles.Select(r => $"Missing: {FormatTagKey(r)}").ToList(),
                SuggestedActions = missingRoles.Select(r => $"Acquire a ship for {FormatTagKey(r)}").ToList()
            });
        }

        return recs;
    }

    /// <summary>
    /// Pattern 8: DoctrineMismatch — ship's primary doctrine tag conflicts with group's doctrine tags.
    /// </summary>
    private static List<Recommendation> AnalyseDoctrineMismatch(FleetGraph graph)
    {
        var recs = new List<Recommendation>();

        foreach (var group in graph.Groups)
        {
            var groupDoctrine = group.DoctrineAndFocusTags
                .Where(t => t.Definition.Category == "doctrine")
                .Select(t => t.Definition.Key)
                .ToHashSet();

            if (groupDoctrine.Count == 0)
                continue;

            foreach (var ship in group.MemberShips)
            {
                var shipDoctrine = GetAllTags(ship, group.Group.Id)
                    .Where(t => t.Definition.Category == "doctrine" && t.Weight == 1)
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
    /// Pattern 9: RemoveFromGroup — ship contributes nothing to a group based on group doctrine + ship tags.
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
                        SuggestedActions = [$"Remove {ship.CatalogueShip.Name} from this group", "Add relevant role/capability tags to the ship"]
                    });
                }
            }
        }

        return recs;
    }

    /// <summary>
    /// Pattern 10: CrewEfficiency — group's required crew total significantly exceeds or is under group.CrewTarget.
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

    // ── Helpers ────────────────────────────────────────────────────────

    /// <summary>Gets role-category tags for a ship in a specific group context.</summary>
    private static List<TagNode> GetRoleTags(ShipNode ship, int groupId)
    {
        var tags = new List<TagNode>();
        tags.AddRange(ship.GlobalTags.Where(t => t.Definition.Category == "role"));
        if (ship.ContextualTags.TryGetValue(groupId, out var ctxTags))
            tags.AddRange(ctxTags.Where(t => t.Definition.Category == "role"));
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

    /// <summary>Formats a tag key for display (e.g. "role:escort" → "Escort").</summary>
    private static string FormatTagKey(string key)
    {
        var parts = key.Split(':');
        if (parts.Length < 2) return key;
        return System.Globalization.CultureInfo.CurrentCulture.TextInfo
            .ToTitleCase(parts[1].Replace('-', ' '));
    }
}
