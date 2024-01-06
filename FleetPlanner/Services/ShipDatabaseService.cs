using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using FleetPlanner.MVVM.Models;

namespace FleetPlanner.Services
{
    public class ShipDatabaseService : IReadOnlyDatabaseService
    {
        readonly Dictionary<int, Ship> shipDB = [];

        public Dictionary<int, Ship> Db
        {
            get => shipDB;
        }

        //public ShipDatabaseService()
        //{
        //    Task.Run( Init );
        //}

        public async Task Init()
        {

            try
            {
                using Stream stream = await FileSystem.OpenAppPackageFileAsync( "ships.json" );
                using StreamReader r = new( stream );

                IAsyncEnumerable<Ship> ships = System.Text.Json.JsonSerializer.DeserializeAsyncEnumerable<Ship>( stream );

                await foreach( Ship ship in ships )
                {
                    shipDB.Add( ship.Id, ship );
                }

            }
            catch( Exception ex )
            {
                throw;
            }

        }

        public IDatabaseItem GetRow( int id )
        {
            return shipDB[ id ];
        }
    }
}
