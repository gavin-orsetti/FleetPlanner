using FleetPlanner.MVVM.Models;

using SQLite;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FleetPlanner.Services
{
    public class FleetDatabaseService : IDatabaseService<FleetDatabaseService, Fleet>
    {

        private static FleetDatabaseService service;
        private static TableMapping mapping;
        private static DatabaseService instance;

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
            instance = await DatabaseService.Instance();

            try
            {
                mapping = await instance.CreateTable<Fleet>();
            }
            catch( Exception )
            {
                throw;
            }
        }
        #endregion Initialization

        #region Database Operations

        #region Create Data

        public async Task<bool> Insert( Fleet f )
        {
            try
            {
                return await instance.Insert( f );
            }
            catch( Exception )
            {

                throw;
            }
        }

        #endregion Create Data

        #region Read Data

        /// <summary>
        /// Gets all the records in the table.
        /// </summary>
        /// <returns>A list of all records</returns>
        public async Task<List<Fleet>> GetAll()
        {
            return await instance.GetAll<Fleet>();
        }

        /// <summary>
        /// Gets all the Fleets with the associated Id
        /// </summary>
        /// <param name="range"></param>
        /// <returns> List of Fleet objects where the ids match those passed in.</returns>
        public async Task<List<Fleet>> GetRange( List<int> range )
        {
            List<Fleet> result = await GetAll();

            return result.Where( x => range.Contains( x.Id ) ) as List<Fleet>;
        }

        /// <summary>
        /// This uses the "FindAsync" method of the SQLite-net project which means it can return null. Make sure to do a null check.
        /// </summary>
        /// <param name="id"></param>
        /// <returns>The Fleet with the associated id or null</returns>
        public async Task<Fleet> GetRow( int id )
        {
            return await instance.FindRow<Fleet>( id );
        }
        #endregion Read Data

        #region Update Data
        #endregion Update Data

        #region Delete Data
        #endregion Delete Data

        #endregion Database Operations
    }
}
