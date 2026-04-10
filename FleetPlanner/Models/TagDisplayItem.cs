namespace FleetPlanner.Models;

/// <summary>
/// Lightweight display DTO for rendering a tag chip in the UI.
/// Maps a <see cref="FleetPlanner.Models.TagDefinition"/> assignment to the
/// properties needed by tag chip templates (name, category, colour, weight).
/// Lives in the MAUI head project (not Core) because it references
/// <see cref="Microsoft.Maui.Graphics.Color"/>.
/// </summary>
public class TagDisplayItem
{
    /// <summary>The <see cref="TagDefinition.Key"/> (e.g. "role:activity:escort").</summary>
    public string TagKey { get; set; } = string.Empty;

    /// <summary>Human-readable name shown on the chip.</summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>Tag category (role, doctrine, status, etc.).</summary>
    public string Category { get; set; } = string.Empty;

    /// <summary>Hex colour from the <see cref="TagDefinition.ColorHex"/>.</summary>
    public string? ColorHex { get; set; }

    /// <summary>Priority weight (1 = Primary, 2 = Secondary, 3 = Tertiary).</summary>
    public int Weight { get; set; } = 1;

    /// <summary>Human-readable weight label for role tags.</summary>
    public string WeightLabel => Weight switch { 1 => "Primary", 2 => "Secondary", 3 => "Tertiary", _ => "" };

    /// <summary>Whether this tag is in an intent sub-dimension category (shows weight badge).</summary>
    public bool IsIntentTag => Category.StartsWith("intent:", StringComparison.Ordinal);

    /// <summary>Resolved chip colour (full opacity, used for border/stroke) — falls back to custom grey if no <see cref="ColorHex"/> is set.</summary>
    public Color ChipColor => ColorHex is not null ? Color.FromArgb(ColorHex) : Color.FromArgb("#6B7A8B");

    /// <summary>Chip fill colour with 30% alpha for unselected background.</summary>
    public Color ChipFillColor => Color.FromArgb("4D" + (ColorHex?.TrimStart('#') ?? "6B7A8B"));
}
