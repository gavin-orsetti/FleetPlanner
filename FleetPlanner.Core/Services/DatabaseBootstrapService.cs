using FleetPlanner.Models;

using SQLite;

namespace FleetPlanner.Services;

/// <summary>
/// Creates all SQLite tables and seeds the system-defined tag taxonomy on first launch.
/// This is the app's schema-and-seed bootstrapper — called once at startup from
/// <see cref="MauiProgram.CreateMauiApp"/> after <c>builder.Build()</c> but before the
/// <see cref="MauiApp"/> is returned.
///
/// <para><b>Schema versioning strategy:</b> An <see cref="AppMetadata"/> row with key
/// <c>"schema_version"</c> gates whether seeding runs. On first launch the row does not
/// exist, so <see cref="InitialiseAsync"/> seeds all system tags and writes version "2".
/// On subsequent launches the row is found immediately and the method returns — this is
/// the idempotency guard. Future schema migrations can bump the version and add migration
/// logic between the version check and the version write.</para>
///
/// <para><b>Idempotency:</b> Safe to call on every startup. <c>CreateTableAsync</c> is a
/// no-op if the table already exists (SQLite <c>CREATE TABLE IF NOT EXISTS</c>). Seeding
/// only runs when <c>schema_version</c> is absent. Even if seeding did run twice, tags use
/// <c>InsertOrReplaceAsync</c> keyed on the stable <see cref="TagDefinition.Key"/>, so
/// duplicates are impossible.</para>
///
/// <para><b>Seeding strategy — 8 tag categories:</b> The taxonomy seeds 40+ tags across:
/// <list type="bullet">
///   <item><b>role</b> — ship role classification (escort, mining, medical, etc.)</item>
///   <item><b>doctrine</b> — operational philosophy (solo, combat, industrial, etc.)</item>
///   <item><b>status</b> — ship lifecycle state (core, situational, upgrade-target, etc.)</item>
///   <item><b>crew</b> — crewing pattern (solo, duo, small, large, NPC-viable)</item>
///   <item><b>capability</b> — ship hardware features (medical bay, tractor beam, hangar, etc.)</item>
///   <item><b>preference</b> — player sentiment (daily-driver, favorite, lore-pick, investment)</item>
///   <item><b>constraint</b> — planning constraints (soloable, budget, hangar-limited)</item>
///   <item><b>custom</b> — user-created tags (not seeded; created at runtime)</item>
/// </list></para>
///
/// <para><b>Why stable slug keys instead of integer IDs:</b> System tags use string keys
/// in <c>"category:slug"</c> format (e.g. <c>"role:escort"</c>) rather than auto-increment
/// integers. This ensures keys are stable across installs, schema migrations, and database
/// resets — a tag assignment saved as <c>"role:escort"</c> is always meaningful, even if
/// the database was recreated. Integer IDs would depend on insertion order and could
/// silently change meaning after a migration.</para>
/// </summary>
public class DatabaseBootstrapService
{
    private readonly string _dbPath;

    /// <summary>Parameterless constructor for DI — resolves the platform-appropriate DB path.</summary>
    public DatabaseBootstrapService()
    {
#if ANDROID || IOS || MACCATALYST || WINDOWS
        _dbPath = Path.Combine(FileSystem.AppDataDirectory, "FleetPlanner.db3");
#else
        _dbPath = Path.Combine(Path.GetTempPath(), "FleetPlanner.db3");
#endif
    }

    /// <summary>Constructor for testing with a custom database path.</summary>
    public DatabaseBootstrapService(string dbPath)
    {
        _dbPath = dbPath;
    }

    /// <summary>
    /// Creates all SQLite tables and seeds the system tag taxonomy if not already present.
    /// <para>
    /// <b>Idempotent:</b> Safe to call on every startup. The <c>schema_version</c> guard in
    /// <see cref="AppMetadata"/> prevents duplicate seeding — if the row exists, this method
    /// returns immediately after ensuring tables exist. Table creation itself is idempotent
    /// (SQLite <c>CREATE TABLE IF NOT EXISTS</c>).
    /// </para>
    /// <para>
    /// <b>Call site:</b> Invoked synchronously (via <c>.GetAwaiter().GetResult()</c>) from
    /// <see cref="MauiProgram.CreateMauiApp"/> because the MAUI startup contract requires a
    /// synchronous return. The blocking call is safe here because no UI thread exists yet.
    /// </para>
    /// </summary>
    public async Task InitialiseAsync()
    {
        var db = new SQLiteAsyncConnection(_dbPath, SQLiteOpenFlags.ReadWrite | SQLiteOpenFlags.Create | SQLiteOpenFlags.SharedCache);

        // Create all tables
        await db.CreateTableAsync<OwnedShip>();
        await db.CreateTableAsync<TagDefinition>();
        await db.CreateTableAsync<OwnedShipTag>();
        await db.CreateTableAsync<UserFleetGroup>();
        await db.CreateTableAsync<UserFleetGroupTag>();
        await db.CreateTableAsync<AppMetadata>();
        await db.CreateTableAsync<ShipCacheMetadata>();

        // Check schema version — seed only if not already done
        var versionRecord = await db.FindAsync<AppMetadata>("schema_version");
        if (versionRecord is not null)
            return;

        // Seed system tags
        await SeedTagsAsync(db);

        // Record schema version
        await db.InsertOrReplaceAsync(new AppMetadata
        {
            Key = "schema_version",
            Value = "2",
            UpdatedUtc = DateTime.UtcNow
        });
    }

    private static async Task SeedTagsAsync(SQLiteAsyncConnection db)
    {
        var tags = BuildSystemTags();
        foreach (var tag in tags)
            await db.InsertOrReplaceAsync(tag);
    }

    /// <summary>
    /// Builds the complete list of system-defined tags that form the seeded taxonomy.
    /// <para>
    /// Each tag uses a stable <c>"category:slug"</c> key that NEVER changes once shipped.
    /// The slug is a lowercase, hyphenated identifier (e.g. <c>"role:frontline"</c>,
    /// <c>"doctrine:small-crew"</c>). Keys are the primary key in SQLite — renaming a slug
    /// would orphan all existing tag assignments.
    /// </para>
    /// <para>
    /// <b>AllowedScopes:</b> Each tag's <see cref="TagDefinition.AllowedScopes"/> controls
    /// which entity types it can be applied to. Most role/status/crew/capability/preference
    /// tags are scoped to <c>"OwnedShip"</c> only. Doctrine and constraint tags allow both
    /// <c>"OwnedShip,UserFleetGroup"</c> so they can describe both individual ships and
    /// fleet groups.
    /// </para>
    /// <para>
    /// <b>IsSystemDefined:</b> All tags created here have <c>IsSystemDefined = true</c>.
    /// System tags can be archived (hidden from pickers) but never hard-deleted by the user.
    /// This protects the recommendation engine's tag key references from breaking.
    /// </para>
    /// </summary>
    /// <returns>A list of 40+ <see cref="TagDefinition"/> records spanning 7 categories
    /// (role, doctrine, status, crew, capability, preference, constraint).</returns>
    internal static List<TagDefinition> BuildSystemTags()
    {
        var sortOrder = 0;
        var tags = new List<TagDefinition>();

        // ── Role tags ──────────────────────────────────────────────────
        void AddRole(string slug, string displayName, string description)
        {
            tags.Add(new TagDefinition
            {
                Key = $"role:{slug}",
                DisplayName = displayName,
                Category = "role",
                Description = description,
                ColorHex = "#ef4444",
                SortOrder = sortOrder++,
                IsSystemDefined = true,
                IsUserEditable = false,
                AllowedScopes = "OwnedShip"
            });
        }

        AddRole("escort", "Escort", "Protecting other ships in the fleet");
        AddRole("frontline", "Frontline Combat", "Direct combat engagement");
        AddRole("interdiction", "Interdiction", "Preventing enemy escape or approach");
        AddRole("bomber", "Bomber", "Heavy strike against large targets");
        AddRole("hauling", "Hauling", "Transporting cargo");
        AddRole("mining", "Mining", "Resource extraction");
        AddRole("salvage", "Salvage", "Recovering derelict ships and cargo");
        AddRole("exploration", "Exploration", "Surveying unknown space");
        AddRole("scanning", "Scanning/Recon", "Intelligence gathering");
        AddRole("medical", "Medical", "Combat and field medical support");
        AddRole("repair", "Repair", "Field repair of other ships");
        AddRole("refuel", "Refuel", "Field refuelling operations");
        AddRole("refinery", "Refinery", "On-site resource processing");
        AddRole("transport", "Personnel Transport", "Moving crew or passengers");
        AddRole("command", "Command/Coordination", "Fleet coordination");

        // ── Doctrine tags ──────────────────────────────────────────────
        sortOrder = 0;
        void AddDoctrine(string slug, string displayName, string description)
        {
            tags.Add(new TagDefinition
            {
                Key = $"doctrine:{slug}",
                DisplayName = displayName,
                Category = "doctrine",
                Description = description,
                ColorHex = "#a855f7",
                SortOrder = sortOrder++,
                IsSystemDefined = true,
                IsUserEditable = false,
                AllowedScopes = "OwnedShip,UserFleetGroup"
            });
        }

        AddDoctrine("solo", "Solo Operation", "Single pilot, self-sufficient");
        AddDoctrine("small-crew", "Small Crew", "2–4 players");
        AddDoctrine("org-scale", "Org Scale", "Large coordinated operations");
        AddDoctrine("industrial", "Industrial", "Resource gathering and processing focus");
        AddDoctrine("combat", "Combat", "Offensive or defensive military focus");
        AddDoctrine("exploration", "Exploration", "Discovery and surveying focus");
        AddDoctrine("trade", "Trade", "Commerce and logistics focus");
        AddDoctrine("support", "Support", "Enabling other ships and operations");
        AddDoctrine("multipurpose", "Multipurpose", "No single dominant focus");

        // ── Status tags ────────────────────────────────────────────────
        sortOrder = 0;
        void AddStatus(string slug, string displayName, string description)
        {
            tags.Add(new TagDefinition
            {
                Key = $"status:{slug}",
                DisplayName = displayName,
                Category = "status",
                Description = description,
                ColorHex = "#f59e0b",
                SortOrder = sortOrder++,
                IsSystemDefined = true,
                IsUserEditable = true,
                AllowedScopes = "OwnedShip"
            });
        }

        AddStatus("core", "Core Ship", "Essential to the fleet, always deployed");
        AddStatus("situational", "Situational", "Deployed only for specific operations");
        AddStatus("upgrade-target", "Upgrade Target", "Planned for replacement");
        AddStatus("placeholder", "Placeholder", "Temporary fill for a role gap");
        AddStatus("concept", "Concept/Unflown", "Not yet flyable in game");

        // ── Crew pattern tags ──────────────────────────────────────────
        sortOrder = 0;
        void AddCrew(string slug, string displayName, string description)
        {
            tags.Add(new TagDefinition
            {
                Key = $"crew:{slug}",
                DisplayName = displayName,
                Category = "crew",
                Description = description,
                ColorHex = "#06b6d4",
                SortOrder = sortOrder++,
                IsSystemDefined = true,
                IsUserEditable = false,
                AllowedScopes = "OwnedShip"
            });
        }

        AddCrew("solo", "Solo Operated", "Designed and used by one player");
        AddCrew("duo", "Duo Operated", "Best with two players");
        AddCrew("small", "Small Crew", "3–5 players");
        AddCrew("large", "Large Crew", "6+ players");
        AddCrew("npc-viable", "NPC Viable", "Can be crewed with NPCs");

        // ── Capability tags ────────────────────────────────────────────
        sortOrder = 0;
        void AddCapability(string slug, string displayName, string description)
        {
            tags.Add(new TagDefinition
            {
                Key = $"capability:{slug}",
                DisplayName = displayName,
                Category = "capability",
                Description = description,
                ColorHex = "#22c55e",
                SortOrder = sortOrder++,
                IsSystemDefined = true,
                IsUserEditable = false,
                AllowedScopes = "OwnedShip"
            });
        }

        AddCapability("medical", "Has Medical Bay", "Ship has medical facilities");
        AddCapability("repair", "Can Repair Ships", "Ship can perform field repairs");
        AddCapability("refuel", "Can Refuel Ships", "Ship can refuel other ships");
        AddCapability("tractor-beam", "Has Tractor Beam", "Ship has tractor beam equipment");
        AddCapability("quantum-capable", "Quantum Travel Capable", "Ship can perform quantum travel");
        AddCapability("cargo", "Has Cargo Space", "Ship has cargo hold");
        AddCapability("hangar", "Has Ship Hangar", "Ship has internal ship hangar");

        // ── Preference tags ────────────────────────────────────────────
        sortOrder = 0;
        void AddPreference(string slug, string displayName, string description)
        {
            tags.Add(new TagDefinition
            {
                Key = $"preference:{slug}",
                DisplayName = displayName,
                Category = "preference",
                Description = description,
                ColorHex = "#f97316",
                SortOrder = sortOrder++,
                IsSystemDefined = true,
                IsUserEditable = true,
                AllowedScopes = "OwnedShip"
            });
        }

        AddPreference("daily-driver", "Daily Driver", "Used in most play sessions");
        AddPreference("favorite", "Favorite", "Personal favorite");
        AddPreference("lore-pick", "Lore Pick", "Chosen for roleplay or lore reasons");
        AddPreference("investment", "Investment", "Kept for future value or pledging");

        // ── Constraint tags ────────────────────────────────────────────
        sortOrder = 0;
        void AddConstraint(string slug, string displayName, string description)
        {
            tags.Add(new TagDefinition
            {
                Key = $"constraint:{slug}",
                DisplayName = displayName,
                Category = "constraint",
                Description = description,
                ColorHex = "#8890a8",
                SortOrder = sortOrder++,
                IsSystemDefined = true,
                IsUserEditable = true,
                AllowedScopes = "OwnedShip,UserFleetGroup"
            });
        }

        AddConstraint("soloable", "Must Be Soloable", "Player needs to fly this alone");
        AddConstraint("budget", "Budget Constraint", "aUEC cost is a factor");
        AddConstraint("hangar-limited", "Hangar Limited", "Must fit in player's hangar");

        return tags;
    }
}
