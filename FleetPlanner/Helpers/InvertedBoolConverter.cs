using System.Globalization;

namespace FleetPlanner.Helpers;

/// <summary>
/// A MAUI value converter that inverts a boolean value for XAML data binding.
/// <para>
/// <b>Why needed?</b> MAUI's <c>IsVisible</c> property shows an element when <c>true</c>.
/// If a ViewModel exposes <c>IsLoading</c>, you'd need <c>IsVisible="{Binding IsLoading, Converter={StaticResource InvertedBoolConverter}}"</c>
/// to hide content while loading. Without this converter, you'd need a separate <c>IsNotLoading</c> property.
/// </para>
/// <para>
/// <b>IValueConverter:</b> MAUI's binding engine calls <see cref="Convert"/> when reading
/// from the ViewModel (source → target), and <see cref="ConvertBack"/> when writing back
/// (target → source, for two-way bindings). Both directions invert the boolean.
/// </para>
/// <para>
/// <b>Registration:</b> Converters must be declared as XAML resources (typically in
/// <c>App.xaml</c> or the page's <c>Resources</c>) and referenced via <c>{StaticResource}</c>.
/// </para>
/// </summary>
/// <see href="https://learn.microsoft.com/en-us/dotnet/maui/fundamentals/data-binding/converters"/>
public class InvertedBoolConverter : IValueConverter
{
    /// <summary>
    /// Inverts a boolean value (source → target direction).
    /// </summary>
    /// <param name="value">The source value from the ViewModel binding.</param>
    /// <param name="targetType">The target property type (expected: bool).</param>
    /// <param name="parameter">Optional converter parameter (not used).</param>
    /// <param name="culture">Culture info for locale-aware conversions (not used for booleans).</param>
    /// <returns>The inverted boolean, or the original value if it's not a bool.</returns>
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool b)
            return !b;
        return value;
    }

    /// <summary>
    /// Inverts a boolean value (target → source direction, for two-way bindings).
    /// </summary>
    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool b)
            return !b;
        return value;
    }
}
