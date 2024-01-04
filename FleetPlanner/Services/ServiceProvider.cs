using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FleetPlanner.Services
{
    public static class ServiceProvider
    {
        public static ShipDatabaseService ShipDB = new ShipDatabaseService();
    }
}
