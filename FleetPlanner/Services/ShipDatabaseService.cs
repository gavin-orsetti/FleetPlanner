using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using FleetPlanner.MVVM.Models;

namespace FleetPlanner.Services
{
    public class ShipDatabaseService : IReadOnlyDatabaseService
    {
        readonly Dictionary<int, Ship> shipDB = [];

        public Dictionary<int, Ship> ShipDB
        {
            get => shipDB;
        }

        public ShipDatabaseService()
        {
            Task.Run( Init );
        }

        async Task Init()
        {
            FileStream fs = new( Config.ShipDatabaseFilePath, FileMode.Open, FileAccess.Read );

            IAsyncEnumerable<Ship> ships = System.Text.Json.JsonSerializer.DeserializeAsyncEnumerable<Ship>( fs );

            await foreach( Ship ship in ships )
            {
                shipDB.Add( ship.Id, ship );
            }
        }

        public IDatabaseItem GetRow( int id )
        {
            return shipDB[ id ];
        }
    }
}
