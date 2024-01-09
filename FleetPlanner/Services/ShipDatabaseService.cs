using FleetPlanner.MVVM.Models;

namespace FleetPlanner.Services
{
    public class ShipDatabaseService : IDatabaseService<ShipDatabaseService, Ship>
    {
        private Dictionary<int, Ship> db;
        private static ShipDatabaseService service;
        public Dictionary<int, Ship> Db
        {
            get => db ??= [];
        }

        private ShipDatabaseService()
        {
        }

        public static async Task<ShipDatabaseService> Create()
        {
            if( service != null )
                return service;

            service = new ShipDatabaseService();
            await service.Init();

            return service;
        }

        private async Task Init()
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

        Task<Ship> IDatabaseService<ShipDatabaseService, Ship>.GetRow( int id )
        {
            throw new NotImplementedException();
        }

        public Task<List<Ship>> GetRange( IEnumerable<int> range )
        {
            throw new NotImplementedException();
        }

        public Task<List<Ship>> GetAll()
        {
            throw new NotImplementedException();
        }
    }
}
