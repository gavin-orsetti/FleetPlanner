namespace FleetPlanner.Models;

/// <summary>
/// The primary gameplay focus a fleet is built around.
/// <para>
/// This enum drives the recommendation engine — when a fleet declares a focus, the
/// <see cref="FleetPlanner.Services.RecommendationService"/> uses it to identify
/// which ship roles are missing or under-represented and suggests ships accordingly.
/// </para>
/// <para>
/// Star Citizen is a sandbox MMO with deep career systems. Each focus area maps to
/// distinct ship roles, gameplay loops, and crew requirements.
/// </para>
/// </summary>
public enum FleetFocus
{
    /// <summary>Combat — dogfighting, bounty hunting, fleet engagements, FPS missions.</summary>
    Combat,

    /// <summary>Trading — buying and selling commodities between locations for profit.</summary>
    Trading,

    /// <summary>Mining — extracting ore from asteroids or planetary deposits, then refining and selling.</summary>
    Mining,

    /// <summary>Exploration — pathfinding, scanning jump points, and discovering new locations.</summary>
    Exploration,

    /// <summary>Industrial — salvage, repair, refining, and other support-oriented production gameplay.</summary>
    Industrial,

    /// <summary>Medical — search-and-rescue, triage, and mobile respawn gameplay.</summary>
    Medical,

    /// <summary>Multipurpose — no single focus; the fleet is built for versatility across all activities.</summary>
    Multipurpose
}
