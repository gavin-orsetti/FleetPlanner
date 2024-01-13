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

        // Fleet Page
        public const string FleetPage_PageName = nameof( FleetPage );
        public static readonly Type FleetPage_PageType = typeof( FleetPage );

        // Edit Fleet Page
        public const string EditFleetPage_PageName = nameof( Fleet_Edit_Page );
        public static readonly Type EditFleetPage_PageType = typeof( Fleet_Edit_Page );

        #region Query Parameters
        public const string id = "id=";
        #endregion

        #endregion Fleets
    }
}
