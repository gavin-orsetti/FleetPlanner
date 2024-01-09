using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using FleetPlanner.MVVM.Models;

using SQLite;

namespace FleetPlanner.Services
{
    public class FleetDatabaseService : IDatabaseService<FleetDatabaseService, Fleet>
    {
        private SQLiteAsyncConnection db;
        private static FleetDatabaseService service;

        private FleetDatabaseService()
        {
        }

        #region Initialization
        public static async Task<FleetDatabaseService> Create()
        {
            if( service != null )
                return service;

            service = new FleetDatabaseService();
            await service.Init();
            return service;
        }

        async Task Init()
        {
            if( db != null )
                return;

            db = new SQLiteAsyncConnection( Config.DatabasePath, Config.Flags );
            CreateTableResult result = await db.CreateTableAsync<Fleet>();
        }
        #endregion Initialization

        #region Data Access
        public Task<List<Fleet>> GetAll()
        {
            throw new NotImplementedException();
        }

        public Task<List<Fleet>> GetRange( IEnumerable<int> range )
        {
            throw new NotImplementedException();
        }

        public Task<Fleet> GetRow( int id )
        {
            throw new NotImplementedException();
        }
        #endregion DataAccess
    }
}
