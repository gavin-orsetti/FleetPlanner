using FleetPlanner.Models;

namespace FleetPlanner.Services;

/// <summary>
/// 100% on-device recommendation engine. No network calls.
/// Takes the user's fleet (with intent metadata), owned ships, and the cached ship catalog.
/// Returns intent-aware suggestions including crew efficiency and focus-aware role gaps.
/// </summary>
public class RecommendationService : IRecommendationService
{
    public List<Recommendation> GetRecommendations(Fleet fleet, List<Ship> ownedShips, List<Ship> allShips)
    {
        var results = new List<Recommendation>();

        results.AddRange(GetCrewEfficiencyRecommendations(fleet, ownedShips, allShips));
        results.AddRange(GetRoleCoverageRecommendations(fleet, ownedShips, allShips));
        results.AddRange(GetFleetSynergyRecommendations(fleet, ownedShips, allShips));
        results.AddRange(GetUpgradePathRecommendations(fleet, ownedShips, allShips));
        results.AddRange(GetValueAnalysisRecommendations(fleet, ownedShips, allShips));

        foreach (var rec in results)
            rec.FleetId = fleet.Id;

        return results.OrderByDescending(r => r.Priority).ToList();
    }

    private static int GetMaxCrewForScale(FleetOperatingScale scale)
    {
        return scale switch
        {
            FleetOperatingScale.Solo => 2,
            FleetOperatingScale.Small => 2,
            FleetOperatingScale.Medium => 5,
            FleetOperatingScale.Large => 16,
            _ => 16
        };
    }

    private static bool IsAppropriateForScale(Ship ship, FleetOperatingScale scale, int availableCrew)
    {
        var maxCrew = GetMaxCrewForScale(scale);
        return ship.CrewMin <= maxCrew && ship.CrewMin <= availableCrew;
    }

    private static List<Recommendation> GetCrewEfficiencyRecommendations(Fleet fleet, List<Ship> owned, List<Ship> allShips)
    {
        var recommendations = new List<Recommendation>();
        if (fleet.AvailableCrewCount <= 0 || owned.Count == 0)
            return recommendations;

        var totalMinCrew = owned.Sum(s => Math.Max(s.CrewMin, 1));
        var totalMaxCrew = owned.Sum(s => Math.Max(s.CrewMax, 1));
        var available = fleet.AvailableCrewCount;

        if (totalMinCrew > available)
        {
            var smallerShips = allShips
                .Where(s => s.CrewMin <= 2 && s.CrewMin > 0)
                .OrderBy(s => s.PriceUsd)
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
            var scale = fleet.OperatingScaleEnum;
            var largerShips = allShips
                .Where(s => s.CrewMin > 1 && IsAppropriateForScale(s, scale, available))
                .OrderByDescending(s => s.CrewMax)
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

    private static List<Recommendation> GetRoleCoverageRecommendations(Fleet fleet, List<Ship> owned, List<Ship> allShips)
    {
        var recommendations = new List<Recommendation>();
        var ownedRoles = owned
            .Select(s => NormalizeRole(s.Role))
            .Where(r => !string.IsNullOrEmpty(r))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

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

        // Map fleet focus to prioritized role groups
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

        // Supportive roles for combat fleets
        var combatSupportRoles = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "Medical", "Support" };

        var focus = fleet.PrimaryFocusEnum;
        var primaryRoles = focusRoleMap.GetValueOrDefault(focus, ["Combat", "Hauling"]);

        foreach (var (groupName, roles) in roleGroups)
        {
            if (roles.Any(r => ownedRoles.Contains(r)))
                continue;

            var scale = fleet.OperatingScaleEnum;
            var candidates = allShips
                .Where(s => roles.Any(r => NormalizeRole(s.Role).Equals(r, StringComparison.OrdinalIgnoreCase)))
                .Where(s => IsAppropriateForScale(s, scale, Math.Max(fleet.AvailableCrewCount, 1)))
                .OrderBy(s => s.PriceUsd)
                .Take(3)
                .ToList();

            if (candidates.Count == 0)
            {
                // Fallback: suggest without scale filter
                candidates = allShips
                    .Where(s => roles.Any(r => NormalizeRole(s.Role).Equals(r, StringComparison.OrdinalIgnoreCase)))
                    .OrderBy(s => s.PriceUsd)
                    .Take(3)
                    .ToList();
            }

            if (candidates.Count == 0)
                continue;

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

    private static RecommendationPriority DetermineRolePriority(
        string groupName,
        FleetFocus focus,
        string[] primaryRoles,
        HashSet<string> combatSupportRoles)
    {
        // Primary focus roles are always High priority
        if (primaryRoles.Contains(groupName, StringComparer.OrdinalIgnoreCase))
            return RecommendationPriority.High;

        // Combat fleets: medical/support is Medium priority
        if (focus == FleetFocus.Combat && combatSupportRoles.Contains(groupName))
            return RecommendationPriority.Medium;

        // Mining fleets: hauling support is Medium
        if (focus == FleetFocus.Mining && groupName.Equals("Hauling", StringComparison.OrdinalIgnoreCase))
            return RecommendationPriority.Medium;

        // Roles outside the fleet's focus are Low priority
        return RecommendationPriority.Low;
    }

    private static string GetRoleCoverageDescription(string groupName, FleetFocus focus)
    {
        if (focus == FleetFocus.Combat && groupName.Equals("Medical", StringComparison.OrdinalIgnoreCase))
            return "Every combat fleet needs medical support. Consider adding a medical ship.";

        if (focus == FleetFocus.Mining && groupName.Equals("Hauling", StringComparison.OrdinalIgnoreCase))
            return "Mining operations need haulers to move ore. Consider adding a cargo ship.";

        return $"Your fleet has no ships filling the {groupName.ToLower()} role. Consider adding one to improve versatility.";
    }

    private static List<Recommendation> GetFleetSynergyRecommendations(Fleet fleet, List<Ship> owned, List<Ship> allShips)
    {
        var recommendations = new List<Recommendation>();
        var scale = fleet.OperatingScaleEnum;
        var crew = Math.Max(fleet.AvailableCrewCount, 1);

        // If user has haulers but no escort
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

        // If user has miners but no hauler
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

        // If fleet is large but no medical
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

    private static List<Recommendation> GetUpgradePathRecommendations(Fleet fleet, List<Ship> owned, List<Ship> allShips)
    {
        var recommendations = new List<Recommendation>();

        foreach (var ship in owned)
        {
            var role = NormalizeRole(ship.Role);
            if (string.IsNullOrEmpty(role))
                continue;

            var upgrades = allShips
                .Where(s =>
                    NormalizeRole(s.Role).Equals(role, StringComparison.OrdinalIgnoreCase) &&
                    s.PriceUsd > ship.PriceUsd &&
                    s.Id != ship.Id)
                .OrderBy(s => s.PriceUsd)
                .Take(2)
                .ToList();

            if (upgrades.Count == 0)
                continue;

            var cheapest = upgrades.First();
            var delta = cheapest.PriceUsd - ship.PriceUsd;

            var acquisitionNote = cheapest.PriceAuec > 0
                ? $"Available for {cheapest.PriceAuec:N0} aUEC in-game"
                : "Pledge store only";

            recommendations.Add(new Recommendation
            {
                Title = $"Upgrade {ship.Name}",
                Description = $"For ${delta:N0} more, you could upgrade from {ship.Name} to {cheapest.Name} ({cheapest.Role}). Better stats in the same role.",
                Category = RecommendationCategory.UpgradePath,
                Priority = delta < 50 ? RecommendationPriority.High : RecommendationPriority.Low,
                SuggestedShips = upgrades,
                AcquisitionNote = acquisitionNote
            });
        }

        return recommendations;
    }

    private static List<Recommendation> GetValueAnalysisRecommendations(Fleet fleet, List<Ship> owned, List<Ship> allShips)
    {
        var recommendations = new List<Recommendation>();

        foreach (var ship in owned)
        {
            var role = NormalizeRole(ship.Role);
            if (string.IsNullOrEmpty(role) || ship.PriceUsd <= 0)
                continue;

            var alternatives = allShips
                .Where(s =>
                    NormalizeRole(s.Role).Equals(role, StringComparison.OrdinalIgnoreCase) &&
                    s.PriceUsd < ship.PriceUsd * 0.7m &&
                    s.CrewMax >= ship.CrewMax &&
                    s.CargoCapacity >= ship.CargoCapacity * 0.8m &&
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

    private static string NormalizeRole(string? role)
    {
        if (string.IsNullOrWhiteSpace(role))
            return string.Empty;
        return role.Trim();
    }

    private static bool IsRole(Ship ship, params string[] roles)
    {
        var normalized = NormalizeRole(ship.Role);
        return roles.Any(r => normalized.Equals(r, StringComparison.OrdinalIgnoreCase));
    }
}
