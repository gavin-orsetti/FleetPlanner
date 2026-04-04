namespace FleetPlanner.Models;

/// <summary>
/// Describes how a ship was acquired by the player.
/// <para>
/// In Star Citizen, ships can be obtained either through gameplay (earning in-game currency called aUEC)
/// or through the real-money RSI pledge store. Tracking this distinction matters for fleet planning
/// because pledge-store ships persist through database wipes ("resets"), while aUEC-purchased ships do not.
/// </para>
/// </summary>
/// <see href="https://robertsspaceindustries.com/store/pledge" />
public enum AcquisitionType
{
    /// <summary>
    /// Bought with in-game currency (Alpha UEC). These ships are lost on database resets
    /// during the current alpha/beta phase of Star Citizen.
    /// </summary>
    AUEC,

    /// <summary>
    /// Bought with real money through the RSI pledge store. These ships persist through
    /// database wipes and are permanently tied to the player's account.
    /// </summary>
    RealMoney
}
