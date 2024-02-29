using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FleetPlanner.Helpers
{
    public static class Constants
    {
        #region User Values

        public static Currency UserCurrency { get; set; }

        #endregion User Values

        #region Strings
        #region Fleet Defaults
        public const string DefaultFleetName = "New Fleet";
        public const string DefaultAffiliation = "None";
        public const string DefaultAreaOfOperation = "Stanton";
        public const string DefaultManifesto = "Replace me";
        #endregion Fleet Defaults

        #region Ship Balance Sheet Defaults
        public const string DefaultBalanceSheetItemKey = "New Item";
        #endregion Ship Balance Sheet Defaults
        #endregion Strings

        #region Colors
        private static Color? tertiary;
        public static Color Tertiary
        {
            get
            {
                if( tertiary != null )
                {
                    return tertiary;
                }
                else if( Application.Current.Resources.TryGetValue( "Tertiary", out object c ) )
                {
                    tertiary = (Color)c;
                    return tertiary;
                }
                else
                {
                    return ColorResourceLookupFailed;
                }
            }
        }

        private static Color? onTertiary;
        public static Color OnTertiary
        {
            get
            {
                if( onTertiary != null )
                {
                    return onTertiary;
                }
                else if( Application.Current.Resources.TryGetValue( "OnTertiary", out object c ) )
                {
                    onTertiary = (Color)c;
                    return onTertiary;
                }
                else
                {
                    return ColorResourceLookupFailed;
                }
            }
        }

        private static Color? warning;
        public static Color Warning
        {
            get
            {
                if( warning != null )
                {
                    return warning;
                }
                else if( Application.Current.Resources.TryGetValue( "Warning", out object c ) )
                {
                    warning = (Color)c;
                    return warning;
                }
                else
                {
                    return ColorResourceLookupFailed;
                }
            }
        }

        private static Color? onWarning;
        public static Color OnWarning
        {
            get
            {
                if( onWarning != null )
                {
                    return onWarning;
                }
                else if( Application.Current.Resources.TryGetValue( "OnWarning", out object c ) )
                {
                    onWarning = (Color)c;
                    return onWarning;
                }
                else
                {
                    return ColorResourceLookupFailed;
                }
            }
        }

        private static Color? error;
        public static Color Error
        {
            get
            {
                if( error != null )
                {
                    return error;
                }
                else if( Application.Current.Resources.TryGetValue( "Error", out object c ) )
                {
                    error = (Color)c;
                    return error;
                }
                else
                {
                    return ColorResourceLookupFailed;
                }
            }
        }

        private static Color? onError;
        public static Color OnError
        {
            get
            {
                if( onError != null )
                {
                    return onError;
                }
                else if( Application.Current.Resources.TryGetValue( "OnError", out object c ) )
                {
                    onError = (Color)c;
                    return onError;
                }
                else
                {
                    return ColorResourceLookupFailed;
                }
            }
        }

        #region Fix Me Colors
        public static readonly Color ColorResourceLookupFailed = Colors.HotPink;
        #endregion Fix Me Colors
        #endregion Colors

        #region Enums
        public enum QueryFlag { Async, Sycn }
        public enum Currency { UEC, USD, GBP, EUR }

        public enum InsuranceType { ThreeMonth, SixMonth, TwelveMonth, OneHundredTwentyMonth, Lifetime }



        public enum ShipManufacturer
        {
            AegisDynamics = 1,
            Anvil = 2,
            Aopoa = 3,
            Argo = 4,
            Banu = 5,
            ConsolidatedOutland = 6,
            Crusader = 7,
            Drake = 8,
            Esperia = 9,
            Gatac = 10,
            Greycat = 11,
            Kruger = 12,
            Mirai = 13,
            MISC = 14,
            Origin = 15,
            RSI = 16,
            Tumbril = 17
        }
        #endregion

        #region SQLite Query Strings

        public const string SELECT_ALL = "SELECT *";
        public const string FROM = "FROM";
        public const string WHERE = "WHERE";
        public const string ID = "Id";
        public const string EQUALS = "=";

        #endregion SQLite Query Strings

    }
}
