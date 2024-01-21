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

        public enum InsuranceType { Lifetime, Three_Month, Six_Month, Twelve_Month, One_Hundred_Twenty_Month }

        public enum ShipManufacturer
        {
            Aegis_Dynamics,
            Anvil_Aerospace,
            Aopoa,
            Argo_Astronautics,
            Banu,
            Consolidated_Outland,
            Crusader_Industries,
            Drake_Interplanetary,
            Esperia,
            Gatac_Manufacture,
            Greycat_Industrial,
            Kruger_Intergalactic,
            Mirai,
            MISC,
            Origin_Jumpworks,
            Roberts_Space_Industries,
            Tumbril_Land_Systems,
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
