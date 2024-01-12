using FleetPlanner.MVVM.Views;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FleetPlanner
{
    internal static class Routes
    {
        #region Basic
        public const string BackOne = "../";
        public const string BackTwo = "../../";
        public const string BackThree = "../../../";


        #endregion Basic

        #region Fleets
        // Home Page
        public const string MainPage_PageName = nameof( MainPage );
        public static readonly Type MainPage_PageType = typeof( MainPage );

        // New Fleet Page
        public const string MyFleets_NewFleet_PageName = nameof( MyFleets_AddNew_Page );
        public static readonly Type MyFleets_NewFleet_PageType = typeof( MyFleets_AddNew_Page );
        #endregion Fleets
    }
}
