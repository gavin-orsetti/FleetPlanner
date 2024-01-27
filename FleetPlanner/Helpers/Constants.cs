using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FleetPlanner.Helpers
{
    public static class Constants
    {
        #region Strings
        #region Fleet Defaults
        public const string DefaultFleetName = "New Fleet";
        public const string DefaultAffiliation = "None";
        public const string DefaultAreaOfOperation = "Stanton";
        public const string DefaultManifesto = "Replace me";
        #endregion Fleet Defaults
        #endregion Strings

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
