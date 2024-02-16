using FleetPlanner.MVVM.Models;

using System.Collections;

namespace FleetPlanner.Services
{
    public class ShipDatabaseService : IDatabaseService<ShipDatabaseService, Ship>
    {
        private static Dictionary<int, Ship> db;
        private static ShipDatabaseService service;
        public static Dictionary<int, Ship> Db
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

        public async Task<Ship> GetRow( int id )
        {
            return Db.TryGetValue( id, out Ship ship ) ? ship : null;
        }

        public async Task<List<Ship>> GetAll()
        {
            return Db.Values.ToList();
        }

        public Task<List<Ship>> GetRange( List<int> range )
        {
            throw new NotImplementedException();
        }

        public Task<Ship> GetLastInsert()
        {
            throw new NotImplementedException();
        }

        public Task<List<Ship>> GetChildrenUsingPropertyNameAsync( int parentId, string columnName )
        {
            throw new NotImplementedException();
        }

        public Task<bool> Insert( Ship item )
        {
            throw new NotImplementedException();
        }

        public Task<bool> DeleteAsync( int id )
        {
            throw new NotImplementedException();
        }

        public Task<bool> InsertMultiple( List<Ship> Items )
        {
            throw new NotImplementedException();
        }
    }
}
