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
            public const string Refresh = "Refresh";
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

        // Task Group Add Ships Page
        public const string TaskGroup_Edit_AddShipsPage_PageName = nameof( TaskGroup_Edit_AddShip_Page );
        public static readonly Type TaskGroup_Edit_AddShipsPage_PageType = typeof( TaskGroup_Edit_AddShip_Page );

        #region Query Parameters
        public class TaskGroupQueryParams : CommonQueryParams
        {
            public const string FleetId = "FleetId";
            public const string Fleet = "Fleet";
            public const string TaskGroup = "Task_Group";
            public const string TaskGroupId = "TaskGroupId";
            public const string AddShips = "AddShips";
        }
        #endregion Query Parameters

        #endregion Task Groups

        #region Ship Detail
        // Ship Detail Page
        public const string ShipDetail_Page_PageName = nameof( ShipDetailPage );
        public static readonly Type ShipDetail_Page_PageType = typeof( ShipDetailPage );

        // Edit Ship Detail Page
        public const string ShipDetail_EditPage_PageName = nameof( ShipDetail_Edit_Page );
        public static readonly Type ShipDetail_EditPage_PageType = typeof( ShipDetail_Edit_Page );

        #region Query Parameters
        public class ShipDetailQueryParams : CommonQueryParams
        {
            public const string Object = "Object";
        }
        #endregion Query Parameters
        #endregion Ship Detail

        #region Re-Task
        // Re-Task Ship Page
        public const string ReTaskShip_Page_PageName = nameof( RetaskShipPage );
        public static readonly Type RetaskShip_Page_PageType = typeof( RetaskShipPage );

        #region Query Parameters
        public class ReTaskQueryParams : CommonQueryParams
        {
            public const string Ship = "Ship";
        }
        #endregion Query Parameters
        #endregion Re-Task


        #region Shopping List
        public const string ShoppingList_Page_PageName = nameof( ShoppingListPage );
        public static readonly Type ShoppingList_Page_PageType = typeof( ShoppingListPage );

        public const string ShoppingList_FleetSelectedPage_PageName = nameof( ShoppingList_FleetSelected_Page );
        public static readonly Type ShoppingList_FleetSelectedPage_PageType = typeof( ShoppingList_FleetSelected_Page );

        #region Query Parameters
        public class ShoppingListQueryParams : CommonQueryParams
        {
            public const string LoadList = "LoadList";
        }
        #endregion Query Parameters
        #endregion Shopping List
    }
}
