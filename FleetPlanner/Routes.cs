using FleetPlanner.MVVM.ViewModels;
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

        public class CommonQueryParams
        {
            public const string Id = "Id";
        }


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

        public class FleetQueryParams : CommonQueryParams
        {
            public const string PopulatedViewModel = "PopulatedViewModel";
            public const string EditViewModel = "EditViewModel";
        }

        #endregion

        #endregion Fleets

        #region Task Groups
        // Add New Task Group
        public const string TaskGroup_AddNew_PageName = nameof( TaskGroup_AddNew_Page );
        public static readonly Type TaskGroup_AddNew_PageType = typeof( TaskGroup_AddNew_Page );

        #region Query Parameters
        public class TaskGroupQueryParams : CommonQueryParams
        {
            public const string FleetId = "FleetId";
            public const string Fleet = "Fleet";
        }
        #endregion Query Parameters

        #endregion Task Groups
    }
}
