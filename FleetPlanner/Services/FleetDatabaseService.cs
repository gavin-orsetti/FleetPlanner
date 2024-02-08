using FleetPlanner.MVVM.Models;

using SQLite;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FleetPlanner.Services
{
    public class FleetDatabaseService : DatabaseService<FleetDatabaseService, Fleet>
    {
        /// <summary>
        /// Private constructor prevents instantiation except through the Create() method.
        /// </summary>
        private FleetDatabaseService() { }

        #region Initialization
        new public static async Task<FleetDatabaseService> Create()
        {
            if( service != null )
                return service;

            service = new FleetDatabaseService();
            await service.Init();
            return service;
        }

        protected override async Task Init()
        {
            instance = DatabaseAccess.Instance();

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

        //new public async Task<bool> Insert( Fleet f )
        //{
        //    try
        //    {
        //        return await instance.Insert( f );
        //    }
        //    catch( Exception )
        //    {

        //        throw;
        //    }
        //}

        #endregion Create Data

        #region Read Data
        #endregion Read Data

        #region Update Data
        #endregion Update Data

        #region Delete Data
        new public async Task DeleteAsync( int id )
        {
            TaskGroupDatabaseService taskGroupDbs = await ServiceProvider.GetTaskGroupDatabaseServiceAsync();
            await taskGroupDbs.DeleteWithFleetIdAsync( id );

            await base.DeleteAsync( id );
        }

        #endregion Delete Data

        #endregion Database Operations
    }
}
