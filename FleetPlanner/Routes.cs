using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using FleetPlanner.MVVM.Views;

namespace FleetPlanner
{
    internal static class Routes
    {
        // New Fleet Page
        public const string MyFleets_NewFleet_PageName = nameof( MyFleets_AddNew_Page );
        public static readonly Type MyFleets_NewFleet_PageType = typeof( MyFleets_AddNew_Page );
    }
}
