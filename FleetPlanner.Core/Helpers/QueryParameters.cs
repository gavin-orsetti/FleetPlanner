namespace FleetPlanner.Helpers;

/// <summary>
/// Compile-safe constants for Shell navigation query parameter keys.
/// Using constants instead of inline strings means a typo causes a
/// compile error rather than a silent runtime bug where the receiving
/// ViewModel gets null/default instead of the intended value.
///
/// Usage:
/// <code>
/// await Shell.Current.GoToAsync(nameof(FleetDetailPage), new Dictionary&lt;string, object&gt;
/// {
///     { QueryParameters.FleetId, fleet.Id }
/// });
/// </code>
/// </summary>
public static class QueryParameters
{
    /// <summary>The integer Id of the fleet being viewed or edited.</summary>
    public const string FleetId = "fleetId";

    /// <summary>The integer Id of the ship being viewed.</summary>
    public const string ShipId = "shipId";

    /// <summary>
    /// Boolean flag passed to ShipBrowserPage to enable "select mode",
    /// where tapping a ship adds it to a fleet rather than viewing its detail.
    /// </summary>
    public const string SelectMode = "selectMode";
}
