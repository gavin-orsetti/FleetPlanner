using FleetPlanner.Graph;
using FleetPlanner.Models;

namespace FleetPlanner.Services;

/// <summary>
/// Graph-driven recommendation engine implementing 10 analytical patterns that analyse
/// the user's fleet composition and produce actionable <see cref="Recommendation"/> objects.
///
/// <para><b>Architecture:</b> Sits in the services layer, operating entirely on the
/// in-memory <see cref="FleetGraph"/> — no I/O, no database calls. The graph must be
/// pre-built by <see cref="IGraphBuildService"/> before calling <see cref="GetRecommendations"/>.
/// This service is stateless and safe to call from any thread.</para>
///
/// <para><b>Scoring pipeline:</b> Each of the 10 patterns independently generates zero or
/// more <see cref="Recommendation"/> objects. Each recommendation receives a
/// <see cref="Recommendation.Score"/> in the 0.0–1.0 range (drawn from
/// <see cref="RecommendationWeights"/> constants, sometimes scaled by a ratio). The
/// <see cref="Recommendation.Priority"/> is derived from the score — typically ≥ 0.7 → High,
/// 0.4–0.7 → Medium, &lt; 0.4 → Low — though some patterns use fixed priorities.
/// Results are sorted by score descending so the most actionable items appear first.</para>
///
/// <para><b>The 10 analytical patterns:</b>
/// <list type="number">
///   <item><b>CapabilityGap</b> — group doctrine implies capabilities that no member ship provides.</item>
///   <item><b>Redundancy</b> — multiple ships in a group share the same primary role tag.</item>
///   <item><b>Complement</b> — an unassigned ship's roles would fill a gap in a group.</item>
///   <item><b>UnderDescribedShip</b> — a ship has fewer than 2 meaningful tags.</item>
///   <item><b>UnassignedShip</b> — a ship has been in the collection &gt; 7 days with no group.</item>
///   <item><b>GroupCoherence</b> — ship role tags in a group align poorly with group doctrine.</item>
///   <item><b>AccountRoleDistribution</b> — the entire collection is missing major role categories.</item>
///   <item><b>DoctrineMismatch</b> — a ship's primary doctrine contradicts its group's doctrine.</item>
///   <item><b>RemoveFromGroup</b> — a ship's tags don't align with any group doctrine capability.</item>
///   <item><b>CrewEfficiency</b> — group crew requirements significantly exceed or underutilise the target.</item>
/// </list></para>
///
/// <para><b>Edge case handling:</b> All patterns guard against empty collections —
/// <c>graph.Ships.Count == 0</c> or <c>group.MemberShips.Count == 0</c> causes the
/// pattern to return an empty list, never throw.</para>
/// </summary>
public class RecommendationService : IRecommendationService
{
    /// <summary>
    /// The 8 role tag keys that define a "well-rounded" fleet for
    /// <see cref="AnalyseAccountRoleDistribution"/>. Missing any of these triggers a gap recommendation.
    /// </summary>
    private static readonly string[] MajorRoleCategories =
    [
        "role:escort", "role:frontline", "role:hauling", "role:mining",
        "role:salvage", "role:exploration", "role:medical", "role:repair"
    ];

    /// <summary>
    /// Maps each doctrine tag key to the role/capability tag keys that doctrine implies.
    /// Used by <see cref="AnalyseCapabilityGaps"/>, <see cref="AnalyseGroupCoherence"/>,
    /// and <see cref="AnalyseRemoveFromGroup"/> to determine what a group "needs".
    /// Doctrines not in this dictionary (e.g. "doctrine:solo", "doctrine:multipurpose") have
    /// no specific capability requirements and are silently skipped by those patterns.
    /// </summary>
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
    /// Pattern 1: CapabilityGap — a group's doctrine implies capabilities that no member ship provides.
    /// <para><b>Trigger:</b> For each group, for each doctrine tag on the group, look up the
    /// expected role/capability tags in <see cref="DoctrineCapabilities"/>. If any expected tag
    /// is absent from all member ships (checking both global and contextual tags for that group),
    /// a CapabilityGap recommendation is generated.</para>
    /// <para><b>Score:</b> Fixed at <see cref="RecommendationWeights.CapabilityGapWeight"/> (1.0).
    /// Priority: always <see cref="RecommendationPriority.High"/>.</para>
    /// <para><b>Evidence:</b> One entry per missing tag key (e.g. "Missing: Mining").</para>
    /// <para><b>Suggested actions:</b> "Add a ship with the [missing role] tag to this group".</para>
    /// <para><b>Edge cases:</b> Doctrines not in <see cref="DoctrineCapabilities"/> (e.g.
    /// "doctrine:solo", "doctrine:multipurpose") are silently skipped — they have no specific
    /// capability requirements. Groups with no member ships generate gaps for all capabilities.</para>
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
    /// Pattern 2: Redundancy — multiple ships in a group share the same primary (weight 1) role tag.
    /// <para><b>Trigger:</b> For each group, collect all role-category tags with weight == 1 from
    /// each member ship (global + contextual). If two or more ships share the same primary role,
    /// a Redundancy recommendation is generated for that role.</para>
    /// <para><b>Score:</b> Fixed at <see cref="RecommendationWeights.RedundancyWeight"/> (0.6).
    /// Priority: Medium if score ≥ 0.7, else Low (in practice always Low at 0.6).</para>
    /// <para><b>Evidence:</b> One entry per ship listing its name and the shared role.</para>
    /// <para><b>Suggested actions:</b> "Reassign one ship's primary role or move it to a different group".</para>
    /// <para><b>Limitation:</b> Intentional redundancy (e.g. multiple escorts for safety) cannot
    /// be distinguished from accidental duplication. The recommendation is always generated.</para>
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
    /// Pattern 3: Complement — a ship not yet in a group has role tags that would fill a gap in that group.
    /// <para><b>Trigger:</b> For each group, collect all role tags from member ships. Then for each
    /// ship NOT already in the group, check if any of its global role tags are absent from the group's
    /// role coverage. If so, generate a Complement recommendation suggesting the ship be added.</para>
    /// <para><b>Score:</b> Fixed at <see cref="RecommendationWeights.ComplementWeight"/> (0.8).
    /// Priority: always <see cref="RecommendationPriority.Medium"/>.</para>
    /// <para><b>Evidence:</b> One entry per complementary role (e.g. "Missing in group: Mining").</para>
    /// <para><b>Suggested actions:</b> "Add [ship name] to group '[group name]'".</para>
    /// <para><b>Limitation:</b> Only checks global tags on candidate ships, not contextual tags
    /// from other groups. A ship already in group A could still be recommended for group B
    /// (which may be desirable — ships can belong to multiple groups via contextual tags).</para>
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
    /// Pattern 4: UnderDescribedShip — an owned ship has fewer than 2 meaningful global tags.
    /// <para><b>Trigger:</b> Count global tags on the ship excluding any with category "acquisition".
    /// If the count is 0 or 1, generate an UnderDescribedShip recommendation.</para>
    /// <para><b>Score:</b> Fixed at <see cref="RecommendationWeights.UnderDescribedWeight"/> (0.4).
    /// Priority: always <see cref="RecommendationPriority.Low"/>.</para>
    /// <para><b>Evidence:</b> "Current tags: {count}".</para>
    /// <para><b>Suggested actions:</b> "Add role tags", "Add crew tags", "Add doctrine tags".</para>
    /// <para><b>Edge case:</b> A ship with zero global tags but many contextual tags is still
    /// flagged, because global tags are what the account-level patterns analyse.</para>
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
    /// Pattern 5: UnassignedShip — an owned ship has been in the collection for over 7 days
    /// but is not a member of any fleet group.
    /// <para><b>Trigger:</b> For each ship, check if <c>CreatedUtc</c> is more than 7 days ago
    /// AND the ship has no contextual tags in any group (i.e. not a member of any group).
    /// New ships (≤ 7 days old) are excluded to give the user time to organise them.</para>
    /// <para><b>Score:</b> Fixed at <see cref="RecommendationWeights.UnassignedShipWeight"/> (0.5).
    /// Priority: always <see cref="RecommendationPriority.Low"/>.</para>
    /// <para><b>Evidence:</b> "Added: {yyyy-MM-dd}".</para>
    /// <para><b>Suggested actions:</b> "Assign this ship to a fleet group with matching doctrine".</para>
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
    /// Pattern 6: GroupCoherence — fewer than 50% of a group's ships have role/capability tags
    /// that align with the group's doctrine.
    /// <para><b>Trigger:</b> For each group with at least one member ship and one doctrine tag,
    /// look up the doctrine's expected roles in <see cref="DoctrineCapabilities"/>. Count how many
    /// member ships have at least one matching tag. If fewer than 50% align, generate a
    /// GroupCoherence recommendation.</para>
    /// <para><b>Score:</b> <c>(1.0 - coherenceRatio) × CapabilityGapWeight × 0.8</c>. A group
    /// with 0% alignment scores 0.8; a group with 49% scores ~0.41. Priority: High if ≥ 0.7,
    /// else Medium.</para>
    /// <para><b>Evidence:</b> "{aligned}/{total} ships aligned".</para>
    /// <para><b>Suggested actions:</b> "Add ships that match the group's doctrine",
    /// "Reassign misaligned ships to a different group".</para>
    /// <para><b>Edge case:</b> Doctrines not in <see cref="DoctrineCapabilities"/> are skipped.
    /// Groups with 0 members or 0 doctrine tags are skipped entirely.</para>
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
    /// Pattern 7: AccountRoleDistribution — the user's entire collection is missing one or more
    /// of the 8 major role categories defined in <see cref="MajorRoleCategories"/>.
    /// <para><b>Trigger:</b> Collect all global role tags across all owned ships. Compare against
    /// the 8 major roles (escort, frontline, hauling, mining, salvage, exploration, medical, repair).
    /// If any are missing, generate a single Account-scoped recommendation listing all gaps.</para>
    /// <para><b>Score:</b> <c>0.5 × (missingCount / totalMajorRoles)</c>. With 8 major roles,
    /// missing 4 = score 0.25, missing all 8 = score 0.5. Priority: High if ≥ 4 missing,
    /// Medium if ≥ 2, Low otherwise.</para>
    /// <para><b>Evidence:</b> One "Missing: {role}" entry per gap.</para>
    /// <para><b>Suggested actions:</b> One "Acquire a ship for {role}" per gap.</para>
    /// <para><b>Edge case:</b> Returns empty if the user has no ships at all (early exit guard).
    /// Only checks global tags — contextual group-scoped role tags are not counted.</para>
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
    /// Pattern 8: DoctrineMismatch — a ship's primary (weight 1) doctrine tag is not among its
    /// group's doctrine tags, indicating the ship may be in the wrong group.
    /// <para><b>Trigger:</b> For each group with doctrine tags, for each member ship, find the
    /// ship's weight-1 doctrine tags (global + contextual). If any ship doctrine tag is NOT in
    /// the group's doctrine set, generate a DoctrineMismatch recommendation.</para>
    /// <para><b>Score:</b> Fixed at <see cref="RecommendationWeights.DoctrineMismatchWeight"/> (0.9).
    /// Priority: always <see cref="RecommendationPriority.High"/>.</para>
    /// <para><b>Evidence:</b> "Ship doctrine: {name}", "Group doctrine: {names}".</para>
    /// <para><b>Suggested actions:</b> "Move this ship to a group with matching doctrine",
    /// "Change the ship's doctrine tag to match the group".</para>
    /// <para><b>Edge case:</b> A ship with no doctrine tags generates no mismatch. Groups
    /// with no doctrine tags are skipped entirely.</para>
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
    /// Pattern 9: RemoveFromGroup — a member ship's tags don't match any of the group's
    /// doctrine-derived capability requirements, suggesting it doesn't contribute to the group.
    /// <para><b>Trigger:</b> For each group with doctrine tags, derive the set of desired
    /// role/capability tags from <see cref="DoctrineCapabilities"/>. For each member ship,
    /// check if any of its tags (global + contextual) match the desired set. If none match,
    /// generate a RemoveFromGroup recommendation.</para>
    /// <para><b>Score:</b> <c>RedundancyWeight × 0.8</c> = 0.48. Priority: always
    /// <see cref="RecommendationPriority.Low"/>.</para>
    /// <para><b>Evidence:</b> "Ship tags: {list}", "Group needs: {list}".</para>
    /// <para><b>Suggested actions:</b> "Remove [ship] from this group",
    /// "Add relevant role/capability tags to the ship".</para>
    /// <para><b>Edge case:</b> Groups with no doctrine tags or doctrines not in
    /// <see cref="DoctrineCapabilities"/> are skipped. If the derived desired set is empty
    /// (all doctrines are unmapped), the group is skipped.</para>
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
    /// Pattern 10: CrewEfficiency — the total minimum crew required to operate all ships in a
    /// group significantly exceeds or underutilises the group's <see cref="UserFleetGroup.CrewTarget"/>.
    /// <para><b>Trigger (overcrew):</b> If <c>totalMinCrew / crewTarget &gt; 1.5</c>, the group
    /// needs more crew than available — some ships will be unmanned. Generates a Medium priority
    /// recommendation with score = <see cref="RecommendationWeights.CrewEfficiencyWeight"/> (0.7).</para>
    /// <para><b>Trigger (undercrew):</b> If <c>totalMinCrew / crewTarget &lt; 0.3</c> AND
    /// <c>crewTarget ≥ 3</c>, the group is wasting available crew. Generates a Low priority
    /// recommendation with score = CrewEfficiencyWeight × 0.7 = 0.49. The crewTarget ≥ 3 guard
    /// prevents false positives for solo players.</para>
    /// <para><b>Evidence:</b> "Min crew needed: {n}", "Crew target: {n}", "Ratio: {n}x".</para>
    /// <para><b>Suggested actions (overcrew):</b> "Remove ships with high crew requirements",
    /// "Increase the crew target", "Replace multi-crew ships with solo-operable alternatives".</para>
    /// <para><b>Suggested actions (undercrew):</b> "Add multi-crew ships to better utilise
    /// available crew", "Reduce crew target if players aren't available".</para>
    /// <para><b>Edge case:</b> Groups with 0 members or crewTarget ≤ 0 are skipped.
    /// Ships with CrewMin = 0 (unusual) contribute nothing to the sum.</para>
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
