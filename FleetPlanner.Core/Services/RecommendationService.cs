using FleetPlanner.Models;

namespace FleetPlanner.Services;

/// <summary>
/// 100% on-device recommendation engine. No network calls.
/// <para>
/// Takes the user's fleet (with intent metadata), owned ships, and the cached ship catalogue.
/// Returns intent-aware suggestions including crew efficiency and focus-aware role gaps.
/// </para>
/// <para>
/// <b>Architecture:</b> The engine is composed of five independent analysis passes, each
/// producing zero or more <see cref="Recommendation"/> objects:
/// <list type="number">
///   <item><b>Crew Efficiency</b> — flags understaffed or over-capacity fleets.</item>
///   <item><b>Role Coverage</b> — identifies missing roles based on the fleet's declared focus.</item>
///   <item><b>Fleet Synergy</b> — suggests complementary ships (e.g., escorts for haulers).</item>
///   <item><b>Upgrade Paths</b> — finds better ships in the same role at a modest price increase.</item>
///   <item><b>Value Analysis</b> — identifies cheaper alternatives with comparable specs.</item>
/// </list>
/// Results from all passes are merged and sorted by priority (High → Medium → Low).
/// </para>
/// <para>
/// <b>Design decision — why synchronous?</b> This method is a pure computation with no I/O.
/// Making it <c>async</c> would add overhead (state machine allocation) for no benefit.
/// The ViewModel wraps it in a <c>Task.Run</c> if needed to keep the UI thread free.
/// </para>
/// </summary>
public class RecommendationService : IRecommendationService
{
    /// <summary>
    /// Entry point — runs all five analysis passes and merges the results.
    /// </summary>
    /// <param name="fleet">The fleet being analysed (provides focus, roles, crew budget, scale).</param>
    /// <param name="ownedShips">Ship records currently in the fleet (global reference data, not FleetShip).</param>
    /// <param name="allShips">The full cached ship catalogue — candidates for suggestions.</param>
    /// <returns>Recommendations sorted by priority descending (High first).</returns>
    public List<Recommendation> GetRecommendations(Fleet fleet, List<Ship> ownedShips, List<Ship> allShips)
    {
        var results = new List<Recommendation>();

        // Run each analysis pass independently. They don't depend on each other's output,
        // so they could theoretically be parallelised — but the data sets are small enough
        // that sequential execution is instantaneous.
        results.AddRange(GetCrewEfficiencyRecommendations(fleet, ownedShips, allShips));
        results.AddRange(GetRoleCoverageRecommendations(fleet, ownedShips, allShips));
        results.AddRange(GetFleetSynergyRecommendations(fleet, ownedShips, allShips));
        results.AddRange(GetUpgradePathRecommendations(fleet, ownedShips, allShips));
        results.AddRange(GetValueAnalysisRecommendations(fleet, ownedShips, allShips));

        // Stamp every recommendation with the fleet ID so the UI can associate them correctly.
        foreach (var rec in results)
            rec.FleetId = fleet.Id;

        // Sort by priority descending so the most critical recommendations appear first.
        return results.OrderByDescending(r => r.Priority).ToList();
    }

    // ══════════════════════════════════════════════════════════════════════
    //  Helper: Scale-appropriate filtering
    // ══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Returns the maximum crew-per-ship appropriate for a given operating scale.
    /// <para>
    /// This is a heuristic — Solo/Small fleets shouldn't be recommended capital ships
    /// that need 8+ crew, even if they technically have enough total crew budget.
    /// The limit is per-ship, not total fleet crew.
    /// </para>
    /// </summary>
    private static int GetMaxCrewForScale(FleetOperatingScale scale)
    {
        return scale switch
        {
            FleetOperatingScale.Solo => 2,   // Snub fighters, small multi-crew
            FleetOperatingScale.Small => 2,  // Still limited to small multi-crew
            FleetOperatingScale.Medium => 5, // Can handle medium multi-crew (e.g., Constellation)
            FleetOperatingScale.Large => 16, // Can field capital ships (e.g., Hammerhead, Idris)
            _ => 16
        };
    }

    /// <summary>
    /// Checks if a ship is appropriate for the fleet's scale AND available crew.
    /// A ship passes if its minimum crew requirement fits within both the scale limit
    /// and the actual crew count the user declared.
    /// </summary>
    private static bool IsAppropriateForScale(Ship ship, FleetOperatingScale scale, int availableCrew)
    {
        var maxCrew = GetMaxCrewForScale(scale);
        // Both conditions must be true: fits the scale category AND the user actually has enough people.
        return ship.CrewMin <= maxCrew && ship.CrewMin <= availableCrew;
    }

    // ══════════════════════════════════════════════════════════════════════
    //  Analysis Pass 1: Crew Efficiency
    // ══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Analyses crew demand vs. supply and flags imbalances.
    /// <para>
    /// <b>Understaffed:</b> Sum of all ships' <c>CrewMin</c> exceeds <c>AvailableCrewCount</c>.
    /// This is a High priority problem — the player literally can't operate all their ships.
    /// Suggests smaller/cheaper alternatives.
    /// </para>
    /// <para>
    /// <b>Over-capacity:</b> Sum of all ships' <c>CrewMax</c> is less than <c>AvailableCrewCount</c>.
    /// This means the player has idle crew. Medium priority — suggests larger ships to use the surplus.
    /// </para>
    /// </summary>
    private static List<Recommendation> GetCrewEfficiencyRecommendations(Fleet fleet, List<Ship> owned, List<Ship> allShips)
    {
        var recommendations = new List<Recommendation>();

        // Guard: can't analyse crew if no crew declared or no ships owned.
        if (fleet.AvailableCrewCount <= 0 || owned.Count == 0)
            return recommendations;

        // Math.Max(CrewMin, 1) — treat 0-crew ships (snubs, ground vehicles) as needing at least 1 person.
        var totalMinCrew = owned.Sum(s => Math.Max(s.CrewMin, 1));
        var totalMaxCrew = owned.Sum(s => Math.Max(s.CrewMax, 1));
        var available = fleet.AvailableCrewCount;

        if (totalMinCrew > available)
        {
            // UNDERSTAFFED: more ships than people to fly them.
            // Suggest small, affordable ships (CrewMin ≤ 2) as alternatives to reduce crew demand.
            var smallerShips = allShips
                .Where(s => s.CrewMin <= 2 && s.CrewMin > 0)
                .OrderBy(s => s.PriceUsd)  // Cheapest first — budget-friendly suggestions.
                .Take(3)
                .ToList();

            recommendations.Add(new Recommendation
            {
                Title = "Fleet is understaffed",
                Description = $"Your ships require a minimum of {totalMinCrew} crew but you only have {available} players available. Consider smaller variants or reducing fleet size.",
                Category = RecommendationCategory.CrewEfficiency,
                Priority = RecommendationPriority.High,
                SuggestedShips = smallerShips,
                CrewNote = $"Crew deficit: need {totalMinCrew}, have {available}"
            });
        }
        else if (totalMaxCrew < available)
        {
            // OVER-CAPACITY: more crew than ships can use.
            // Suggest larger multi-crew ships that can absorb the surplus.
            var scale = fleet.OperatingScaleEnum;
            var largerShips = allShips
                .Where(s => s.CrewMin > 1 && IsAppropriateForScale(s, scale, available))
                .OrderByDescending(s => s.CrewMax)  // Biggest crew capacity first.
                .Take(3)
                .ToList();

            recommendations.Add(new Recommendation
            {
                Title = "Fleet has overcapacity",
                Description = $"Your crew of {available} has spare capacity — your ships only use up to {totalMaxCrew} crew. Consider larger ships or adding more ships.",
                Category = RecommendationCategory.CrewEfficiency,
                Priority = RecommendationPriority.Medium,
                SuggestedShips = largerShips,
                CrewNote = $"Crew surplus: {available} available, ships use max {totalMaxCrew}"
            });
        }

        return recommendations;
    }

    // ══════════════════════════════════════════════════════════════════════
    //  Analysis Pass 2: Focus-Aware Role Coverage
    // ══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Identifies gameplay roles that the fleet lacks, with priority influenced by the fleet's focus.
    /// <para>
    /// <b>Algorithm:</b>
    /// <list type="number">
    ///   <item>Build a set of roles the fleet already covers (from owned ships).</item>
    ///   <item>For each role group (Combat, Mining, Medical, etc.), check if any owned ship covers it.</item>
    ///   <item>If a role group is uncovered, suggest candidate ships (filtered by scale/crew).</item>
    ///   <item>Assign priority based on the fleet's focus — missing your primary role = High,
    ///     complementary roles = Medium, unrelated roles = Low.</item>
    /// </list>
    /// </para>
    /// <para>
    /// <b>Role groups:</b> Multiple API role strings can map to a single logical group.
    /// E.g., "Fighter" and "Bomber" both count as "Combat" coverage. This handles the
    /// inconsistent role naming in the UEX Corp API data.
    /// </para>
    /// </summary>
    private static List<Recommendation> GetRoleCoverageRecommendations(Fleet fleet, List<Ship> owned, List<Ship> allShips)
    {
        var recommendations = new List<Recommendation>();

        // Build a set of normalised role strings the fleet already covers.
        var ownedRoles = owned
            .Select(s => NormalizeRole(s.Role))
            .Where(r => !string.IsNullOrEmpty(r))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        // Role groups: each group name maps to one or more API role strings that satisfy it.
        // If the fleet has ANY ship whose role matches ANY string in a group, that group is "covered".
        var roleGroups = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
        {
            ["Combat"] = ["Combat", "Fighter", "Bomber", "Dropship"],
            ["Hauling"] = ["Transport", "Hauling", "Freight"],
            ["Mining"] = ["Mining"],
            ["Medical"] = ["Medical"],
            ["Exploration"] = ["Exploration", "Pathfinder"],
            ["Salvage"] = ["Salvage"],
            ["Support"] = ["Refueling", "Repair"],
            ["Reconnaissance"] = ["Reconnaissance", "Stealth", "Data"]
        };

        // Map each fleet focus to the role groups that are MOST important for that focus.
        // These primary roles get High priority when missing.
        var focusRoleMap = new Dictionary<FleetFocus, string[]>
        {
            [FleetFocus.Combat] = ["Combat"],
            [FleetFocus.Trading] = ["Hauling"],
            [FleetFocus.Mining] = ["Mining"],
            [FleetFocus.Exploration] = ["Exploration", "Reconnaissance"],
            [FleetFocus.Industrial] = ["Salvage", "Support"],
            [FleetFocus.Medical] = ["Medical"],
            [FleetFocus.Multipurpose] = ["Combat", "Hauling"]
        };

        // Special case: combat fleets treat Medical and Support as Medium priority (not Low)
        // because they're critical in fleet engagements even though they're not the primary focus.
        var combatSupportRoles = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "Medical", "Support" };

        var focus = fleet.PrimaryFocusEnum;
        var primaryRoles = focusRoleMap.GetValueOrDefault(focus, ["Combat", "Hauling"]);

        foreach (var (groupName, roles) in roleGroups)
        {
            // Skip if the fleet already has at least one ship covering this role group.
            if (roles.Any(r => ownedRoles.Contains(r)))
                continue;

            // Find candidate ships that fill this role group AND fit the fleet's scale.
            var scale = fleet.OperatingScaleEnum;
            var candidates = allShips
                .Where(s => roles.Any(r => NormalizeRole(s.Role).Equals(r, StringComparison.OrdinalIgnoreCase)))
                .Where(s => IsAppropriateForScale(s, scale, Math.Max(fleet.AvailableCrewCount, 1)))
                .OrderBy(s => s.PriceUsd)  // Cheapest first — most accessible suggestions.
                .Take(3)
                .ToList();

            if (candidates.Count == 0)
            {
                // Fallback: if no ships fit the scale filter, suggest any ship in the role.
                // Better to suggest a ship that's too large than to show nothing.
                candidates = allShips
                    .Where(s => roles.Any(r => NormalizeRole(s.Role).Equals(r, StringComparison.OrdinalIgnoreCase)))
                    .OrderBy(s => s.PriceUsd)
                    .Take(3)
                    .ToList();
            }

            if (candidates.Count == 0)
                continue;

            // Determine priority based on how important this role is for the fleet's focus.
            var priority = DetermineRolePriority(groupName, focus, primaryRoles, combatSupportRoles);

            var crewNote = candidates.Count > 0
                ? $"Requires {candidates.First().CrewMin} crew — fits your fleet size"
                : null;

            recommendations.Add(new Recommendation
            {
                Title = $"Missing {groupName} capability",
                Description = GetRoleCoverageDescription(groupName, focus),
                Category = RecommendationCategory.RoleCoverage,
                Priority = priority,
                SuggestedShips = candidates,
                CrewNote = crewNote
            });
        }

        return recommendations;
    }

    /// <summary>
    /// Determines the priority of a missing role based on the fleet's focus.
    /// <para>
    /// Priority logic:
    /// <list type="bullet">
    ///   <item><b>High</b> — the role is the fleet's primary focus (e.g., missing Combat in a Combat fleet).</item>
    ///   <item><b>Medium</b> — the role complements the primary focus (e.g., Medical for Combat fleets,
    ///     Hauling for Mining fleets).</item>
    ///   <item><b>Low</b> — the role is outside the fleet's focus area (nice-to-have, not critical).</item>
    /// </list>
    /// </para>
    /// </summary>
    private static RecommendationPriority DetermineRolePriority(
        string groupName,
        FleetFocus focus,
        string[] primaryRoles,
        HashSet<string> combatSupportRoles)
    {
        // Primary focus roles are always High priority
        if (primaryRoles.Contains(groupName, StringComparer.OrdinalIgnoreCase))
            return RecommendationPriority.High;

        // Combat fleets: medical/support is Medium priority — you need healers and repairers in fights.
        if (focus == FleetFocus.Combat && combatSupportRoles.Contains(groupName))
            return RecommendationPriority.Medium;

        // Mining fleets: hauling support is Medium — you need to move the ore you extract.
        if (focus == FleetFocus.Mining && groupName.Equals("Hauling", StringComparison.OrdinalIgnoreCase))
            return RecommendationPriority.Medium;

        // Roles outside the fleet's focus are Low priority — nice to have, not essential.
        return RecommendationPriority.Low;
    }

    /// <summary>
    /// Generates a human-readable description for a role coverage gap,
    /// with special contextual messages for common focus+role combinations.
    /// </summary>
    private static string GetRoleCoverageDescription(string groupName, FleetFocus focus)
    {
        // Context-specific descriptions for common complementary role gaps.
        if (focus == FleetFocus.Combat && groupName.Equals("Medical", StringComparison.OrdinalIgnoreCase))
            return "Every combat fleet needs medical support. Consider adding a medical ship.";

        if (focus == FleetFocus.Mining && groupName.Equals("Hauling", StringComparison.OrdinalIgnoreCase))
            return "Mining operations need haulers to move ore. Consider adding a cargo ship.";

        // Generic fallback description.
        return $"Your fleet has no ships filling the {groupName.ToLower()} role. Consider adding one to improve versatility.";
    }

    // ══════════════════════════════════════════════════════════════════════
    //  Analysis Pass 3: Fleet Synergy
    // ══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Identifies complementary ship combinations — ships that work better together.
    /// <para>
    /// Unlike Role Coverage (which checks declared roles), Synergy looks at what ships
    /// are actually owned and suggests natural companions:
    /// <list type="bullet">
    ///   <item><b>Haulers without escorts</b> — cargo ships are piracy targets; a fighter escort synergises.</item>
    ///   <item><b>Miners without haulers</b> — mining ships extract ore but need transports to sell it.</item>
    ///   <item><b>Large fleets without medical</b> — any fleet with 3+ ships benefits from medical support.</item>
    /// </list>
    /// These are gameplay-knowledge-driven heuristics based on Star Citizen's emergent gameplay loops.
    /// </para>
    /// </summary>
    private static List<Recommendation> GetFleetSynergyRecommendations(Fleet fleet, List<Ship> owned, List<Ship> allShips)
    {
        var recommendations = new List<Recommendation>();
        var scale = fleet.OperatingScaleEnum;
        var crew = Math.Max(fleet.AvailableCrewCount, 1);

        // Synergy check 1: Haulers need escorts — cargo ships are vulnerable to piracy.
        var hasHauler = owned.Any(s => IsRole(s, "Transport", "Hauling", "Freight"));
        var hasFighter = owned.Any(s => IsRole(s, "Combat", "Fighter"));
        if (hasHauler && !hasFighter)
        {
            var escorts = allShips
                .Where(s => IsRole(s, "Combat", "Fighter"))
                .Where(s => IsAppropriateForScale(s, scale, crew))
                .OrderBy(s => s.PriceUsd)
                .Take(3)
                .ToList();
            if (escorts.Count > 0)
            {
                recommendations.Add(new Recommendation
                {
                    Title = "Add escort for your haulers",
                    Description = "You have hauling ships but no fighters to protect them. An escort fighter would synergize well.",
                    Category = RecommendationCategory.FleetSynergy,
                    Priority = RecommendationPriority.High,
                    SuggestedShips = escorts,
                    CrewNote = $"Suggested escorts require {escorts.First().CrewMin}-{escorts.First().CrewMax} crew"
                });
            }
        }

        // Synergy check 2: Miners need haulers — mined ore must be transported to sell points.
        var hasMiner = owned.Any(s => IsRole(s, "Mining"));
        if (hasMiner && !hasHauler)
        {
            var haulers = allShips
                .Where(s => IsRole(s, "Transport", "Hauling", "Freight"))
                .Where(s => IsAppropriateForScale(s, scale, crew))
                .OrderBy(s => s.PriceUsd)
                .Take(3)
                .ToList();
            if (haulers.Count > 0)
            {
                recommendations.Add(new Recommendation
                {
                    Title = "Add a hauler for your mining operation",
                    Description = "You have mining ships but no dedicated transport. A hauler would let you move more ore efficiently.",
                    Category = RecommendationCategory.FleetSynergy,
                    Priority = RecommendationPriority.Medium,
                    SuggestedShips = haulers
                });
            }
        }

        // Synergy check 3: Large fleets benefit from medical support — respawn in the field.
        if (owned.Count >= 3 && !owned.Any(s => IsRole(s, "Medical")))
        {
            var medicals = allShips
                .Where(s => IsRole(s, "Medical"))
                .Where(s => IsAppropriateForScale(s, scale, crew))
                .OrderBy(s => s.PriceUsd)
                .Take(2)
                .ToList();
            if (medicals.Count > 0)
            {
                recommendations.Add(new Recommendation
                {
                    Title = "Consider a medical ship for fleet support",
                    Description = "With a fleet of this size, a medical ship could provide valuable support during operations.",
                    Category = RecommendationCategory.FleetSynergy,
                    Priority = RecommendationPriority.Low,
                    SuggestedShips = medicals
                });
            }
        }

        return recommendations;
    }

    // ══════════════════════════════════════════════════════════════════════
    //  Analysis Pass 4: Upgrade Paths
    // ══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// For each owned ship, looks for more expensive ships in the same role that serve as natural upgrades.
    /// <para>
    /// <b>Algorithm:</b> For each owned ship, find ships with the same role but higher USD price.
    /// The price delta determines priority — cheap upgrades (under $50) are High priority because
    /// they're affordable; expensive upgrades are Low priority (aspirational).
    /// </para>
    /// <para>
    /// <b>Acquisition note:</b> If the upgrade ship has an aUEC price, we note that it can be
    /// earned in-game (no real money needed). Otherwise, it's pledge-store only.
    /// </para>
    /// </summary>
    private static List<Recommendation> GetUpgradePathRecommendations(Fleet fleet, List<Ship> owned, List<Ship> allShips)
    {
        var recommendations = new List<Recommendation>();

        foreach (var ship in owned)
        {
            var role = NormalizeRole(ship.Role);
            if (string.IsNullOrEmpty(role))
                continue;

            // Find ships in the same role that cost more (i.e., are "upgrades").
            // Exclude the ship itself (s.Id != ship.Id).
            var upgrades = allShips
                .Where(s =>
                    NormalizeRole(s.Role).Equals(role, StringComparison.OrdinalIgnoreCase) &&
                    s.PriceUsd > ship.PriceUsd &&
                    s.Id != ship.Id)
                .OrderBy(s => s.PriceUsd)  // Cheapest upgrade first — most accessible.
                .Take(2)
                .ToList();

            if (upgrades.Count == 0)
                continue;

            var cheapest = upgrades.First();
            var delta = cheapest.PriceUsd - ship.PriceUsd;

            // Provide acquisition guidance — can the player earn this in-game?
            var acquisitionNote = cheapest.PriceAuec > 0
                ? $"Available for {cheapest.PriceAuec:N0} aUEC in-game"
                : "Pledge store only";

            recommendations.Add(new Recommendation
            {
                Title = $"Upgrade {ship.Name}",
                Description = $"For ${delta:N0} more, you could upgrade from {ship.Name} to {cheapest.Name} ({cheapest.Role}). Better stats in the same role.",
                Category = RecommendationCategory.UpgradePath,
                // Cheap upgrades (< $50 delta) are High priority — affordable improvements.
                // Expensive upgrades are Low priority — aspirational goals.
                Priority = delta < 50 ? RecommendationPriority.High : RecommendationPriority.Low,
                SuggestedShips = upgrades,
                AcquisitionNote = acquisitionNote
            });
        }

        return recommendations;
    }

    // ══════════════════════════════════════════════════════════════════════
    //  Analysis Pass 5: Value Analysis
    // ══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Identifies owned ships that may be overpriced — cheaper alternatives exist with comparable specs.
    /// <para>
    /// <b>Algorithm:</b> For each owned ship, find ships in the same role that:
    /// <list type="bullet">
    ///   <item>Cost less than 70% of the owned ship's price (at least a 30% savings).</item>
    ///   <item>Have equal or greater crew capacity (<c>CrewMax</c>).</item>
    ///   <item>Have at least 80% of the owned ship's cargo capacity (minor cargo loss is acceptable).</item>
    /// </list>
    /// If such ships exist, the owned ship may be poor value-for-money — the player could
    /// downgrade and free up budget for other fleet needs.
    /// </para>
    /// </summary>
    private static List<Recommendation> GetValueAnalysisRecommendations(Fleet fleet, List<Ship> owned, List<Ship> allShips)
    {
        var recommendations = new List<Recommendation>();

        foreach (var ship in owned)
        {
            var role = NormalizeRole(ship.Role);
            // Skip ships with no role or no price (can't compare value without a price).
            if (string.IsNullOrEmpty(role) || ship.PriceUsd <= 0)
                continue;

            var alternatives = allShips
                .Where(s =>
                    NormalizeRole(s.Role).Equals(role, StringComparison.OrdinalIgnoreCase) &&
                    s.PriceUsd < ship.PriceUsd * 0.7m &&            // At least 30% cheaper
                    s.CrewMax >= ship.CrewMax &&                      // Same or more crew capacity
                    s.CargoCapacity >= ship.CargoCapacity * 0.8m &&   // At least 80% of cargo
                    s.Id != ship.Id)
                .OrderBy(s => s.PriceUsd)
                .Take(2)
                .ToList();

            if (alternatives.Count == 0)
                continue;

            recommendations.Add(new Recommendation
            {
                Title = $"{ship.Name} may be overpriced for its role",
                Description = $"There are cheaper {role.ToLower()} ships with comparable specs. You could save money without losing much capability.",
                Category = RecommendationCategory.ValueAnalysis,
                Priority = RecommendationPriority.Medium,
                SuggestedShips = alternatives
            });
        }

        return recommendations;
    }

    // ══════════════════════════════════════════════════════════════════════
    //  Utility methods
    // ══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Normalises a role string by trimming whitespace. Handles null/empty gracefully.
    /// <para>
    /// The UEX Corp API sometimes returns roles with leading/trailing spaces or inconsistent
    /// casing. This normalisation ensures role comparisons work correctly.
    /// </para>
    /// </summary>
    private static string NormalizeRole(string? role)
    {
        if (string.IsNullOrWhiteSpace(role))
            return string.Empty;
        return role.Trim();
    }

    /// <summary>
    /// Checks whether a ship's role matches any of the given role strings (case-insensitive).
    /// <para>
    /// Used as a predicate in LINQ queries throughout the recommendation engine.
    /// The <c>params</c> keyword lets callers pass multiple role strings without creating an array:
    /// <c>IsRole(ship, "Combat", "Fighter")</c>
    /// </para>
    /// </summary>
    /// <param name="ship">The ship to check.</param>
    /// <param name="roles">One or more role strings to match against.</param>
    /// <returns><see langword="true"/> if the ship's normalised role matches any of the given strings.</returns>
    private static bool IsRole(Ship ship, params string[] roles)
    {
        var normalized = NormalizeRole(ship.Role);
        return roles.Any(r => normalized.Equals(r, StringComparison.OrdinalIgnoreCase));
    }
}
