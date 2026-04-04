namespace FleetPlanner.Helpers;

/// <summary>
/// Application-wide constant values used as defaults for new fleet creation.
/// <para>
/// <b>Star Citizen context:</b> "Stanton" is the star system where most Star Citizen gameplay
/// currently takes place — it's the sensible default for new fleets.
/// </para>
/// <para>
/// These constants are legacy V1 fields carried over for backwards compatibility.
/// The V2 fleet model uses enums (<see cref="Models.FleetFocus"/>, <see cref="Models.FleetOperatingScale"/>)
/// for fleet configuration, but these string defaults are still used in some places.
/// </para>
/// </summary>
public static class Constants
{
    /// <summary>Default name for a newly created fleet before the user renames it.</summary>
    public const string DefaultFleetName = "New Fleet";

    /// <summary>Default affiliation (organisation) — "None" for solo/unaffiliated players.</summary>
    public const string DefaultAffiliation = "None";

    /// <summary>Default area of operation — the Stanton star system, Star Citizen's primary play area.</summary>
    public const string DefaultAreaOfOperation = "Stanton";
}
