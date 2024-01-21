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
        public const string Main_Page_PageName = nameof( MainPage );
        public static readonly Type Main_Page_PageType = typeof( MainPage );

        // New Fleet Page
        public const string MyFleets_NewFleetPage_PageName = nameof( MyFleets_AddNew_Page );
        public static readonly Type MyFleets_NewFleetPage_PageType = typeof( MyFleets_AddNew_Page );

        // Fleet Page
        public const string Fleet_Page_PageName = nameof( FleetPage );
        public static readonly Type Fleet_Page_PageType = typeof( FleetPage );

        // Edit Fleet Page
        public const string Fleet_EditPage_PageName = nameof( Fleet_Edit_Page );
        public static readonly Type Fleet_EditPage_PageType = typeof( Fleet_Edit_Page );



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
        public const string TaskGroup_AddNewPage_PageName = nameof( TaskGroup_AddNew_Page );
        public static readonly Type TaskGroup_AddNewPage_PageType = typeof( TaskGroup_AddNew_Page );

        // Task Group Page
        public const string TaskGroup_Page_PageName = nameof( TaskGroupPage );
        public static readonly Type TaskGroup_Page_PageType = typeof( TaskGroupPage );

        // Task Group Page
        public const string TaskGroup_EditPage_PageName = nameof( TaskGroup_Edit_Page );
        public static readonly Type TaskGroup_EditPage_PageType = typeof( TaskGroup_Edit_Page );

        #region Query Parameters
        public class TaskGroupQueryParams : CommonQueryParams
        {
            public const string FleetId = "FleetId";
            public const string Fleet = "Fleet";
            public const string TaskGroup = "Task_Group";
            public const string TaskGroupId = "TaskGroupId";
        }
        #endregion Query Parameters

        #endregion Task Groups
    }
}
