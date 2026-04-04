using FleetPlanner.Models;

using FluentAssertions;

namespace FleetPlanner_Tests;

/// <summary>
/// Unit tests for <see cref="ShipCacheMetadata"/> — tests the cache expiry logic.
/// <para>
/// <b>Pure logic tests:</b> These tests verify the cache TTL (time-to-live) calculation
/// without touching any database. They create <c>ShipCacheMetadata</c> instances with
/// different <c>LastFetched</c> timestamps and check whether the cache would be considered expired.
/// </para>
/// <para>
/// <b>Note:</b> The expiry check is done inline here (not as a method on ShipCacheMetadata)
/// because the model is a simple data class. The actual expiry check in production lives in
/// <see cref="FleetPlanner.Services.CachedShipDataService"/>.
/// </para>
/// </summary>
public class ShipCacheMetadata_Tests
{
    /// <summary>Cache TTL matching the production value in CachedShipDataService (24 hours).</summary>
    private static readonly TimeSpan CacheTtl = TimeSpan.FromHours(24);

    [Fact]
    public void IsExpired_CacheOlderThanTtl_ReturnsTrue()
    {
        var meta = new ShipCacheMetadata
        {
            Key = "ship_cache",
            LastFetched = DateTime.UtcNow.AddHours(-25)
        };

        var isExpired = (DateTime.UtcNow - meta.LastFetched) > CacheTtl;

        isExpired.Should().BeTrue();
    }

    [Fact]
    public void IsExpired_CacheFresherThanTtl_ReturnsFalse()
    {
        var meta = new ShipCacheMetadata
        {
            Key = "ship_cache",
            LastFetched = DateTime.UtcNow.AddHours(-1)
        };

        var isExpired = (DateTime.UtcNow - meta.LastFetched) > CacheTtl;

        isExpired.Should().BeFalse();
    }
}
