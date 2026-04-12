using System.Text.RegularExpressions;

namespace FleetPlanner.Helpers;

/// <summary>
/// String manipulation utilities for tag key generation and display formatting.
/// </summary>
public static partial class StringHelpers
{
    /// <summary>
    /// Converts a display name into a lowercase hyphenated slug suitable for tag keys.
    /// Strips all characters except letters, digits, and hyphens.
    /// </summary>
    /// <example><c>Slugify("My Ship Role")</c> returns <c>"my-ship-role"</c>.</example>
    public static string Slugify(string input) =>
        SlugRegex().Replace(
            input.Trim().ToLowerInvariant().Replace(" ", "-"),
            "");

    [GeneratedRegex(@"[^a-z0-9\-]")]
    private static partial Regex SlugRegex();
}
