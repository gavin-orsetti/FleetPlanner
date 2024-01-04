using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using FleetPlanner.MVVM.Models;

namespace FleetPlanner.MVVM.ViewModels
{
    class TestBed
    {
        public List<Ship> Ships
        {
            get; set;
        }

        private Command loadShips => GetShips();
        public Command LoadShips => loadShips;

        private Command GetShips()
        {
            Ships = Services.ServiceProvider.ShipDB.ShipDB.Values.ToList();
            foreach( Ship ship in Ships )
            {
                Console.WriteLine( ship );
            }

            return GetShips();
        }
    }
}
