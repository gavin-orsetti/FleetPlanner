using System.Globalization;

namespace FleetPlanner.Helpers;

/// <summary>
/// A MAUI value converter that returns <c>true</c> when the bound value is not null.
/// <para>
/// <b>Use case:</b> Show/hide UI elements based on whether an object is loaded.
/// Example: <c>IsVisible="{Binding Ship, Converter={StaticResource IsNotNullConverter}}"</c>
/// shows the ship detail panel only after the Ship object has been loaded from the cache.
/// </para>
/// <para>
/// <b>One-way only:</b> <see cref="ConvertBack"/> throws <see cref="NotSupportedException"/>
/// because there's no meaningful way to convert a boolean back to the original object.
/// This converter is only useful for one-way bindings (ViewModel → View).
/// </para>
/// </summary>
/// <see href="https://learn.microsoft.com/en-us/dotnet/maui/fundamentals/data-binding/converters"/>
public class IsNotNullConverter : IValueConverter
{
    /// <summary>
    /// Returns <c>true</c> if the value is not null; <c>false</c> otherwise.
    /// </summary>
    /// <param name="value">The source value from the ViewModel binding.</param>
    /// <param name="targetType">The target property type (expected: bool).</param>
    /// <param name="parameter">Optional converter parameter (not used).</param>
    /// <param name="culture">Culture info (not used).</param>
    /// <returns><c>true</c> if the value is not null.</returns>
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value is not null;
    }

    /// <summary>
    /// Not supported — this is a one-way converter. Converting a boolean back to an
    /// arbitrary object is not meaningful.
    /// </summary>
    /// <exception cref="NotSupportedException">Always thrown.</exception>
    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}
