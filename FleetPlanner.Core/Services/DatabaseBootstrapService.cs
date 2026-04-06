using FleetPlanner.Models;

using SQLite;

namespace FleetPlanner.Services;

/// <summary>
/// Creates all database tables and seeds the system tag taxonomy on first run.
/// Must be called at startup after <c>builder.Build()</c> but before the app is returned.
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
    /// Creates all tables and seeds system tags if not already present.
    /// Idempotent — safe to call on every startup.
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
    /// </summary>
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
