namespace FleetPlanner.Models;

public enum RecommendationPriority
{
    Low,
    Medium,
    High
}

public enum RecommendationCategory
{
    RoleCoverage,
    FleetSynergy,
    UpgradePath,
    ValueAnalysis
}

/// <summary>
/// A recommendation generated on-device by the RecommendationService.
/// </summary>
public class Recommendation
{
    public string Title { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public RecommendationCategory Category { get; set; }

    public RecommendationPriority Priority { get; set; }

    public List<Ship> SuggestedShips { get; set; } = [];
}
