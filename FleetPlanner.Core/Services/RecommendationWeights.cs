namespace FleetPlanner.Services;

/// <summary>
/// Fixed scoring weights used by <see cref="RecommendationService"/> to produce normalised
/// <see cref="FleetPlanner.Models.Recommendation.Score"/> values (0.0–1.0 range).
///
/// <para><b>How scoring works:</b> Each analytical pattern in <see cref="RecommendationService"/>
/// assigns a base score drawn from these constants. Some patterns multiply the weight by a
/// ratio (e.g. coherence percentage, crew ratio) to produce a continuous score. The final
/// <see cref="FleetPlanner.Models.RecommendationPriority"/> is derived from the score:
/// typically ≥ 0.7 → High, 0.4–0.7 → Medium, &lt; 0.4 → Low, though individual patterns
/// may override this mapping.</para>
///
/// <para><b>Rationale for defaults:</b> Weights are ordered by operational impact.
/// <c>CapabilityGapWeight</c> (1.0) is highest because a group missing a core capability
/// is the most actionable finding. <c>UnderDescribedWeight</c> (0.4) is lowest because
/// missing tags are a data-quality issue, not a fleet composition problem.</para>
///
/// <para><b>Phase 1 (current):</b> Hard-coded constants. <b>Phase 2 (planned):</b>
/// Move to user-configurable settings so players can tune which recommendations
/// matter most to their play style.</para>
/// </summary>
internal static class RecommendationWeights
{
    /// <summary>
    /// Base score for <see cref="FleetPlanner.Models.RecommendationKind.CapabilityGap"/>.
    /// Highest weight (1.0) — a group whose doctrine requires capabilities that no member
    /// ship provides is the most critical gap to surface. Used directly as the score.
    /// </summary>
    public const double CapabilityGapWeight = 1.0;

    /// <summary>
    /// Base score for <see cref="FleetPlanner.Models.RecommendationKind.Redundancy"/>.
    /// Moderate weight (0.6) — duplicate primary roles in a group are wasteful but not
    /// harmful. Redundancy is sometimes intentional (e.g. multiple escorts for safety).
    /// Also used (×0.8) as the base for <see cref="FleetPlanner.Models.RecommendationKind.RemoveFromGroup"/>.
    /// </summary>
    public const double RedundancyWeight = 0.6;

    /// <summary>
    /// Base score for <see cref="FleetPlanner.Models.RecommendationKind.Complement"/>.
    /// High weight (0.8) — a ship that fills a missing role in a group is a strong
    /// actionable suggestion. Used directly as the score.
    /// </summary>
    public const double ComplementWeight = 0.8;

    /// <summary>
    /// Base score for <see cref="FleetPlanner.Models.RecommendationKind.UnderDescribedShip"/>.
    /// Lowest weight (0.4) — ships with fewer than 2 meaningful tags are a data-quality
    /// issue. The recommendation engine can't analyse what it doesn't know about.
    /// </summary>
    public const double UnderDescribedWeight = 0.4;

    /// <summary>
    /// Base score for <see cref="FleetPlanner.Models.RecommendationKind.UnassignedShip"/>.
    /// Moderate weight (0.5) — an unassigned ship isn't a problem per se, but organising
    /// it into a group enables better recommendations. Only triggers after 7 days in collection.
    /// </summary>
    public const double UnassignedShipWeight = 0.5;

    /// <summary>
    /// Base score for <see cref="FleetPlanner.Models.RecommendationKind.DoctrineMismatch"/>.
    /// High weight (0.9) — a ship whose primary doctrine contradicts its group's doctrine
    /// is likely miscategorised or in the wrong group. Used directly as the score.
    /// </summary>
    public const double DoctrineMismatchWeight = 0.9;

    /// <summary>
    /// Base score for <see cref="FleetPlanner.Models.RecommendationKind.CrewEfficiency"/>.
    /// Moderate-high weight (0.7) for overcrew (ratio &gt; 1.5×), reduced (×0.7 = 0.49)
    /// for undercrew. Crew mismatches directly affect whether ships can be operated.
    /// </summary>
    public const double CrewEfficiencyWeight = 0.7;

    // TODO: Phase 2 — add ConflictingTagPenalty (0.2) when tag conflict detection
    // is added to the graph builder. This penalty would reduce a recommendation's
    // score when a ship has conflicting tags at the same weight within the same category.
}
