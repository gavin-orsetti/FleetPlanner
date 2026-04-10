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
/// exist, so <see cref="InitialiseAsync"/> seeds all system tags and writes version "7".
/// On subsequent launches the row is found and the method checks whether a migration is
/// needed (current version &lt; 7). Future schema migrations can bump the version and add
/// migration logic between the version check and the version write.</para>
///
/// <para><b>Idempotency:</b> Safe to call on every startup. <c>CreateTableAsync</c> is a
/// no-op if the table already exists (SQLite <c>CREATE TABLE IF NOT EXISTS</c>). Seeding
/// only runs when no system tags exist. Even if seeding did run twice, tags use
/// <c>InsertOrReplaceAsync</c> keyed on the stable <see cref="TagDefinition.Key"/>, so
/// duplicates are impossible.</para>
///
/// <para><b>Seeding strategy — 5-pillar tag taxonomy (86 ship + 51 group):</b>
/// <list type="bullet">
///   <item><b>doctrine:</b> — fleet-scoped persistent beliefs (value, frequency, investment, identity, structural)</item>
///   <item><b>intent:</b> — group-scoped deployment commitment (activity, economy, crew, legal, org)</item>
///   <item><b>potency:</b> — group-scoped force multiplication (capacity, reach, resilience, footprint)</item>
///   <item><b>status:</b> — fleet-scoped lifecycle state (lifecycle, modifier)</item>
///   <item><b>tradeoff:</b> — group-scoped accepted downsides (flat)</item>
/// </list></para>
///
/// <para><b>Why stable slug keys instead of integer IDs:</b> System tags use string keys
/// in <c>"category:slug"</c> format (e.g. <c>"doctrine:value:backbone"</c>) rather than
/// auto-increment integers. This ensures keys are stable across installs, schema migrations,
/// and database resets — a tag assignment saved as <c>"intent:activity:fight"</c> is always
/// meaningful, even if the database was recreated.</para>
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
        const int currentSchemaVersion = 7;

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

        // Migration: wipe ALL old system tags when upgrading to 5-pillar taxonomy
        if (storedVersion > 0 && storedVersion < currentSchemaVersion)
        {
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
    /// Builds the complete list of system-defined ship tags — 86 tags across 5 pillars.
    /// <para>
    /// <b>Pillars:</b> doctrine (16), intent (33), potency (19), status (9), tradeoff (9).
    /// </para>
    /// <para>
    /// <b>AllowedScopes:</b> All seeded tags use <c>"OwnedShip,UserFleetGroup"</c>.
    /// </para>
    /// </summary>
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

        // ── doctrine:value (4 tags) — how irreplaceable is this ship ─
        const string doctrineColor = "#8B66B8";
        Add("doctrine:value", doctrineColor, 1, "backbone", "Backbone", "Core to the fleet; loss would significantly degrade capability");
        Add("doctrine:value", doctrineColor, 2, "specialist", "Specialist", "Uniquely capable at one thing; no substitute in the fleet");
        Add("doctrine:value", doctrineColor, 3, "filler", "Filler", "Useful but easily replaced; one of several interchangeable options");
        Add("doctrine:value", doctrineColor, 4, "reserve", "Reserve", "Kept for contingency; not part of regular deployment planning");

        // ── doctrine:frequency (4 tags) — how often deployed ─────────
        Add("doctrine:frequency", doctrineColor, 1, "daily-driver", "Daily Driver", "Default choice; deployed in the majority of sessions");
        Add("doctrine:frequency", doctrineColor, 2, "rotational", "Rotational", "Deployed regularly but not every session; cycles with others");
        Add("doctrine:frequency", doctrineColor, 3, "occasional", "Occasional", "Pulled out for specific circumstances; not a regular deploy");
        Add("doctrine:frequency", doctrineColor, 4, "situational", "Situational", "Deployed only when a very specific condition is met");

        // ── doctrine:investment (3 tags) — subjective cost ───────────
        Add("doctrine:investment", doctrineColor, 1, "high-investment", "High Investment", "Significant real-money or in-game commitment; loss is felt");
        Add("doctrine:investment", doctrineColor, 2, "mid-investment", "Mid Investment", "Moderate commitment; replaceable but not trivial");
        Add("doctrine:investment", doctrineColor, 3, "low-investment", "Low Investment", "Easy to replace; loss is inconsequential");

        // ── doctrine:identity (3 tags) — personal meaning ────────────
        Add("doctrine:identity", doctrineColor, 1, "legacy", "Legacy", "Held for historical or sentimental reasons");
        Add("doctrine:identity", doctrineColor, 2, "aspirational", "Aspirational", "Not yet fully utilized; represents a future capability goal");
        Add("doctrine:identity", doctrineColor, 3, "signature", "Signature", "Defines the user's identity or brand within the community");

        // ── doctrine:structural (2 tags) — fleet progression ─────────
        Add("doctrine:structural", doctrineColor, 1, "bridge", "Bridge", "A stepping-stone ship; useful now but will be superseded");
        Add("doctrine:structural", doctrineColor, 2, "capstone", "Capstone", "The highest-expression ship in the fleet; goal state");

        // ── intent:activity (16 tags) — what the ship does ───────────
        const string intentColor = "#C4706A";
        Add("intent:activity", intentColor, 1, "fight", "Fight", "Primary purpose is direct combat engagement");
        Add("intent:activity", intentColor, 2, "haul", "Haul", "Primary purpose is cargo transport");
        Add("intent:activity", intentColor, 3, "mine", "Mine", "Primary purpose is resource extraction");
        Add("intent:activity", intentColor, 4, "salvage", "Salvage", "Primary purpose is wreck processing and reclamation");
        Add("intent:activity", intentColor, 5, "explore", "Explore", "Primary purpose is discovery, scanning, and charting");
        Add("intent:activity", intentColor, 6, "patrol", "Patrol", "Primary purpose is presence, deterrence, and response");
        Add("intent:activity", intentColor, 7, "escort", "Escort", "Primary purpose is protecting another specific asset");
        Add("intent:activity", intentColor, 8, "support", "Support", "Primary purpose is enabling other ships (repair, rearm, refuel)");
        Add("intent:activity", intentColor, 9, "heal", "Heal", "Primary purpose is crew medical support");
        Add("intent:activity", intentColor, 10, "scan", "Scan", "Primary purpose is intelligence gathering");
        Add("intent:activity", intentColor, 11, "hack", "Hack", "Primary purpose is electronic warfare and system intrusion");
        Add("intent:activity", intentColor, 12, "recon", "Recon", "Primary purpose is advance scouting and threat assessment");
        Add("intent:activity", intentColor, 13, "ferry", "Ferry", "Primary purpose is player/crew transport");
        Add("intent:activity", intentColor, 14, "command", "Command", "Primary purpose is coordination and fleet command");
        Add("intent:activity", intentColor, 15, "race", "Race", "Primary purpose is competitive speed events");
        Add("intent:activity", intentColor, 16, "respond", "Respond", "Primary purpose is rapid reaction to emerging situations");

        // ── intent:economy (7 tags) — career loop ────────────────────
        Add("intent:economy", intentColor, 1, "combat-loop", "Combat Loop", "Ship earns value through combat activities");
        Add("intent:economy", intentColor, 2, "extraction-loop", "Extraction Loop", "Ship earns value through resource extraction");
        Add("intent:economy", intentColor, 3, "logistics-loop", "Logistics Loop", "Ship earns value through moving goods or people");
        Add("intent:economy", intentColor, 4, "support-loop", "Support Loop", "Ship earns value by enabling other players' loops");
        Add("intent:economy", intentColor, 5, "intel-loop", "Intel Loop", "Ship earns value through information and data activities");
        Add("intent:economy", intentColor, 6, "competition-loop", "Competition Loop", "Ship earns value through competitive events");
        Add("intent:economy", intentColor, 7, "roleplay-loop", "Roleplay Loop", "Ship earns value through narrative and social activities");

        // ── intent:crew (3 tags) — crew commitment ───────────────────
        Add("intent:crew", intentColor, 1, "solo", "Solo", "User intends to fly this ship alone");
        Add("intent:crew", intentColor, 2, "duo", "Duo", "User intends to fly with one other player");
        Add("intent:crew", intentColor, 3, "crewed", "Crewed", "User intends to fly with a full or near-full crew");

        // ── intent:legal (3 tags) — legal stance ─────────────────────
        Add("intent:legal", intentColor, 1, "lawful", "Lawful", "Operations will remain within legal boundaries");
        Add("intent:legal", intentColor, 2, "grey", "Grey", "Operations may cross legal lines situationally");
        Add("intent:legal", intentColor, 3, "unlawful", "Unlawful", "Operations will actively violate law");

        // ── intent:org (4 tags) — org context ────────────────────────
        Add("intent:org", intentColor, 1, "org-op", "Org Op", "Deployed in coordinated org operations");
        Add("intent:org", intentColor, 2, "pickup-group", "Pickup Group", "Deployed with ad-hoc players");
        Add("intent:org", intentColor, 3, "solo-op", "Solo Op", "Deployed in personal, single-player sessions");
        Add("intent:org", intentColor, 4, "public", "Public", "Available for public crew or community events");

        // ── potency:capacity (5 tags) — mission amplification ────────
        const string potencyColor = "#3A9CB8";
        Add("potency:capacity", potencyColor, 1, "overwhelming", "Overwhelming", "Dramatically exceeds the group's needs for this mission");
        Add("potency:capacity", potencyColor, 2, "high", "High", "Meaningfully above what the mission requires");
        Add("potency:capacity", potencyColor, 3, "matched", "Matched", "Well-proportioned to the mission's demands");
        Add("potency:capacity", potencyColor, 4, "low", "Low", "Below what the mission ideally calls for");
        Add("potency:capacity", potencyColor, 5, "token", "Token", "Minimal contribution to the mission's core work");

        // ── potency:reach (4 tags) — operational range ───────────────
        Add("potency:reach", potencyColor, 1, "extended", "Extended", "Significantly expands operational range");
        Add("potency:reach", potencyColor, 2, "standard", "Standard", "Matches the group's baseline range");
        Add("potency:reach", potencyColor, 3, "limited", "Limited", "Constrains or is constrained by the group's range");
        Add("potency:reach", potencyColor, 4, "point", "Point", "Negligible range contribution; point-deployment only");

        // ── potency:resilience (5 tags) — survivability ──────────────
        Add("potency:resilience", potencyColor, 1, "hardened", "Hardened", "Built to absorb and continue");
        Add("potency:resilience", potencyColor, 2, "robust", "Robust", "Above-average survivability");
        Add("potency:resilience", potencyColor, 3, "moderate", "Moderate", "Standard survivability for the mission context");
        Add("potency:resilience", potencyColor, 4, "fragile", "Fragile", "Vulnerable; loss significantly degrades the group");
        Add("potency:resilience", potencyColor, 5, "expendable", "Expendable", "Expected to be lost or consumed; planned for");

        // ── potency:footprint (5 tags) — presence/signature ──────────
        Add("potency:footprint", potencyColor, 1, "dominant", "Dominant", "Impossible to ignore; commands attention");
        Add("potency:footprint", potencyColor, 2, "heavy", "Heavy", "Significant presence; will be noticed");
        Add("potency:footprint", potencyColor, 3, "standard", "Standard", "Unremarkable presence");
        Add("potency:footprint", potencyColor, 4, "light", "Light", "Below-average presence");
        Add("potency:footprint", potencyColor, 5, "minimal", "Minimal", "Near-invisible; avoids detection");

        // ── status:lifecycle (5 tags) — acquisition state ────────────
        const string statusColor = "#B8913A";
        Add("status:lifecycle", statusColor, 1, "concept", "Concept", "Ship is in wishlist/planning stage");
        Add("status:lifecycle", statusColor, 2, "pledged", "Pledged", "Purchased via RSI store but not flight-ready");
        Add("status:lifecycle", statusColor, 3, "loaner", "Loaner", "Available via loaner program");
        Add("status:lifecycle", statusColor, 4, "owned", "Owned", "Flight-ready and available in-game");
        Add("status:lifecycle", statusColor, 5, "retired", "Retired", "Removed from active fleet");

        // ── status:modifier (4 tags) — pledge details ────────────────
        Add("status:modifier", statusColor, 1, "ccu-pending", "CCU Pending", "Has a pending Chain of Upgrades");
        Add("status:modifier", statusColor, 2, "warbond", "Warbond", "Purchased as warbond pledge");
        Add("status:modifier", statusColor, 3, "lti", "LTI", "Carries Lifetime Insurance");
        Add("status:modifier", statusColor, 4, "gifted", "Gifted", "Received as a gift from another player");

        // ── tradeoff (9 tags) — accepted downsides (flat) ────────────
        const string tradeoffColor = "#7A8499";
        Add("tradeoff", tradeoffColor, 1, "undercrew", "Undercrew", "Operating with fewer crew than designed for");
        Add("tradeoff", tradeoffColor, 2, "overcrew", "Overcrew", "Ship too small for available crew pool");
        Add("tradeoff", tradeoffColor, 3, "underarmed", "Underarmed", "Insufficient capability for threat environment");
        Add("tradeoff", tradeoffColor, 4, "overbuilt", "Overbuilt", "Excess capability for the task");
        Add("tradeoff", tradeoffColor, 5, "high-overhead", "High Overhead", "Crew burden or cost above mission norms");
        Add("tradeoff", tradeoffColor, 6, "low-endurance", "Low Endurance", "Limited range or duration vs requirements");
        Add("tradeoff", tradeoffColor, 7, "high-footprint", "High Footprint", "Signature is a liability for the mission");
        Add("tradeoff", tradeoffColor, 8, "undermatch", "Undermatch", "Capability below ideal for the task");
        Add("tradeoff", tradeoffColor, 9, "single-point", "Single Point", "No redundancy for a critical function");

        return tags;
    }

    /// <summary>
    /// Builds the complete list of system-defined group tags — 51 tags across 5 pillars.
    /// <para>
    /// <b>Pillars:</b> doctrine (6), intent (14), potency (18), status (6), tradeoff (7).
    /// </para>
    /// <para>
    /// <b>AllowedScopes:</b> All seeded group tags use <c>"UserFleetGroup"</c>.
    /// </para>
    /// </summary>
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

        // ── doctrine (6 tags) — group structural role (flat) ─────────
        const string doctrineColor = "#8B66B8";
        Add("doctrine", doctrineColor, 1, "primary-arm", "Primary Arm", "Main operational capability of the fleet");
        Add("doctrine", doctrineColor, 2, "support-echelon", "Support Echelon", "Exists to sustain and enable other groups");
        Add("doctrine", doctrineColor, 3, "specialist-detachment", "Specialist Detachment", "Fills a narrow, unique capability gap");
        Add("doctrine", doctrineColor, 4, "carrier-element", "Carrier Element", "Built around a capital or command ship");
        Add("doctrine", doctrineColor, 5, "rapid-response", "Rapid Response", "Designed for fast deployment and flexible tasking");
        Add("doctrine", doctrineColor, 6, "reserve-force", "Reserve Force", "Held back for contingency and reinforcement");

        // ── intent:mission (10 tags) — group collective purpose ──────
        const string intentColor = "#C4706A";
        Add("intent:mission", intentColor, 1, "fight", "Fight", "Group's collective purpose is combat");
        Add("intent:mission", intentColor, 2, "patrol", "Patrol", "Group's collective purpose is presence and deterrence");
        Add("intent:mission", intentColor, 3, "escort", "Escort", "Group's collective purpose is asset protection");
        Add("intent:mission", intentColor, 4, "haul", "Haul", "Group's collective purpose is cargo transport");
        Add("intent:mission", intentColor, 5, "mine", "Mine", "Group's collective purpose is resource extraction");
        Add("intent:mission", intentColor, 6, "salvage", "Salvage", "Group's collective purpose is wreck processing");
        Add("intent:mission", intentColor, 7, "explore", "Explore", "Group's collective purpose is discovery");
        Add("intent:mission", intentColor, 8, "support", "Support", "Group's collective purpose is enabling other groups");
        Add("intent:mission", intentColor, 9, "command", "Command", "Group's collective purpose is fleet coordination");
        Add("intent:mission", intentColor, 10, "respond", "Respond", "Group's collective purpose is rapid reaction");

        // ── intent:org (4 tags) — same as ship ───────────────────────
        Add("intent:org", intentColor, 1, "org-op", "Org Op", "Deployed in coordinated org operations");
        Add("intent:org", intentColor, 2, "pickup-group", "Pickup Group", "Deployed with ad-hoc players");
        Add("intent:org", intentColor, 3, "solo-op", "Solo Op", "Deployed in personal, single-player sessions");
        Add("intent:org", intentColor, 4, "public", "Public", "Available for public crew or community events");

        // ── potency:capacity (5 tags) — same as ship ─────────────────
        const string potencyColor = "#3A9CB8";
        Add("potency:capacity", potencyColor, 1, "overwhelming", "Overwhelming", "Dramatically exceeds the group's needs for this mission");
        Add("potency:capacity", potencyColor, 2, "high", "High", "Meaningfully above what the mission requires");
        Add("potency:capacity", potencyColor, 3, "matched", "Matched", "Well-proportioned to the mission's demands");
        Add("potency:capacity", potencyColor, 4, "low", "Low", "Below what the mission ideally calls for");
        Add("potency:capacity", potencyColor, 5, "token", "Token", "Minimal contribution to the mission's core work");

        // ── potency:reach (4 tags) — same as ship ────────────────────
        Add("potency:reach", potencyColor, 1, "extended", "Extended", "Significantly expands operational range");
        Add("potency:reach", potencyColor, 2, "standard", "Standard", "Matches the group's baseline range");
        Add("potency:reach", potencyColor, 3, "limited", "Limited", "Constrains or is constrained by the group's range");
        Add("potency:reach", potencyColor, 4, "point", "Point", "Negligible range contribution; point-deployment only");

        // ── potency:resilience (4 tags) — no "expendable" for groups ─
        Add("potency:resilience", potencyColor, 1, "hardened", "Hardened", "Built to absorb and continue");
        Add("potency:resilience", potencyColor, 2, "robust", "Robust", "Above-average survivability");
        Add("potency:resilience", potencyColor, 3, "moderate", "Moderate", "Standard survivability for the mission context");
        Add("potency:resilience", potencyColor, 4, "fragile", "Fragile", "Vulnerable; loss significantly degrades the group");

        // ── potency:footprint (5 tags) — same as ship ────────────────
        Add("potency:footprint", potencyColor, 1, "dominant", "Dominant", "Impossible to ignore; commands attention");
        Add("potency:footprint", potencyColor, 2, "heavy", "Heavy", "Significant presence; will be noticed");
        Add("potency:footprint", potencyColor, 3, "standard", "Standard", "Unremarkable presence");
        Add("potency:footprint", potencyColor, 4, "light", "Light", "Below-average presence");
        Add("potency:footprint", potencyColor, 5, "minimal", "Minimal", "Near-invisible; avoids detection");

        // ── status (6 tags) — group readiness lifecycle (flat) ────────
        const string statusColor = "#B8913A";
        Add("status", statusColor, 1, "concept", "Concept", "Group exists as doctrine only; no ships assigned");
        Add("status", statusColor, 2, "assembling", "Assembling", "Ships being identified and assigned");
        Add("status", statusColor, 3, "undermanned", "Undermanned", "Ships assigned but crew insufficient");
        Add("status", statusColor, 4, "ready", "Ready", "Fully composed and deployment-capable");
        Add("status", statusColor, 5, "stood-down", "Stood Down", "Temporarily inactive; ships remain assigned");
        Add("status", statusColor, 6, "disbanded", "Disbanded", "Permanently dissolved");

        // ── tradeoff (7 tags) — accepted group gaps (flat) ───────────
        const string tradeoffColor = "#7A8499";
        Add("tradeoff", tradeoffColor, 1, "undercrew", "Undercrew", "Group has insufficient crew commitments");
        Add("tradeoff", tradeoffColor, 2, "underarmed", "Underarmed", "Insufficient offensive/defensive capability");
        Add("tradeoff", tradeoffColor, 3, "overbuilt", "Overbuilt", "Excess capability for the mission");
        Add("tradeoff", tradeoffColor, 4, "high-overhead", "High Overhead", "Logistical complexity above mission norms");
        Add("tradeoff", tradeoffColor, 5, "low-endurance", "Low Endurance", "Limited operational duration");
        Add("tradeoff", tradeoffColor, 6, "high-footprint", "High Footprint", "Signature is a liability");
        Add("tradeoff", tradeoffColor, 7, "single-point", "Single Point", "No redundancy for a critical function");

        return tags;
    }
}
