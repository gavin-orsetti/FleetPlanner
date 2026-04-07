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
/// exist, so <see cref="InitialiseAsync"/> seeds all system tags and writes version "6".
/// On subsequent launches the row is found and the method checks whether a migration is
/// needed (current version &lt; 6). Future schema migrations can bump the version and add
/// migration logic between the version check and the version write.</para>
///
/// <para><b>Idempotency:</b> Safe to call on every startup. <c>CreateTableAsync</c> is a
/// no-op if the table already exists (SQLite <c>CREATE TABLE IF NOT EXISTS</c>). Seeding
/// only runs when no system tags exist. Even if seeding did run twice, tags use
/// <c>InsertOrReplaceAsync</c> keyed on the stable <see cref="TagDefinition.Key"/>, so
/// duplicates are impossible.</para>
///
/// <para><b>Seeding strategy — 15 tag categories:</b> The taxonomy seeds tags across:
/// <list type="bullet">
///   <item><b>role:economy</b> — career loop the ship supports</item>
///   <item><b>role:activity</b> — moment-to-moment gameplay verb</item>
///   <item><b>role:domain</b> — operational environment</item>
///   <item><b>role:scale</b> — operational footprint (ship only)</item>
///   <item><b>role:posture</b> — engagement stance</item>
///   <item><b>ctx</b> — how and where the ship operates (crew, environment, legality)</item>
///   <item><b>doctrine:weight</b> — how important the ship is to the fleet</item>
///   <item><b>doctrine:frequency</b> — how often the ship deploys</item>
///   <item><b>doctrine:purpose</b> — strategic function in the fleet</item>
///   <item><b>doctrine:autonomy</b> — operational independence</item>
///   <item><b>doctrine:flexibility</b> — role breadth</item>
///   <item><b>doctrine:retention</b> — why the ship is kept (ship only)</item>
///   <item><b>doctrine:lifecycle</b> — intended future of the asset</item>
///   <item><b>status</b> — acquisition and lifecycle state</item>
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
    /// Migrates from previous schema versions by wiping old system tags before reseeding.
    /// <para>
    /// <b>Idempotent:</b> Safe to call on every startup. Table creation is idempotent
    /// (SQLite <c>CREATE TABLE IF NOT EXISTS</c>). The migration block only runs once
    /// per version bump, and the seed guard only runs when no system tags exist.
    /// </para>
    /// <para>
    /// <b>Call site:</b> Invoked synchronously (via <c>.GetAwaiter().GetResult()</c>) from
    /// <see cref="MauiProgram.CreateMauiApp"/> because the MAUI startup contract requires a
    /// synchronous return. The blocking call is safe here because no UI thread exists yet.
    /// </para>
    /// </summary>
    public async Task InitialiseAsync()
    {
        const int currentSchemaVersion = 6;

        var db = new SQLiteAsyncConnection(_dbPath, SQLiteOpenFlags.ReadWrite | SQLiteOpenFlags.Create | SQLiteOpenFlags.SharedCache);

        // Create all tables
        await db.CreateTableAsync<OwnedShip>();
        await db.CreateTableAsync<TagDefinition>();
        await db.CreateTableAsync<OwnedShipTag>();
        await db.CreateTableAsync<UserFleetGroup>();
        await db.CreateTableAsync<UserFleetGroupTag>();
        await db.CreateTableAsync<AppMetadata>();
        await db.CreateTableAsync<ShipCacheMetadata>();
        await db.CreateTableAsync<GroupTagDefinition>();

        // Check schema version for migration
        var versionRecord = await db.FindAsync<AppMetadata>("schema_version");
        var storedVersion = 0;
        if (versionRecord is not null)
            int.TryParse(versionRecord.Value, out storedVersion);

        // Migration: wipe old system tags when upgrading to new taxonomy
        if (storedVersion > 0 && storedVersion < currentSchemaVersion)
        {
            // v4→v5: replace flat 'doctrine' category with 7 atomic intent sub-dimensions
            if (storedVersion < 5)
            {
                await db.ExecuteAsync("DELETE FROM TagDefinitions WHERE Category = 'doctrine' AND IsSystemDefined = 1");
                await db.ExecuteAsync("DELETE FROM GroupTagDefinition WHERE Category = 'doctrine' AND IsSystemDefined = 1");
            }

            // v5→v6: replace flat 'role' category with 5 atomic role sub-dimensions
            if (storedVersion < 6)
            {
                await db.ExecuteAsync("DELETE FROM TagDefinitions WHERE Category = 'role' AND IsSystemDefined = 1");
                await db.ExecuteAsync("DELETE FROM GroupTagDefinition WHERE Category = 'role' AND IsSystemDefined = 1");
            }

            await db.ExecuteAsync("DELETE FROM TagDefinitions WHERE IsSystemDefined = 1");
            await db.ExecuteAsync("DELETE FROM GroupTagDefinition WHERE IsSystemDefined = 1");
            await db.InsertOrReplaceAsync(new AppMetadata
            {
                Key = "schema_version",
                Value = currentSchemaVersion.ToString(),
                UpdatedUtc = DateTime.UtcNow
            });
        }

        // Seed ship tags if no system tags exist (first launch or post-migration wipe)
        var existingSystemTags = await db.Table<TagDefinition>().Where(t => t.IsSystemDefined).CountAsync();
        if (existingSystemTags == 0)
        {
            await SeedTagsAsync(db);
        }

        // Seed group tags if no system group tags exist (first launch or post-migration wipe)
        var existingGroupSystemTags = await db.Table<GroupTagDefinition>().Where(t => t.IsSystemDefined).CountAsync();
        if (existingGroupSystemTags == 0)
        {
            await SeedGroupTagsAsync(db);
        }

        // Record schema version on first launch
        if (versionRecord is null)
        {
            await db.InsertOrReplaceAsync(new AppMetadata
            {
                Key = "schema_version",
                Value = currentSchemaVersion.ToString(),
                UpdatedUtc = DateTime.UtcNow
            });
        }
    }

    private static async Task SeedTagsAsync(SQLiteAsyncConnection db)
    {
        var tags = BuildSystemTags();
        foreach (var tag in tags)
            await db.InsertOrReplaceAsync(tag);
    }

    private static async Task SeedGroupTagsAsync(SQLiteAsyncConnection db)
    {
        var tags = BuildSystemGroupTags();
        foreach (var tag in tags)
            await db.InsertOrReplaceAsync(tag);
    }

    /// <summary>
    /// Builds the complete list of system-defined tags that form the seeded taxonomy.
    /// <para>
    /// Each tag uses a stable <c>"category:slug"</c> key that NEVER changes once shipped.
    /// The slug is a lowercase, hyphenated identifier (e.g. <c>"role:fighter"</c>,
    /// <c>"doctrine:weight:anchor"</c>). Keys are the primary key in SQLite — renaming a slug
    /// would orphan all existing tag assignments.
    /// </para>
    /// <para>
    /// <b>AllowedScopes:</b> All seeded tags use <c>"OwnedShip,UserFleetGroup"</c>.
    /// </para>
    /// <para>
    /// <b>IsSystemDefined:</b> All tags created here have <c>IsSystemDefined = true</c>.
    /// System tags can be archived (hidden from pickers) but never hard-deleted by the user.
    /// This protects the recommendation engine's tag key references from breaking.
    /// </para>
    /// </summary>
    /// <returns>A list of <see cref="TagDefinition"/> records spanning 5 role sub-dimensions
    /// (economy, activity, domain, scale, posture), ctx, 7 doctrine sub-dimensions
    /// (weight, frequency, purpose, autonomy, flexibility, retention, lifecycle), and status.</returns>
    internal static List<TagDefinition> BuildSystemTags()
    {
        var tags = new List<TagDefinition>();

        void Add(string category, string colorHex, int sortOrder, string slug, string displayName, string description)
        {
            tags.Add(new TagDefinition
            {
                Key = $"{category}:{slug}",
                DisplayName = displayName,
                Category = category,
                Description = description,
                ColorHex = colorHex,
                SortOrder = sortOrder,
                IsSystemDefined = true,
                IsUserEditable = false,
                AllowedScopes = "OwnedShip,UserFleetGroup"
            });
        }

        // ── role:economy (7 tags) — career loop ───────────────────────
        const string roleEconomyColor = "#C4706A";
        Add("role:economy", roleEconomyColor, 1, "combat", "Combat", "Value derived from destroying or neutralizing threats");
        Add("role:economy", roleEconomyColor, 2, "extraction", "Extraction", "Value derived from gathering raw resources from the environment");
        Add("role:economy", roleEconomyColor, 3, "logistics", "Logistics", "Value derived from moving people, goods, or data between locations");
        Add("role:economy", roleEconomyColor, 4, "support", "Support", "Value derived from enabling, sustaining, or amplifying other actors");
        Add("role:economy", roleEconomyColor, 5, "intelligence", "Intelligence", "Value derived from collecting, protecting, or denying information");
        Add("role:economy", roleEconomyColor, 6, "competition", "Competition", "Value derived from racing or time-competitive events");
        Add("role:economy", roleEconomyColor, 7, "roleplay", "Roleplay", "Value is social, narrative, or atmospheric rather than mechanical");

        // ── role:activity (21 tags) — moment-to-moment loop ──────────
        const string roleActivityColor = "#B87040";
        Add("role:activity", roleActivityColor, 1, "fight", "Fight", "Direct weapons engagement against other ships or ground targets");
        Add("role:activity", roleActivityColor, 2, "intercept", "Intercept", "High-speed pursuit and denial of other vessels");
        Add("role:activity", roleActivityColor, 3, "bomb", "Bomb", "Ordnance delivery against hardened or capital targets");
        Add("role:activity", roleActivityColor, 4, "board", "Board", "Deploying personnel onto other ships or structures");
        Add("role:activity", roleActivityColor, 5, "mine", "Mine", "Fracturing and extracting mineral or gas resources");
        Add("role:activity", roleActivityColor, 6, "salvage", "Salvage", "Stripping, scraping, or recovering material from wrecks");
        Add("role:activity", roleActivityColor, 7, "haul", "Haul", "Moving bulk cargo between locations");
        Add("role:activity", roleActivityColor, 8, "courier", "Courier", "Moving high-value low-volume cargo or data with speed or stealth premium");
        Add("role:activity", roleActivityColor, 9, "ferry", "Ferry", "Moving passengers or crew between locations");
        Add("role:activity", roleActivityColor, 10, "refuel", "Refuel", "Transferring fuel to other ships in the field");
        Add("role:activity", roleActivityColor, 11, "repair", "Repair", "Restoring hull and component integrity of other ships");
        Add("role:activity", roleActivityColor, 12, "heal", "Heal", "Providing medical treatment to players and NPCs");
        Add("role:activity", roleActivityColor, 13, "scan", "Scan", "Active or passive sensor operations for detection, mapping, or science");
        Add("role:activity", roleActivityColor, 14, "signal", "Signal", "Electronic warfare: jamming, spoofing, disrupting, or amplifying signals");
        Add("role:activity", roleActivityColor, 15, "tug", "Tug", "Recovering disabled ships or objects and repositioning them");
        Add("role:activity", roleActivityColor, 16, "patrol", "Patrol", "Area denial, presence, and threat deterrence through movement");
        Add("role:activity", roleActivityColor, 17, "escort", "Escort", "Close protection of a specific asset or convoy");
        Add("role:activity", roleActivityColor, 18, "race", "Race", "Pure speed competition on designated courses");
        Add("role:activity", roleActivityColor, 19, "farm", "Farm", "Harvesting biological or agricultural resources");
        Add("role:activity", roleActivityColor, 20, "hack", "Hack", "Intrusion into, extraction from, or manipulation of data systems");
        Add("role:activity", roleActivityColor, 21, "rearm", "Rearm", "Resupplying ammunition and ordnance to other ships");

        // ── role:domain (6 tags) — operational environment ───────────
        const string roleDomainColor = "#4A9E6B";
        Add("role:domain", roleDomainColor, 1, "ground", "Ground", "Planetary surface operations; vehicles, structures, EVA support");
        Add("role:domain", roleDomainColor, 2, "atmo", "Atmo", "Within a planet's atmosphere");
        Add("role:domain", roleDomainColor, 3, "orbit", "Orbit", "Low planetary orbit and transition zones");
        Add("role:domain", roleDomainColor, 4, "asteroid", "Asteroid", "Asteroid fields and ring systems");
        Add("role:domain", roleDomainColor, 5, "deep-space", "Deep Space", "Far from stations, jump points, or gravity wells");
        Add("role:domain", roleDomainColor, 6, "urban", "Urban", "Dense station interiors, platforms, or city environments");

        // ── role:scale (5 tags) — operational footprint (ship only) ──
        const string roleScaleColor = "#7A8499";
        Add("role:scale", roleScaleColor, 1, "solo", "Solo", "Designed for single-operator tasks");
        Add("role:scale", roleScaleColor, 2, "pair", "Pair", "Optimized for two-ship cooperative operation");
        Add("role:scale", roleScaleColor, 3, "cell", "Cell", "Optimized for a small squad or cell (3–6 ships)");
        Add("role:scale", roleScaleColor, 4, "fleet", "Fleet", "Designed to operate as part of a coordinated multi-element fleet");
        Add("role:scale", roleScaleColor, 5, "capital", "Capital", "Designed to anchor or command fleet-level engagements");

        // ── role:posture (5 tags) — engagement stance ────────────────
        const string rolePostureColor = "#3A9CB8";
        Add("role:posture", rolePostureColor, 1, "offensive", "Offensive", "Built to find, close with, and destroy");
        Add("role:posture", rolePostureColor, 2, "defensive", "Defensive", "Built to absorb punishment, hold ground, or protect others");
        Add("role:posture", rolePostureColor, 3, "stealth", "Stealth", "Built to avoid detection; trades raw performance for signature reduction");
        Add("role:posture", rolePostureColor, 4, "reactive", "Reactive", "Built to respond to events initiated by others");
        Add("role:posture", rolePostureColor, 5, "enabling", "Enabling", "Built to amplify others' capabilities rather than act independently");

        // ── ctx (14 tags) — how and where the ship operates ────────────
        const string ctxColor = "#3A9CB8";
        Add("ctx", ctxColor, 1, "solo", "Solo", "Genuinely effective with a single pilot");
        Add("ctx", ctxColor, 2, "duo", "Duo", "Optimized for two players");
        Add("ctx", ctxColor, 3, "small-crew", "Small Crew", "Requires or shines with 3–6 players");
        Add("ctx", ctxColor, 4, "large-crew", "Large Crew", "Requires or benefits from 7+ players");
        Add("ctx", ctxColor, 5, "multicrew", "Multicrew", "Non-specific multicrew (crew count varies)");
        Add("ctx", ctxColor, 6, "space-only", "Space Only", "Operates exclusively in space");
        Add("ctx", ctxColor, 7, "atmospheric", "Atmospheric", "Capable of planetary atmosphere operations");
        Add("ctx", ctxColor, 8, "planetary", "Planetary", "Designed for or primarily used on planetary surfaces");
        Add("ctx", ctxColor, 9, "deep-space", "Deep Space", "Extended operations far from stations");
        Add("ctx", ctxColor, 10, "lawful", "Lawful", "Intended for legal operations within UEE space");
        Add("ctx", ctxColor, 11, "unlawful", "Unlawful", "Used for criminal, piracy, or smuggling activities");
        Add("ctx", ctxColor, 12, "neutral", "Neutral", "Grey area: mercenary, bounty hunting, free trade");
        Add("ctx", ctxColor, 13, "org-dependent", "Org Dependent", "Only viable with org-level crew and coordination");
        Add("ctx", ctxColor, 14, "snub-requires-carrier", "Requires Carrier", "Must be deployed from a parent ship");

        // ── doctrine:weight (5 tags) — how important this ship is ──────
        const string weightColor = "#B87040";
        Add("doctrine:weight", weightColor, 1, "anchor", "Anchor", "The fleet's single most load-bearing asset; losing it critically impairs the primary loop");
        Add("doctrine:weight", weightColor, 2, "core", "Core", "Consistently important; a major contributor to the fleet's operational capacity");
        Add("doctrine:weight", weightColor, 3, "supplementary", "Supplementary", "Adds meaningful value but is not essential; the fleet functions without it");
        Add("doctrine:weight", weightColor, 4, "fringe", "Fringe", "Nice to have; minimal impact on primary loops");
        Add("doctrine:weight", weightColor, 5, "luxury", "Luxury", "Kept for reasons other than operational necessity; fleet doesn't depend on it");

        // ── doctrine:frequency (5 tags) — how often this ship deploys ──
        const string frequencyColor = "#7A8499";
        Add("doctrine:frequency", frequencyColor, 1, "always-on", "Always On", "Deployed every session; near-constant presence");
        Add("doctrine:frequency", frequencyColor, 2, "regular", "Regular", "Deployed most sessions; reliably in rotation");
        Add("doctrine:frequency", frequencyColor, 3, "occasional", "Occasional", "Pulled out for specific activities; not a default pick");
        Add("doctrine:frequency", frequencyColor, 4, "standby", "Standby", "Held in reserve; deployed only when conditions call for it");
        Add("doctrine:frequency", frequencyColor, 5, "rare", "Rare", "Almost never deployed; kept for theoretical scenarios");

        // ── doctrine:purpose (9 tags) — strategic function ─────────────
        const string purposeColor = "#8B66B8";
        Add("doctrine:purpose", purposeColor, 1, "earner", "Earner", "Primary income/resource generator; the fleet's economic engine");
        Add("doctrine:purpose", purposeColor, 2, "protector", "Protector", "Exists to keep other ships safe; value measured in losses prevented");
        Add("doctrine:purpose", purposeColor, 3, "enabler", "Enabler", "Unlocks capabilities in other ships (refueling, repair, medical, logistics)");
        Add("doctrine:purpose", purposeColor, 4, "suppressor", "Suppressor", "Denies the enemy options: EW, interdiction, area denial");
        Add("doctrine:purpose", purposeColor, 5, "expander", "Expander", "Extends the fleet's operational reach or access");
        Add("doctrine:purpose", purposeColor, 6, "deliverer", "Deliverer", "Moves things or people from one place to another as its core value");
        Add("doctrine:purpose", purposeColor, 7, "controller", "Controller", "Commands and coordinates other assets; a force multiplier through organization");
        Add("doctrine:purpose", purposeColor, 8, "wildcard", "Wildcard", "Keeps options open; exists to respond to unexpected situations");
        Add("doctrine:purpose", purposeColor, 9, "experiment", "Experiment", "Being evaluated; no fixed purpose yet committed");

        // ── doctrine:autonomy (6 tags) — operational independence ──────
        const string autonomyColor = "#3A9CB8";
        Add("doctrine:autonomy", autonomyColor, 1, "self-reliant", "Self-Reliant", "Fully operational without requiring other fleet assets");
        Add("doctrine:autonomy", autonomyColor, 2, "paired", "Paired", "Designed to operate with one other ship/group as a consistent duo");
        Add("doctrine:autonomy", autonomyColor, 3, "grouped", "Grouped", "Operates as part of a wing or small coordinated element");
        Add("doctrine:autonomy", autonomyColor, 4, "fleet-linked", "Fleet Linked", "Requires broader fleet infrastructure to be effective");
        Add("doctrine:autonomy", autonomyColor, 5, "carrier-based", "Carrier Based", "Depends entirely on a parent ship for deployment and recovery");
        Add("doctrine:autonomy", autonomyColor, 6, "enables-others", "Enables Others", "Its value is realized through what it gives other assets");

        // ── doctrine:flexibility (5 tags) — role breadth ──────────────
        const string flexibilityColor = "#4A9E6B";
        Add("doctrine:flexibility", flexibilityColor, 1, "dedicated", "Dedicated", "Strictly single-purpose; optimized for one role and poorly suited for others");
        Add("doctrine:flexibility", flexibilityColor, 2, "specialist", "Specialist", "Primarily one role but with limited secondary capability");
        Add("doctrine:flexibility", flexibilityColor, 3, "versatile", "Versatile", "Genuinely useful across two or three different roles");
        Add("doctrine:flexibility", flexibilityColor, 4, "generalist", "Generalist", "Can contribute to almost any loop, but rarely dominates any of them");
        Add("doctrine:flexibility", flexibilityColor, 5, "swing", "Swing", "A flexible ship held specifically to fill whatever gap appears in a session");

        // ── doctrine:retention (7 tags) — why this ship is kept (ship only) ─
        const string retentionColor = "#C4706A";
        Add("doctrine:retention", retentionColor, 1, "utility", "Utility", "Kept because it is operationally useful; a rational acquisition");
        Add("doctrine:retention", retentionColor, 2, "identity", "Identity", "Kept because it represents who the player is or wants to be");
        Add("doctrine:retention", retentionColor, 3, "aspiration", "Aspiration", "Kept because the player wants to grow into using it");
        Add("doctrine:retention", retentionColor, 4, "lore", "Lore", "Kept for narrative, worldbuilding, or roleplay reasons");
        Add("doctrine:retention", retentionColor, 5, "social", "Social", "Kept to enable play with specific people");
        Add("doctrine:retention", retentionColor, 6, "progression", "Progression", "A bridge ship; kept while working toward something else");
        Add("doctrine:retention", retentionColor, 7, "attachment", "Attachment", "Kept due to sentimental value regardless of utility");

        // ── doctrine:lifecycle (6 tags) — intended future ─────────────
        const string lifecycleColor = "#6B7A8B";
        Add("doctrine:lifecycle", lifecycleColor, 1, "permanent", "Permanent", "Intended as a long-term fleet member; not under consideration for replacement");
        Add("doctrine:lifecycle", lifecycleColor, 2, "developing", "Developing", "New to the fleet; use case still being defined and refined");
        Add("doctrine:lifecycle", lifecycleColor, 3, "transitional", "Transitional", "A placeholder until a better ship is acquired; expected to be replaced");
        Add("doctrine:lifecycle", lifecycleColor, 4, "legacy", "Legacy", "An older ship from a previous fleet doctrine; still present but not actively developed");
        Add("doctrine:lifecycle", lifecycleColor, 5, "evaluating", "Evaluating", "Being tested; a final keep/sell decision has not been made");
        Add("doctrine:lifecycle", lifecycleColor, 6, "terminal", "Terminal", "Marked for eventual removal; kept for specific remaining purposes");

        // ── status (8 tags) — acquisition and lifecycle state ──────────
        const string statusColor = "#B8913A";
        Add("status", statusColor, 1, "owned", "Owned", "Ship is in the hangar, fully acquired");
        Add("status", statusColor, 2, "planned", "Planned", "Being actively saved toward or prioritized");
        Add("status", statusColor, 3, "concept", "Concept", "Long-term aspiration, not actively pursued");
        Add("status", statusColor, 4, "loaner", "Loaner", "Temporarily available through CIG's loaner system");
        Add("status", statusColor, 5, "pledged", "Pledged", "Pledged to CIG but not yet flight-ready in game");
        Add("status", statusColor, 6, "on-loan", "On Loan", "Borrowed from org or friend; not permanently owned");
        Add("status", statusColor, 7, "for-review", "For Review", "Under evaluation; undecided whether to keep");
        Add("status", statusColor, 8, "retired", "Retired", "No longer part of the active fleet");

        return tags;
    }

    /// <summary>
    /// Builds the complete list of system-defined group tags that form the group tag taxonomy.
    /// <para>
    /// Each tag uses a stable <c>"category:slug"</c> key that NEVER changes once shipped.
    /// Group tags are stored in a separate <see cref="GroupTagDefinition"/> table from
    /// ship tags. The taxonomy seeds tags across:
    /// <list type="bullet">
    ///   <item><b>role:economy</b> (9) — career loop (7 shared + 2 group extras)</item>
    ///   <item><b>role:activity</b> (21) — moment-to-moment gameplay verb (shared with ship)</item>
    ///   <item><b>role:domain</b> (6) — operational environment (shared with ship)</item>
    ///   <item><b>role:scope</b> (3) — group-only operational scope (replaces ship scale)</item>
    ///   <item><b>role:posture</b> (5) — engagement stance (shared with ship)</item>
    ///   <item><b>ctx</b> (16) — operational conditions: scale, theater, autonomy, legality</item>
    ///   <item><b>doctrine:weight</b> (4) — group importance</item>
    ///   <item><b>doctrine:frequency</b> (5) — deployment cadence (shared with ship)</item>
    ///   <item><b>doctrine:purpose</b> (6) — group-specific strategic function</item>
    ///   <item><b>doctrine:autonomy</b> (6) — operational independence (shared with ship)</item>
    ///   <item><b>doctrine:flexibility</b> (5) — role breadth (shared with ship)</item>
    ///   <item><b>doctrine:lifecycle</b> (7) — intended future (shared with ship + experimental)</item>
    ///   <item><b>status</b> (8) — formation readiness and planning lifecycle</item>
    /// </list></para>
    /// <para>
    /// <b>AllowedScopes:</b> All seeded group tags use <c>"UserFleetGroup"</c>.
    /// </para>
    /// </summary>
    /// <returns>A list of <see cref="GroupTagDefinition"/> records spanning 5 role sub-dimensions
    /// (economy, activity, domain, scope, posture), ctx, 6 doctrine sub-dimensions
    /// (weight, frequency, purpose, autonomy, flexibility, lifecycle), and status.</returns>
    internal static List<GroupTagDefinition> BuildSystemGroupTags()
    {
        var tags = new List<GroupTagDefinition>();

        void Add(string category, string colorHex, int sortOrder, string slug, string displayName, string description)
        {
            tags.Add(new GroupTagDefinition
            {
                Key = $"{category}:{slug}",
                DisplayName = displayName,
                Category = category,
                Description = description,
                ColorHex = colorHex,
                SortOrder = sortOrder,
                IsSystemDefined = true,
                IsUserEditable = false,
                AllowedScopes = "UserFleetGroup"
            });
        }

        // ── role:economy (9 tags) — career loop (7 shared + 2 group extras) ─
        const string roleEconomyColor = "#C4706A";
        Add("role:economy", roleEconomyColor, 1, "combat", "Combat", "Value derived from destroying or neutralizing threats");
        Add("role:economy", roleEconomyColor, 2, "extraction", "Extraction", "Value derived from gathering raw resources from the environment");
        Add("role:economy", roleEconomyColor, 3, "logistics", "Logistics", "Value derived from moving people, goods, or data between locations");
        Add("role:economy", roleEconomyColor, 4, "support", "Support", "Value derived from enabling, sustaining, or amplifying other actors");
        Add("role:economy", roleEconomyColor, 5, "intelligence", "Intelligence", "Value derived from collecting, protecting, or denying information");
        Add("role:economy", roleEconomyColor, 6, "competition", "Competition", "Value derived from racing or time-competitive events");
        Add("role:economy", roleEconomyColor, 7, "roleplay", "Roleplay", "Value is social, narrative, or atmospheric rather than mechanical");
        Add("role:economy", roleEconomyColor, 8, "power-projection", "Power Projection", "Group exists to project force or presence into contested space");
        Add("role:economy", roleEconomyColor, 9, "self-sufficiency", "Self-Sufficiency", "Group designed to operate for extended periods without external resupply");

        // ── role:activity (21 tags) — shared with ship, identical ────
        const string roleActivityColor = "#B87040";
        Add("role:activity", roleActivityColor, 1, "fight", "Fight", "Direct weapons engagement against other ships or ground targets");
        Add("role:activity", roleActivityColor, 2, "intercept", "Intercept", "High-speed pursuit and denial of other vessels");
        Add("role:activity", roleActivityColor, 3, "bomb", "Bomb", "Ordnance delivery against hardened or capital targets");
        Add("role:activity", roleActivityColor, 4, "board", "Board", "Deploying personnel onto other ships or structures");
        Add("role:activity", roleActivityColor, 5, "mine", "Mine", "Fracturing and extracting mineral or gas resources");
        Add("role:activity", roleActivityColor, 6, "salvage", "Salvage", "Stripping, scraping, or recovering material from wrecks");
        Add("role:activity", roleActivityColor, 7, "haul", "Haul", "Moving bulk cargo between locations");
        Add("role:activity", roleActivityColor, 8, "courier", "Courier", "Moving high-value low-volume cargo or data with speed or stealth premium");
        Add("role:activity", roleActivityColor, 9, "ferry", "Ferry", "Moving passengers or crew between locations");
        Add("role:activity", roleActivityColor, 10, "refuel", "Refuel", "Transferring fuel to other ships in the field");
        Add("role:activity", roleActivityColor, 11, "repair", "Repair", "Restoring hull and component integrity of other ships");
        Add("role:activity", roleActivityColor, 12, "heal", "Heal", "Providing medical treatment to players and NPCs");
        Add("role:activity", roleActivityColor, 13, "scan", "Scan", "Active or passive sensor operations for detection, mapping, or science");
        Add("role:activity", roleActivityColor, 14, "signal", "Signal", "Electronic warfare: jamming, spoofing, disrupting, or amplifying signals");
        Add("role:activity", roleActivityColor, 15, "tug", "Tug", "Recovering disabled ships or objects and repositioning them");
        Add("role:activity", roleActivityColor, 16, "patrol", "Patrol", "Area denial, presence, and threat deterrence through movement");
        Add("role:activity", roleActivityColor, 17, "escort", "Escort", "Close protection of a specific asset or convoy");
        Add("role:activity", roleActivityColor, 18, "race", "Race", "Pure speed competition on designated courses");
        Add("role:activity", roleActivityColor, 19, "farm", "Farm", "Harvesting biological or agricultural resources");
        Add("role:activity", roleActivityColor, 20, "hack", "Hack", "Intrusion into, extraction from, or manipulation of data systems");
        Add("role:activity", roleActivityColor, 21, "rearm", "Rearm", "Resupplying ammunition and ordnance to other ships");

        // ── role:domain (6 tags) — shared with ship, identical ───────
        const string roleDomainColor = "#4A9E6B";
        Add("role:domain", roleDomainColor, 1, "ground", "Ground", "Planetary surface operations; vehicles, structures, EVA support");
        Add("role:domain", roleDomainColor, 2, "atmo", "Atmo", "Within a planet's atmosphere");
        Add("role:domain", roleDomainColor, 3, "orbit", "Orbit", "Low planetary orbit and transition zones");
        Add("role:domain", roleDomainColor, 4, "asteroid", "Asteroid", "Asteroid fields and ring systems");
        Add("role:domain", roleDomainColor, 5, "deep-space", "Deep Space", "Far from stations, jump points, or gravity wells");
        Add("role:domain", roleDomainColor, 6, "urban", "Urban", "Dense station interiors, platforms, or city environments");

        // ── role:scope (3 tags) — group-only, replaces scale ─────────
        const string roleScopeColor = "#7A8499";
        Add("role:scope", roleScopeColor, 1, "tactical", "Tactical", "Designed for engagements or tasks at the individual-encounter level");
        Add("role:scope", roleScopeColor, 2, "operational", "Operational", "Designed for sustained multi-engagement campaigns in a region or system");
        Add("role:scope", roleScopeColor, 3, "strategic", "Strategic", "Designed to shape theater-level outcomes across multiple systems");

        // ── role:posture (5 tags) — shared with ship, identical ──────
        const string rolePostureColor = "#3A9CB8";
        Add("role:posture", rolePostureColor, 1, "offensive", "Offensive", "Built to find, close with, and destroy");
        Add("role:posture", rolePostureColor, 2, "defensive", "Defensive", "Built to absorb punishment, hold ground, or protect others");
        Add("role:posture", rolePostureColor, 3, "stealth", "Stealth", "Built to avoid detection; trades raw performance for signature reduction");
        Add("role:posture", rolePostureColor, 4, "reactive", "Reactive", "Built to respond to events initiated by others");
        Add("role:posture", rolePostureColor, 5, "enabling", "Enabling", "Built to amplify others' capabilities rather than act independently");

        // ── ctx (16 tags) — operational conditions ───────────────────
        const string ctxColor = "#3A9CB8";
        Add("ctx", ctxColor, 1, "solo-operated", "Solo Operated", "Entire group is operated by a single player across its ships");
        Add("ctx", ctxColor, 2, "small-team", "Small Team", "Group requires 2–5 players to function at intended capacity");
        Add("ctx", ctxColor, 3, "full-crew", "Full Crew", "Group requires 6–15 players across its ships");
        Add("ctx", ctxColor, 4, "org-scale", "Org Scale", "Group requires 15+ players; only viable with substantial org coordination");
        Add("ctx", ctxColor, 5, "autonomous", "Autonomous", "Group operates independently without requiring coordination with the rest of the fleet");
        Add("ctx", ctxColor, 6, "fleet-dependent", "Fleet Dependent", "Group relies on other fleet elements to be effective");
        Add("ctx", ctxColor, 7, "space-theater", "Space Theater", "Operates exclusively in space");
        Add("ctx", ctxColor, 8, "atmospheric", "Atmospheric", "Designed to conduct operations in planetary atmospheres");
        Add("ctx", ctxColor, 9, "planetary-surface", "Planetary Surface", "Operates on planetary surfaces; includes ground vehicles and landing craft");
        Add("ctx", ctxColor, 10, "deep-space", "Deep Space", "Intended for extended operations far from stations or populated systems");
        Add("ctx", ctxColor, 11, "local-space", "Local Space", "Operates within a single system or near a station/planet");
        Add("ctx", ctxColor, 12, "lawful", "Lawful", "Group operates within legal bounds");
        Add("ctx", ctxColor, 13, "unlawful", "Unlawful", "Group conducts criminal operations (piracy, smuggling, griefing)");
        Add("ctx", ctxColor, 14, "neutral", "Neutral", "Grey-area operations: mercenary, bounty hunting, free trade");
        Add("ctx", ctxColor, 15, "rapid-deployment", "Rapid Deployment", "Group is designed for quick scramble and fast operational tempo");
        Add("ctx", ctxColor, 16, "sustained-ops", "Sustained Ops", "Group is designed for long-duration, extended operations with logistics support");

        // ── doctrine:weight (4 tags) — group importance ────────────────
        const string weightColor = "#B87040";
        Add("doctrine:weight", weightColor, 1, "primary-arm", "Primary Arm", "The group executing the fleet's main declared loop");
        Add("doctrine:weight", weightColor, 2, "secondary-arm", "Secondary Arm", "A significant but subordinate group; handles a secondary declared loop");
        Add("doctrine:weight", weightColor, 3, "supporting-element", "Supporting Element", "Exists to enable other groups; does not execute primary loops directly");
        Add("doctrine:weight", weightColor, 4, "contingency", "Contingency", "Exists for rare or specific scenarios; not regularly contributing");

        // ── doctrine:frequency (5 tags) — shared with ship ──────────
        const string frequencyColor = "#7A8499";
        Add("doctrine:frequency", frequencyColor, 1, "always-on", "Always On", "Deployed every session; near-constant presence");
        Add("doctrine:frequency", frequencyColor, 2, "regular", "Regular", "Deployed most sessions; reliably in rotation");
        Add("doctrine:frequency", frequencyColor, 3, "occasional", "Occasional", "Pulled out for specific activities; not a default pick");
        Add("doctrine:frequency", frequencyColor, 4, "standby", "Standby", "Held in reserve; deployed only when conditions call for it");
        Add("doctrine:frequency", frequencyColor, 5, "rare", "Rare", "Almost never deployed; kept for theoretical scenarios");

        // ── doctrine:purpose (6 tags) — group-specific ──────────────
        const string purposeColor = "#8B66B8";
        Add("doctrine:purpose", purposeColor, 1, "strike-element", "Strike Element", "Exists to project offensive force against targets");
        Add("doctrine:purpose", purposeColor, 2, "shield-element", "Shield Element", "Exists to absorb, deflect, or deny offensive pressure");
        Add("doctrine:purpose", purposeColor, 3, "lift-element", "Lift Element", "Exists to deliver forces, cargo, or passengers to objectives");
        Add("doctrine:purpose", purposeColor, 4, "sustain-element", "Sustain Element", "Exists to extend the operational endurance of other groups");
        Add("doctrine:purpose", purposeColor, 5, "recon-element", "Recon Element", "Exists to gather information and expand situational awareness");
        Add("doctrine:purpose", purposeColor, 6, "control-element", "Control Element", "Exists to coordinate and command fleet operations");

        // ── doctrine:autonomy (6 tags) — shared with ship ───────────
        const string autonomyColor = "#3A9CB8";
        Add("doctrine:autonomy", autonomyColor, 1, "self-reliant", "Self-Reliant", "Fully operational without requiring other fleet assets");
        Add("doctrine:autonomy", autonomyColor, 2, "paired", "Paired", "Designed to operate with one other ship/group as a consistent duo");
        Add("doctrine:autonomy", autonomyColor, 3, "grouped", "Grouped", "Operates as part of a wing or small coordinated element");
        Add("doctrine:autonomy", autonomyColor, 4, "fleet-linked", "Fleet Linked", "Requires broader fleet infrastructure to be effective");
        Add("doctrine:autonomy", autonomyColor, 5, "carrier-based", "Carrier Based", "Depends entirely on a parent ship for deployment and recovery");
        Add("doctrine:autonomy", autonomyColor, 6, "enables-others", "Enables Others", "Its value is realized through what it gives other assets");

        // ── doctrine:flexibility (5 tags) — shared with ship ────────
        const string flexibilityColor = "#4A9E6B";
        Add("doctrine:flexibility", flexibilityColor, 1, "dedicated", "Dedicated", "Strictly single-purpose; optimized for one role and poorly suited for others");
        Add("doctrine:flexibility", flexibilityColor, 2, "specialist", "Specialist", "Primarily one role but with limited secondary capability");
        Add("doctrine:flexibility", flexibilityColor, 3, "versatile", "Versatile", "Genuinely useful across two or three different roles");
        Add("doctrine:flexibility", flexibilityColor, 4, "generalist", "Generalist", "Can contribute to almost any loop, but rarely dominates any of them");
        Add("doctrine:flexibility", flexibilityColor, 5, "swing", "Swing", "A flexible ship held specifically to fill whatever gap appears in a session");

        // ── doctrine:lifecycle (7 tags) — shared with ship + experimental ─
        const string lifecycleColor = "#6B7A8B";
        Add("doctrine:lifecycle", lifecycleColor, 1, "permanent", "Permanent", "Intended as a long-term fleet member; not under consideration for replacement");
        Add("doctrine:lifecycle", lifecycleColor, 2, "developing", "Developing", "New to the fleet; use case still being defined and refined");
        Add("doctrine:lifecycle", lifecycleColor, 3, "transitional", "Transitional", "A placeholder until a better ship is acquired; expected to be replaced");
        Add("doctrine:lifecycle", lifecycleColor, 4, "legacy", "Legacy", "An older ship from a previous fleet doctrine; still present but not actively developed");
        Add("doctrine:lifecycle", lifecycleColor, 5, "evaluating", "Evaluating", "Being tested; a final keep/sell decision has not been made");
        Add("doctrine:lifecycle", lifecycleColor, 6, "terminal", "Terminal", "Marked for eventual removal; kept for specific remaining purposes");
        Add("doctrine:lifecycle", lifecycleColor, 7, "experimental", "Experimental", "A group whose doctrine is still being tested or refined");

        // ── status (8 tags) — formation readiness and lifecycle ──────
        const string statusColor = "#B8913A";
        Add("status", statusColor, 1, "operational", "Operational", "Group is fully assembled, assigned, and deployable as declared");
        Add("status", statusColor, 2, "partial", "Partial", "Group exists but is missing ships or crew to reach declared capacity");
        Add("status", statusColor, 3, "undermanned", "Undermanned", "Group has the ships but lacks the players to crew them at intended scale");
        Add("status", statusColor, 4, "paper", "Paper", "Group is defined in doctrine only; no ships have been assigned yet");
        Add("status", statusColor, 5, "assembling", "Assembling", "Actively being built; ships are being added and assignments made");
        Add("status", statusColor, 6, "on-hold", "On Hold", "Group is defined and has ships but is not being actively developed or deployed");
        Add("status", statusColor, 7, "retired", "Retired", "Group has been dissolved or superseded; kept for historical reference");
        Add("status", statusColor, 8, "contingency", "Contingency", "Group exists purely for a specific scenario; not a standing formation");

        return tags;
    }
}
