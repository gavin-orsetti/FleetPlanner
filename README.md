# FleetPlanner

A Star Citizen fleet planning app for Android. FleetPlanner helps players manage their ship collection, organise ships into operation groups, tag ships with roles and doctrines, and get intelligent recommendations about fleet composition gaps.

This is an unofficial Star Citizen fan project, not affiliated with the Cloud Imperium group of companies.

## Key Features

- **Ship browser** with live catalogue data fetched from the starcitizen.tools Semantic MediaWiki API, cached locally in SQLite for offline use
- **Owned ship collection** where each ship can have a callsign, acquisition type (real money / aUEC), price paid, and free-form notes
- **Tag system** with 58 seeded system tags across 8 categories (role, doctrine, status, crew, capability, preference, constraint) plus user-defined custom tags. Tags can be global (applied to a ship everywhere) or contextual (scoped to a specific group)
- **Fleet groups** as an optional organisational layer; ships can belong to multiple groups with different contextual role tags per group
- **Recommendation engine** running 10 analytical patterns entirely on-device:
  - CapabilityGap, Redundancy, Complement, UnderDescribedShip, UnassignedShip, GroupCoherence, AccountRoleDistribution, DoctrineMismatch, RemoveFromGroup, CrewEfficiency
- **Dashboard** with KPI cards (total ships, pledge value, roles covered, groups), acquisition breakdown, role composition pie chart, and top-10 value distribution bar chart

All recommendation logic runs on-device -- no server component.

## Architecture

### Reference Layer
Immutable ship catalogue fetched from the starcitizen.tools SMW API and cached in SQLite. `CachedShipDataService` wraps `ShipDataService` with a 24-hour cache window. The `Ship` table is read-only game data; `ShipCacheMetadata` tracks the last fetch timestamp.

### User Data Layer
All user data is stored in SQLite via sqlite-net-pcl:

| Table | Purpose |
|-------|---------|
| `OwnedShip` | The domain root -- represents a ship the player owns |
| `TagDefinition` | System and custom tags with category, allowed scopes, and stable slug keys |
| `OwnedShipTag` | Links an OwnedShip to a TagDefinition (global or contextual per group) |
| `UserFleetGroup` | An organisational group with name, description, focus, and crew target |
| `UserFleetGroupTag` | Links a group to doctrine/focus TagDefinitions |
| `AppMetadata` | Key-value store for app-level metadata (bootstrap version, etc.) |

### Graph Layer
An in-memory `FleetGraph` built on demand from SQLite data by `GraphBuildService`. The graph contains `ShipNode`, `GroupNode`, and `TagNode` objects with resolved relationships. It is rebuilt after any mutation (cache invalidation). The recommendation engine operates entirely on this graph -- no I/O during analysis.

### App Layer
.NET MAUI MVVM with CommunityToolkit.Mvvm, Shell navigation, and LiveCharts for data visualisation.

## Project Structure

```
FleetPlanner/              MAUI head project (Android target)
  ViewModels/              9 ViewModels (Dashboard, ShipBrowser, ShipDetail,
                           OwnedShipLibrary, OwnedShipEditor, GroupOverview,
                           GroupDetail, Recommendations, Settings)
  Views/                   9 XAML pages + code-behinds
  Helpers/                 Converters, constants, extension methods
  MauiProgram.cs           DI composition root

FleetPlanner.Core/         Platform-agnostic business logic
  Models/                  OwnedShip, Ship, TagDefinition, OwnedShipTag,
                           UserFleetGroup, UserFleetGroupTag, Recommendation, etc.
  Repositories/            5 repository interfaces + implementations (SQLite)
  Services/                ShipDataService, CachedShipDataService,
                           DatabaseBootstrapService, GraphBuildService,
                           RecommendationService, RecommendationWeights
  Graph/                   FleetGraph, ShipNode, GroupNode, TagNode
  Helpers/                 QueryParameters

FleetPlanner_Tests/        xUnit test project (39 tests)
                           Covers repositories, graph build, recommendation
                           engine, caching, and view model logic
```

## Building

- Requires .NET 9 SDK
- Target: `net9.0-android` (minimum Android API 28)
- `dotnet build FleetPlanner/FleetPlanner.csproj`
- `dotnet test FleetPlanner_Tests/FleetPlanner_Tests.csproj`
- CI: GitHub Actions on the `FleetplannerV2` branch

## Design Decisions

- **No EF Core** -- sqlite-net-pcl only, keeping the dependency footprint minimal and avoiding EF's MAUI compatibility issues
- **OwnedShip as domain root** -- all user data hangs off OwnedShip rather than a Fleet parent entity; groups are optional overlays, not containers
- **Graph rebuilt in memory** -- no graph database; the `FleetGraph` is rebuilt from SQLite on demand and cached until invalidated, keeping the architecture simple
- **Fixed scoring weights in Phase 1** -- recommendation weights are named constants in `RecommendationWeights.cs` for future exposure via a settings UI
- **Tag keys are stable slugs** -- tag keys (e.g. `role:mining`, `doctrine:stealth`) never change once shipped; display names can change freely without breaking saved data
