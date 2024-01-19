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

        public Task<Ship> GetRow( int id )
        {
            throw new NotImplementedException();
        }

        public async Task<List<Ship>> GetAll()
        {
            return Db.Values.ToList();
        }

        public Task<List<Ship>> GetRange( List<int> range )
        {
            throw new NotImplementedException();
        }

        public Task<List<Ship>> GetChildrenUsingPropertyName( int parentId, string columnName )
        {
            throw new NotImplementedException();
        }

        public Task<bool> Insert( Ship item )
        {
            throw new NotImplementedException();
        }

        public Task<bool> DeleteAsync<T>( int id )
        {
            throw new NotImplementedException();
        }
    }
}
