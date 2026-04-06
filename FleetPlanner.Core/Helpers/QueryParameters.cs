namespace FleetPlanner.Helpers;

/// <summary>
/// Compile-safe constants for Shell navigation query parameter keys.
/// Using constants instead of inline strings means a typo causes a
/// compile error rather than a silent runtime bug where the receiving
/// ViewModel gets null/default instead of the intended value.
/// </summary>
public static class QueryParameters
{
    /// <summary>The integer Id of the ship being viewed (catalogue ship).</summary>
    public const string ShipId = "shipId";

    /// <summary>The integer Id of the OwnedShip being edited.</summary>
    public const string OwnedShipId = "ownedShipId";

    /// <summary>The integer Id of the group being viewed or edited.</summary>
    public const string GroupId = "groupId";
}
