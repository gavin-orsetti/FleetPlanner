using FleetPlanner.Models;
using FleetPlanner.Services;

using FluentAssertions;

using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace FleetPlanner_Tests;

/// <summary>
/// Tests for <see cref="CachedShipDataService"/> — the caching decorator around the live API service.
/// <para>
/// <b>Test strategy:</b> Uses NSubstitute to mock the underlying <see cref="IShipDataService"/>
/// (the "live" service) and verifies that the caching layer:
/// <list type="bullet">
///   <item>Returns cached data on subsequent calls (avoiding unnecessary API hits).</item>
///   <item>Bypasses the cache when <c>forceRefresh: true</c> is passed.</item>
///   <item>Falls back to cached data when the live service throws <see cref="HttpRequestException"/>.</item>
/// </list>
/// </para>
/// <para>
/// <b>IDisposable pattern:</b> Each test creates a temporary SQLite database file with a unique
/// GUID name. <see cref="Dispose"/> deletes the file after the test completes, ensuring tests
/// don't leave temp files behind. xUnit calls Dispose automatically after each test.
/// </para>
/// <para>
/// <b>NSubstitute:</b> A mocking library for .NET. <c>Substitute.For&lt;T&gt;()</c> creates a
/// mock that implements the interface. <c>.Returns()</c> configures return values.
/// <c>.Received(N)</c> asserts a method was called exactly N times.
/// </para>
/// <para>
/// <b>FluentAssertions:</b> Provides readable assertion syntax: <c>result.Should().HaveCount(1)</c>
/// instead of xUnit's <c>Assert.Single(result)</c>. Produces better error messages on failure.
/// </para>
/// </summary>
/// <see href="https://nsubstitute.github.io/help/creating-a-substitute/"/>
/// <see href="https://fluentassertions.com/introduction"/>
/// <see href="https://xunit.net/docs/shared-context#constructor"/>
public class CachedShipDataService_Tests : IDisposable
{
    /// <summary>NSubstitute mock of the live API service.</summary>
    private readonly IShipDataService _mockLiveService;

    /// <summary>The system under test — wraps the mock live service with caching.</summary>
    private readonly CachedShipDataService _cachedService;

    /// <summary>Path to the temporary SQLite database used for this test run.</summary>
    private readonly string _dbPath;

    /// <summary>
    /// Test constructor — called before each test method by xUnit.
    /// <para>
    /// Creates a fresh mock, a unique temp database, and a new CachedShipDataService instance.
    /// This ensures each test starts with a clean cache and independent mock configuration.
    /// </para>
    /// </summary>
    public CachedShipDataService_Tests()
    {
        // SQLite native bindings must be initialized before any SQLite operations.
        SQLitePCL.Batteries_V2.Init();
        // Unique filename per test run to avoid database locking between parallel tests.
        _dbPath = Path.Combine(Path.GetTempPath(), $"CacheTest_{Guid.NewGuid()}.db3");
        _mockLiveService = Substitute.For<IShipDataService>();
        // Pass the concrete mock (not the interface) to break the circular dependency,
        // same pattern used in production (see MauiProgram.cs factory lambda).
        _cachedService = new CachedShipDataService(_mockLiveService, _dbPath);
    }

    /// <summary>Cleanup — delete the temporary SQLite database file.</summary>
    public void Dispose()
    {
        try { File.Delete(_dbPath); } catch { }
    }

    /// <summary>
    /// Verifies that after the first call populates the cache, a second call does NOT
    /// hit the underlying live service — it serves data from the local SQLite cache.
    /// </summary>
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
