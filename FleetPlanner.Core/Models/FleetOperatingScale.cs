namespace FleetPlanner.Models;

/// <summary>
/// The expected crew size a fleet operates with.
/// <para>
/// Operating scale directly influences ship recommendations — the recommendation engine
/// filters out ships that require more crew than the fleet has available. For example,
/// a Solo player shouldn't be recommended a Hammerhead (which needs 6+ crew to operate effectively).
/// </para>
/// <para>
/// These tiers roughly correspond to Star Citizen's org (guild) sizes and the crew
/// requirements of progressively larger multi-crew ships.
/// </para>
/// </summary>
public enum FleetOperatingScale
{
    /// <summary>Solo — single player, limited to ships operable by one person.</summary>
    Solo,

    /// <summary>Small group — 2-5 crew members, can operate most medium multi-crew ships.</summary>
    Small,

    /// <summary>Medium group — 6-15 crew members, can staff capital-class ships and run coordinated operations.</summary>
    Medium,

    /// <summary>Large organization — 16+ crew members, can field multiple capital ships simultaneously.</summary>
    Large
}
