using FleetPlanner.Graph;
using FleetPlanner.Models;
using FleetPlanner.Repositories;

namespace FleetPlanner.Services;

/// <summary>
/// Builds the in-memory <see cref="FleetGraph"/> by loading all user data from SQLite
/// repositories and resolving cross-entity relationships (ship→catalogue, ship→tags,
/// group→tags, group→member ships).
///
/// <para><b>Build sequence (order matters):</b>
/// <list type="number">
///   <item>Load all non-archived <see cref="OwnedShip"/> records.</item>
///   <item>Load the full ship catalogue from <see cref="IShipDataService"/> and build an ID lookup.</item>
///   <item>Load all <see cref="TagDefinition"/> records (including archived, for tag resolution).</item>
///   <item>Load all non-archived <see cref="UserFleetGroup"/> records.</item>
///   <item>For each OwnedShip, load its <see cref="OwnedShipTag"/> records and resolve each to a
///     <see cref="TagNode"/>. Tags are partitioned into global (ContextType == null) and contextual
///     (ContextType == "group") buckets on the <see cref="ShipNode"/>.</item>
///   <item>For each group, load its <see cref="UserFleetGroupTag"/> records and resolve to TagNodes.
///     Then determine group membership: a ship is a member if it has ANY contextual tags for that group.</item>
/// </list></para>
///
/// <para><b>Contextual tags and group membership:</b> A ship belongs to a group if and only if
/// it has at least one <see cref="OwnedShipTag"/> with <c>ContextType == "group"</c> and
/// <c>ContextId == group.Id</c>. There is no separate membership table — the contextual tag IS
/// the membership. This means adding any group-scoped tag to a ship automatically makes it a
/// group member.</para>
///
/// <para><b>Cache invalidation contract:</b> The graph is cached in <c>_cachedGraph</c>.
/// <see cref="InvalidateCache"/> sets it to null. Any code that mutates ships, tags, or groups
/// should call <c>InvalidateCache()</c> afterwards so the next <c>GetOrRebuildAsync()</c> call
/// produces a fresh graph. Currently, cache invalidation is the caller's responsibility (typically
/// the ViewModel layer).</para>
///
/// <para><b>Thread safety:</b> Not thread-safe. The <c>_cachedGraph</c> field is read/written
/// without synchronisation. This is acceptable in MAUI where ViewModel calls are serialised
/// on the UI thread.</para>
/// </summary>
public class GraphBuildService : IGraphBuildService
{
    private readonly IOwnedShipRepository _ownedShipRepo;
    private readonly IOwnedShipTagRepository _ownedShipTagRepo;
    private readonly ITagRepository _tagRepo;
    private readonly IUserFleetGroupRepository _groupRepo;
    private readonly IUserFleetGroupTagRepository _groupTagRepo;
    private readonly IShipDataService _shipDataService;

    private FleetGraph? _cachedGraph;

    /// <summary>
    /// Constructor — receives all required repositories and services from DI.
    /// </summary>
    public GraphBuildService(
        IOwnedShipRepository ownedShipRepo,
        IOwnedShipTagRepository ownedShipTagRepo,
        ITagRepository tagRepo,
        IUserFleetGroupRepository groupRepo,
        IUserFleetGroupTagRepository groupTagRepo,
        IShipDataService shipDataService)
    {
        _ownedShipRepo = ownedShipRepo;
        _ownedShipTagRepo = ownedShipTagRepo;
        _tagRepo = tagRepo;
        _groupRepo = groupRepo;
        _groupTagRepo = groupTagRepo;
        _shipDataService = shipDataService;
    }

    /// <inheritdoc/>
    public async Task<FleetGraph> BuildGraphAsync()
    {
        // 1. Load all non-archived OwnedShips
        var ownedShips = await _ownedShipRepo.GetAllOwnedShipsAsync();

        // 2. Load cached ship catalogue and build lookup
        var catalogueShips = await _shipDataService.GetAllShipsAsync();
        var catalogueLookup = catalogueShips.ToDictionary(s => s.Id);

        // 3. Load all tags for lookup
        var allTagDefs = await _tagRepo.GetAllTagsAsync(includeArchived: true);
        var tagLookup = allTagDefs.ToDictionary(t => t.Key);

        // 4. Load all non-archived groups and their tags
        var groups = await _groupRepo.GetAllGroupsAsync();

        // 5. Build ShipNodes
        var shipNodes = new List<ShipNode>();
        var allTagNodes = new List<TagNode>();

        foreach (var owned in ownedShips)
        {
            catalogueLookup.TryGetValue(owned.ShipId, out var catalogueShip);
            if (catalogueShip is null)
                continue; // skip orphaned OwnedShips with no catalogue match

            var shipNode = new ShipNode
            {
                OwnedShipId = owned.Id,
                OwnedShip = owned,
                CatalogueShip = catalogueShip
            };

            // Load all tags for this ship
            var shipTags = await _ownedShipTagRepo.GetTagsForOwnedShipAsync(owned.Id);

            foreach (var st in shipTags)
            {
                tagLookup.TryGetValue(st.TagKey, out var tagDef);

                // Contextual tags establish group membership even when their
                // definition is missing (e.g. the "status:placeholder" sentinel
                // used to mark group membership before real tags are assigned).
                // Ensure the ContextualTags dictionary is populated so the ship
                // counts as a group member regardless.
                if (st.ContextType == "group" && st.ContextId.HasValue)
                {
                    if (!shipNode.ContextualTags.ContainsKey(st.ContextId.Value))
                        shipNode.ContextualTags[st.ContextId.Value] = new List<TagNode>();
                }

                if (tagDef is null) continue;

                var tagNode = new TagNode
                {
                    Definition = tagDef,
                    Weight = st.Weight,
                    IsContextual = st.ContextType is not null,
                    ContextGroupId = st.ContextType == "group" ? st.ContextId : null
                };

                allTagNodes.Add(tagNode);

                if (st.ContextType is null)
                {
                    // Global tag
                    shipNode.GlobalTags.Add(tagNode);
                }
                else if (st.ContextType == "group" && st.ContextId.HasValue)
                {
                    // Contextual tag scoped to a group — entry was created above
                    shipNode.ContextualTags[st.ContextId.Value].Add(tagNode);
                }
            }

            shipNodes.Add(shipNode);
        }

        // 6. Build GroupNodes
        var groupNodes = new List<GroupNode>();
        foreach (var group in groups)
        {
            var groupNode = new GroupNode { Group = group };

            // Load group doctrine sub-dimension and focus tags
            var groupTags = await _groupTagRepo.GetTagsForGroupAsync(group.Id);
            foreach (var gt in groupTags)
            {
                tagLookup.TryGetValue(gt.TagKey, out var tagDef);
                if (tagDef is null) continue;

                var tagNode = new TagNode
                {
                    Definition = tagDef,
                    Weight = gt.Weight,
                    IsContextual = false,
                    ContextGroupId = null
                };
                groupNode.DoctrineAndFocusTags.Add(tagNode);
                allTagNodes.Add(tagNode);
            }

            // Find member ships: ships with contextual tags for this group
            groupNode.MemberShips = shipNodes
                .Where(sn => sn.ContextualTags.ContainsKey(group.Id))
                .ToList();

            groupNodes.Add(groupNode);
        }

        var graph = new FleetGraph
        {
            Ships = shipNodes,
            Groups = groupNodes,
            AllTags = allTagNodes,
            BuiltAt = DateTime.UtcNow
        };

        _cachedGraph = graph;
        return graph;
    }

    /// <inheritdoc/>
    public async Task<FleetGraph> GetOrRebuildAsync()
    {
        if (_cachedGraph is not null)
            return _cachedGraph;

        return await BuildGraphAsync();
    }

    /// <inheritdoc/>
    public void InvalidateCache()
    {
        _cachedGraph = null;
    }
}
