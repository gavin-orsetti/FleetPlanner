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

        public static async Task<ShipDatabaseService> GetShipDatabaseService()
        {
            shipDb ??= await ShipDatabaseService.Create();

            return shipDb;
        }

        public static async Task<List<Ship>> GetShips()
        {
            if( shipDb == null )
            {
                await GetShipDatabaseService();
            }

            return [ .. shipDb.Db.Values ];
        }
    }
}
