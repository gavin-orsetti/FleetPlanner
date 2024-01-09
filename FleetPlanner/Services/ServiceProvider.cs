using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using FleetPlanner.MVVM.Models;

namespace FleetPlanner.Services
{
    public static class ServiceProvider
    {
        private static ShipDatabaseService shipDb;
        private static FleetDatabaseService fleetDb;

        public static async Task<ShipDatabaseService> GetShipDatabaseServiceAsync()
        {
            return shipDb ??= await ShipDatabaseService.Create();
        }

        public static async Task<FleetDatabaseService> GetFleetDatabaseServiceAsync()
        {
            return fleetDb ??= await FleetDatabaseService.Create();
        }

    }
}
