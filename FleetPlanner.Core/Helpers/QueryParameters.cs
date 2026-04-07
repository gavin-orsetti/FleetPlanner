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

    /// <summary>OwnedShip Id passed to the tag picker (nullable — omit for group-only editing).</summary>
    public const string TagPickerOwnedShipId = "tagPickerOwnedShipId";

    /// <summary>Group Id passed to the tag picker (nullable — omit for global ship tag editing).</summary>
    public const string TagPickerGroupId = "tagPickerGroupId";

    /// <summary>Context type for contextual tag editing (e.g. "group"). Null = global scope.</summary>
    public const string TagPickerContextType = "tagPickerContextType";

    /// <summary>Context Id for contextual tag editing (e.g. the group Id). Null = global scope.</summary>
    public const string TagPickerContextId = "tagPickerContextId";
}
