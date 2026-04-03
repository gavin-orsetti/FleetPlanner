using FleetPlanner.Models;

namespace FleetPlanner.Services;

/// <summary>
/// 100% on-device recommendation engine. No network calls.
/// Takes the user's fleet and the cached ship catalog, returns suggestions.
/// Registered as Singleton (stateless computation).
/// </summary>
public class RecommendationService : IRecommendationService
{
    private static readonly string[] CoreRoles =
    [
        "Combat", "Fighter", "Bomber",
        "Transport", "Hauling", "Freight",
        "Mining",
        "Medical",
        "Exploration",
        "Salvage",
        "Refueling",
        "Repair",
        "Reconnaissance", "Pathfinder",
        "Racing",
        "Touring",
        "Ground Vehicle",
        "Dropship",
        "Science",
        "Data",
        "Stealth"
    ];

    public List<Recommendation> GetRecommendations(List<Ship> ownedShips, List<Ship> allShips)
    {
        var results = new List<Recommendation>();

        results.AddRange(GetRoleCoverageRecommendations(ownedShips, allShips));
        results.AddRange(GetFleetSynergyRecommendations(ownedShips, allShips));
        results.AddRange(GetUpgradePathRecommendations(ownedShips, allShips));
        results.AddRange(GetValueAnalysisRecommendations(ownedShips, allShips));

        return results.OrderByDescending(r => r.Priority).ToList();
    }

    private static List<Recommendation> GetRoleCoverageRecommendations(List<Ship> owned, List<Ship> allShips)
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

        foreach (var (groupName, roles) in roleGroups)
        {
            if (roles.Any(r => ownedRoles.Contains(r)))
                continue;

            var candidates = allShips
                .Where(s => roles.Any(r => NormalizeRole(s.Role).Equals(r, StringComparison.OrdinalIgnoreCase)))
                .OrderBy(s => s.PriceUsd)
                .Take(3)
                .ToList();

            if (candidates.Count == 0)
                continue;

            recommendations.Add(new Recommendation
            {
                Title = $"Missing {groupName} capability",
                Description = $"Your fleet has no ships filling the {groupName.ToLower()} role. Consider adding one to improve versatility.",
                Category = RecommendationCategory.RoleCoverage,
                Priority = groupName is "Combat" or "Hauling" ? RecommendationPriority.High : RecommendationPriority.Medium,
                SuggestedShips = candidates
            });
        }

        return recommendations;
    }

    private static List<Recommendation> GetFleetSynergyRecommendations(List<Ship> owned, List<Ship> allShips)
    {
        var recommendations = new List<Recommendation>();

        // If user has haulers but no escort
        var hasHauler = owned.Any(s => IsRole(s, "Transport", "Hauling", "Freight"));
        var hasFighter = owned.Any(s => IsRole(s, "Combat", "Fighter"));
        if (hasHauler && !hasFighter)
        {
            var escorts = allShips
                .Where(s => IsRole(s, "Combat", "Fighter"))
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
                    SuggestedShips = escorts
                });
            }
        }

        // If user has miners but no hauler
        var hasMiner = owned.Any(s => IsRole(s, "Mining"));
        if (hasMiner && !hasHauler)
        {
            var haulers = allShips
                .Where(s => IsRole(s, "Transport", "Hauling", "Freight"))
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

    private static List<Recommendation> GetUpgradePathRecommendations(List<Ship> owned, List<Ship> allShips)
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

            recommendations.Add(new Recommendation
            {
                Title = $"Upgrade {ship.Name}",
                Description = $"For ${delta:N0} more, you could upgrade from {ship.Name} to {cheapest.Name} ({cheapest.Role}). Better stats in the same role.",
                Category = RecommendationCategory.UpgradePath,
                Priority = delta < 50 ? RecommendationPriority.High : RecommendationPriority.Low,
                SuggestedShips = upgrades
            });
        }

        return recommendations;
    }

    private static List<Recommendation> GetValueAnalysisRecommendations(List<Ship> owned, List<Ship> allShips)
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
