using FleetPlanner.Models;
using FleetPlanner.Services;

using FluentAssertions;

using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace FleetPlanner_Tests;

public class CachedShipDataService_Tests : IDisposable
{
    private readonly IShipDataService _mockLiveService;
    private readonly CachedShipDataService _cachedService;
    private readonly string _dbPath;

    public CachedShipDataService_Tests()
    {
        SQLitePCL.Batteries_V2.Init();
        _dbPath = Path.Combine(Path.GetTempPath(), $"CacheTest_{Guid.NewGuid()}.db3");
        _mockLiveService = Substitute.For<IShipDataService>();
        _cachedService = new CachedShipDataService(_mockLiveService, _dbPath);
    }

    public void Dispose()
    {
        try { File.Delete(_dbPath); } catch { }
    }

    [Fact]
    public async Task GetAllShipsAsync_CacheHit_DoesNotCallUnderlyingServiceAgain()
    {
        var ships = new List<Ship>
        {
            new() { Id = 1, Name = "Arrow", Role = "Fighter", PriceUsd = 75 }
        };
        _mockLiveService.GetAllShipsAsync(Arg.Any<bool>()).Returns(ships);

        // First call — cache is empty, will hit the live service
        var first = await _cachedService.GetAllShipsAsync(forceRefresh: true);
        first.Should().HaveCount(1);

        // Second call — cache is populated, should NOT call live service again
        var second = await _cachedService.GetAllShipsAsync();
        second.Should().HaveCount(1);

        await _mockLiveService.Received(1).GetAllShipsAsync(Arg.Any<bool>());
    }

    [Fact]
    public async Task GetAllShipsAsync_ForceRefresh_CallsUnderlyingService()
    {
        var ships = new List<Ship>
        {
            new() { Id = 1, Name = "Arrow", Role = "Fighter", PriceUsd = 75 }
        };
        _mockLiveService.GetAllShipsAsync(Arg.Any<bool>()).Returns(ships);

        await _cachedService.GetAllShipsAsync(forceRefresh: true);
        await _cachedService.GetAllShipsAsync(forceRefresh: true);

        await _mockLiveService.Received(2).GetAllShipsAsync(Arg.Any<bool>());
    }

    [Fact]
    public async Task GetAllShipsAsync_ServiceThrowsHttpRequestException_ReturnsCachedData()
    {
        var ships = new List<Ship>
        {
            new() { Id = 1, Name = "Arrow", Role = "Fighter", PriceUsd = 75 }
        };

        // First call succeeds and populates cache
        _mockLiveService.GetAllShipsAsync(Arg.Any<bool>()).Returns(ships);
        await _cachedService.GetAllShipsAsync(forceRefresh: true);

        // Second call — live service throws, should return cached data
        _mockLiveService.GetAllShipsAsync(Arg.Any<bool>()).Throws(new HttpRequestException("Network error"));
        var result = await _cachedService.GetAllShipsAsync(forceRefresh: true);

        result.Should().HaveCount(1);
        result.First().Name.Should().Be("Arrow");
    }
}
