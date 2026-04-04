using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace FleetPlanner.Helpers
{
    /// <summary>
    /// Extension methods for string formatting and enum display throughout the app.
    /// <para>
    /// <b>partial class + [GeneratedRegex]:</b> This class is declared <c>partial</c> because it uses
    /// .NET 7+'s <see cref="GeneratedRegexAttribute"/>. The source generator creates a compile-time
    /// optimized regex implementation in a separate partial file, avoiding the runtime overhead of
    /// <c>new Regex(...)</c>. This is the recommended pattern for regexes in modern .NET.
    /// </para>
    /// </summary>
    /// <see href="https://learn.microsoft.com/en-us/dotnet/standard/base-types/regular-expression-source-generators"/>
    public static partial class ExtensionMethods
    {
        /// <summary>
        /// Converts an enum value to a human-readable string by splitting its PascalCase name.
        /// <para>
        /// Example: <c>FleetFocus.BountyHunting.ToSplitString()</c> → <c>"Bounty Hunting"</c>.
        /// Used in the UI to display enum values as readable labels without maintaining a
        /// separate display-name dictionary.
        /// </para>
        /// </summary>
        /// <typeparam name="T">Any enum type.</typeparam>
        /// <param name="enumType">The enum value to format.</param>
        /// <returns>The enum name with spaces inserted before each capital letter.</returns>
        public static string ToSplitString<T>( this T enumType ) where T : Enum
        {
            return enumType.ToString().SplitCamelCase();
        }

        /// <summary>
        /// Removes all spaces from a string and trims leading/trailing whitespace.
        /// </summary>
        /// <param name="s">The source string.</param>
        /// <returns>The string with all spaces removed.</returns>
        public static string RemoveSpaces( this string s )
        {
            return s.Replace( " ", "" ).Trim();
        }

        /// <summary>
        /// Splits a PascalCase or camelCase string into separate words.
        /// <para>
        /// Uses a regex lookahead <c>(?&lt;!^)(?=[A-Z])</c> that matches positions before
        /// uppercase letters (but not at the start of the string), then joins with the delimiter.
        /// Example: <c>"BountyHunting".SplitCamelCase()</c> → <c>"Bounty Hunting"</c>.
        /// </para>
        /// </summary>
        /// <param name="source">The source string (e.g., "CamelCase").</param>
        /// <param name="delimiter">The character to insert between words. Defaults to space.</param>
        /// <returns>The string with words separated by the delimiter.</returns>
        public static string SplitCamelCase( this string source, char delimiter = ' ' )
        {
            return string.Join( delimiter, SplitOnCapitalLetters().Split( source ) );
        }

        /// <summary>
        /// Source-generated regex that matches positions before capital letters (but not at string start).
        /// <para>
        /// <b>[GeneratedRegex]</b> tells the .NET source generator to create a compile-time optimized
        /// Regex implementation. The generated code is faster than runtime-compiled regexes and avoids
        /// allocating a <c>Regex</c> object. The method must be <c>partial</c> — the generator provides
        /// the implementation.
        /// </para>
        /// </summary>
        /// <see href="https://learn.microsoft.com/en-us/dotnet/standard/base-types/regular-expression-source-generators"/>
        [GeneratedRegex( @"(?<!^)(?=[A-Z])" )]
        private static partial Regex SplitOnCapitalLetters();
    }
}
