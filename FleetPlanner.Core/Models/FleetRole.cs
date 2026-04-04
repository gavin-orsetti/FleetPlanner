namespace FleetPlanner.Models;

/// <summary>
/// Specific tactical roles a fleet declares it wants to fill.
/// <para>
/// Unlike <see cref="FleetFocus"/> (which is a single high-level goal), a fleet can declare
/// multiple <see cref="FleetRole"/> values. These are stored as a comma-separated list of
/// integer values in <see cref="Fleet.DeclaredRolesRaw"/> because SQLite-net does not
/// natively support collection-type columns.
/// </para>
/// <para>
/// The recommendation engine compares declared roles against the roles already covered
/// by ships in the fleet to identify gaps and suggest ships that fill them.
/// </para>
/// </summary>
public enum FleetRole
{
    /// <summary>Frontline — primary damage dealers in fleet engagements (fighters, bombers).</summary>
    Frontline,

    /// <summary>Escort — protecting other ships during travel or operations (light fighters, interceptors).</summary>
    Escort,

    /// <summary>Support — electronic warfare, command-and-control, scanning (e.g., Herald, Terrapin).</summary>
    Support,

    /// <summary>Hauling — moving cargo between locations (e.g., Hull series, Caterpillar, Freelancer).</summary>
    Hauling,

    /// <summary>Mining — ore extraction from asteroids or ground deposits (e.g., Prospector, MOLE).</summary>
    Mining,

    /// <summary>Salvage — stripping wrecked ships for materials and components (e.g., Vulture, Reclaimer).</summary>
    Salvage,

    /// <summary>Exploration — scanning, pathfinding, and jump-point discovery (e.g., Carrack, 315p).</summary>
    Exploration,

    /// <summary>Medical — healing, respawning, and search-and-rescue (e.g., Cutlass Red, Apollo).</summary>
    Medical,

    /// <summary>Refining — processing raw ore into refined materials in the field (e.g., Expanse).</summary>
    Refining,

    /// <summary>Repair — field repair of damaged ships (e.g., Vulcan, Crucible).</summary>
    Repair,

    /// <summary>Refuel — in-field refueling of other ships (e.g., Starfarer, Vulcan).</summary>
    Refuel,

    /// <summary>Interdiction — pulling ships out of quantum travel for piracy or law enforcement (e.g., Mantis).</summary>
    Interdiction
}
