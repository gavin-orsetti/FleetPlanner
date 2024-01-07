using FleetPlanner.MVVM.Models;

namespace FleetPlanner.Services
{
    public class ShipDatabaseService : IReadOnlyDatabaseService
    {
        private Dictionary<int, Ship> db;

        public Dictionary<int, Ship> Db
        {
            get => db ??= [];
        }

        private ShipDatabaseService()
        {
        }

        public static async Task<ShipDatabaseService> Create()
        {
            ShipDatabaseService service = new();
            await service.Init();
            return service;
        }

        public async Task Init()
        {

            try
            {
                using Stream stream = await FileSystem.OpenAppPackageFileAsync( "ships.json" );
                using StreamReader r = new( stream );

                IAsyncEnumerable<Ship> ships = System.Text.Json.JsonSerializer.DeserializeAsyncEnumerable<Ship>( stream );

                await foreach( Ship ship in ships )
                {
                    Db.Add( ship.Id, ship );
                }

            }
            catch( Exception )
            {
                throw;
            }

        }

        public IDatabaseItem GetRow( int id )
        {
            return Db[ id ];
        }
    }
}
