using SQLite;

namespace FleetPlanner.Models;

/// <summary>
/// A user-created fleet that contains ships. Persisted locally in SQLite.
/// <para>
/// The Fleet is the top-level organising unit in FleetPlanner. A user can have multiple fleets,
/// each with its own focus (e.g., combat, mining), declared roles, crew budget, and operating scale.
/// Ships are linked to a fleet via <see cref="FleetShip"/> (a join/bridge entity).
/// </para>
/// <para>
/// <b>SQLite-net mapping:</b> The <c>[Table]</c> attribute maps this class to the "Fleet" table.
/// Properties decorated with <c>[PrimaryKey, AutoIncrement]</c> become the auto-generated row ID.
/// Properties decorated with <c>[NotNull]</c> add a NOT NULL constraint in the schema.
/// Properties decorated with <c>[Ignore]</c> are excluded from the table — they exist only
/// as convenience wrappers for enum conversions in application code.
/// </para>
/// </summary>
/// <see href="https://github.com/praeclarum/sqlite-net"/>
[Table("Fleet")]
public class Fleet
{
    /// <summary>
    /// Auto-incrementing primary key assigned by SQLite on insert.
    /// Zero (default) means the entity has not been persisted yet.
    /// </summary>
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    /// <summary>
    /// User-chosen display name for the fleet (e.g., "Solo Mining Fleet").
    /// Marked <c>[NotNull]</c> so SQLite rejects inserts without a name.
    /// </summary>
    [NotNull]
    public string Name { get; set; } = string.Empty;

    /// <summary>Optional free-text description of the fleet's purpose or backstory.</summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// The fleet's primary gameplay focus, stored as an <see langword="int"/> because
    /// SQLite-net does not natively serialise enums. Use <see cref="PrimaryFocusEnum"/>
    /// for typed access in application code.
    /// </summary>
    public int PrimaryFocus { get; set; }

    /// <summary>
    /// Comma-separated list of <see cref="FleetRole"/> integer values (e.g., "0,3,4").
    /// <para>
    /// SQLite-net has no built-in support for list/collection columns, so we serialise
    /// the list ourselves. Use the <see cref="DeclaredRoles"/> property for typed access —
    /// it handles parsing and formatting automatically.
    /// </para>
    /// </summary>
    public string DeclaredRolesRaw { get; set; } = string.Empty;

    /// <summary>
    /// How many crew members the fleet operator has available.
    /// The recommendation engine uses this to avoid suggesting ships the player can't crew.
    /// </summary>
    public int AvailableCrewCount { get; set; }

    /// <summary>
    /// The fleet's operating scale, stored as an <see langword="int"/>.
    /// Use <see cref="OperatingScaleEnum"/> for typed access.
    /// </summary>
    public int OperatingScale { get; set; }

    /// <summary>UTC timestamp of when this fleet was first created.</summary>
    public DateTime DateCreated { get; set; } = DateTime.UtcNow;

    /// <summary>UTC timestamp of the last modification. Updated on every save.</summary>
    public DateTime DateModified { get; set; } = DateTime.UtcNow;

    // ── Legacy fields ──────────────────────────────────────────────────
    // These columns exist in the SQLite schema from V1 of the app.
    // They are preserved so that existing user databases don't break on upgrade
    // (SQLite-net would throw if the code model didn't match the table shape).

    /// <summary>Legacy V1 field — the fleet's in-game organisation affiliation.</summary>
    public string Affiliation { get; set; } = string.Empty;

    /// <summary>Legacy V1 field — the fleet's declared area of operation (e.g., Stanton system).</summary>
    public string AreaOfOperation { get; set; } = string.Empty;

    /// <summary>Legacy V1 field — a manifesto or mission statement for the fleet.</summary>
    public string Manifesto { get; set; } = string.Empty;

    /// <summary>Legacy V1 field — general notes.</summary>
    public string Notes { get; set; } = string.Empty;

    // ── Computed / ignored properties ──────────────────────────────────
    // [Ignore] tells SQLite-net to skip these during table creation and CRUD.
    // They provide strongly-typed enum access over the raw int columns above.

    /// <summary>
    /// Strongly-typed accessor for <see cref="PrimaryFocus"/>.
    /// Casting between <see langword="int"/> and <see cref="FleetFocus"/> is safe because
    /// the enum's underlying type is <see langword="int"/> (the C# default).
    /// </summary>
    [Ignore]
    public FleetFocus PrimaryFocusEnum
    {
        get => (FleetFocus)PrimaryFocus;
        set => PrimaryFocus = (int)value;
    }

    /// <summary>
    /// Strongly-typed accessor for <see cref="OperatingScale"/>.
    /// </summary>
    [Ignore]
    public FleetOperatingScale OperatingScaleEnum
    {
        get => (FleetOperatingScale)OperatingScale;
        set => OperatingScale = (int)value;
    }

    /// <summary>
    /// Strongly-typed accessor for <see cref="DeclaredRolesRaw"/>.
    /// <para>
    /// <b>Getter:</b> Splits the comma-separated string, parses each segment to an int,
    /// then casts to <see cref="FleetRole"/>. Returns an empty list if the raw string is blank.
    /// </para>
    /// <para>
    /// <b>Setter:</b> Joins the list of enum values back into a comma-separated string of ints.
    /// </para>
    /// </summary>
    [Ignore]
    public List<FleetRole> DeclaredRoles
    {
        get
        {
            if (string.IsNullOrWhiteSpace(DeclaredRolesRaw))
                return [];
            // Split → parse each segment to int → cast to enum → materialise as List
            return DeclaredRolesRaw
                .Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(s => (FleetRole)int.Parse(s))
                .ToList();
        }
        set
        {
            // Cast each enum value to int → convert to string → join with commas
            DeclaredRolesRaw = string.Join(",", value.Select(r => ((int)r).ToString()));
        }
    }
}
