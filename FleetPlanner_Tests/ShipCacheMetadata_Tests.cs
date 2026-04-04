using FleetPlanner.Models;

using FluentAssertions;

namespace FleetPlanner_Tests;

public class ShipCacheMetadata_Tests
{
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
