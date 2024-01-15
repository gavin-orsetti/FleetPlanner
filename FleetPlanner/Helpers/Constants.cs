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
