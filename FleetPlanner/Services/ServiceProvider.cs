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
        public static ShipDatabaseService ShipDB = new ShipDatabaseService();

        public static async Task<List<Ship>> GetShips()
        {
            if( ShipDB.Db.Count < 1 )
            {
                await ShipDB.Init();
            }

            return ShipDB.Db.Values.ToList();
        }
    }
}
