namespace FleetPlanner.Services;

/// <summary>
/// Fixed scoring weights for the recommendation engine.
/// Phase 1: hard-coded. Phase 2: move to user settings.
/// </summary>
internal static class RecommendationWeights
{
    /// <summary>Weight for capability gap recommendations (group missing expected capability).</summary>
    public const double CapabilityGapWeight = 1.0;

    /// <summary>Weight for redundancy recommendations (duplicate primary roles).</summary>
    public const double RedundancyWeight = 0.6;

    /// <summary>Weight for complement recommendations (ship would strengthen a group).</summary>
    public const double ComplementWeight = 0.8;

    /// <summary>Weight for under-described ship recommendations (too few tags).</summary>
    public const double UnderDescribedWeight = 0.4;

    /// <summary>Weight for unassigned ship recommendations (not in any group).</summary>
    public const double UnassignedShipWeight = 0.5;

    /// <summary>Weight for doctrine mismatch recommendations.</summary>
    public const double DoctrineMismatchWeight = 0.9;

    /// <summary>Weight for crew efficiency recommendations.</summary>
    public const double CrewEfficiencyWeight = 0.7;

    /// <summary>Penalty subtracted from score when same-weight conflicting tags exist.</summary>
    public const double ConflictingTagPenalty = 0.2;
}
