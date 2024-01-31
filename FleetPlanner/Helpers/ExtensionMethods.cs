using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace FleetPlanner.Helpers
{
    public static partial class ExtensionMethods
    {
        /// <summary>
        /// Returns a string representation of the insurance type with each word split out.
        /// </summary>
        /// <param name="enumType"></param>
        /// <returns></returns>
        public static string ToSplitString<T>( this T enumType ) where T : Enum
        {
            return enumType.ToString().SplitCamelCase();
        }

        /// <summary>
        /// Removes the spaces from a string
        /// </summary>
        /// <param name="s"></param>
        /// <returns></returns>
        public static string RemoveSpaces( this string s )
        {
            return s.Replace( " ", "" ).Trim();
        }

        /// <summary>
        /// Splits a CamelCase or pascalCase string into separate words (Camel Case, pascal Case)
        /// </summary>
        /// <param name="source">The source string</param>
        /// <param name="delimiter">The delimiter char to split on. This allows you to also split snake_case and skewer-case strings.</param>
        /// <returns></returns>
        public static string SplitCamelCase( this string source, char delimiter = ' ' )
        {
            return string.Join( delimiter, SplitOnCapitalLetters().Split( source ) );
        }

        [GeneratedRegex( @"(?<!^)(?=[A-Z])" )]
        private static partial Regex SplitOnCapitalLetters();
    }
}
