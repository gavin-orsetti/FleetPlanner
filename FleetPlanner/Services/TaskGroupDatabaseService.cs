using FleetPlanner.MVVM.Models;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FleetPlanner.Services
{
    public class TaskGroupDatabaseService : DatabaseService<TaskGroupDatabaseService, TaskGroup>
    {
        /// <summary>
        /// Private constructor prevents instantiation except through the Create() method
        /// </summary>
        private TaskGroupDatabaseService() { }

        #region Initialization
        new public static async Task<TaskGroupDatabaseService> Create()
        {
            if( service != null )
                return service;

            service = new TaskGroupDatabaseService();
            await service.Init();
            return service;
        }

        protected override async Task Init()
        {
            instance = DatabaseAccess.Instance();

            try
            {
                mapping = await instance.CreateTable<TaskGroup>();
            }
            catch( Exception )
            {

                throw;
            }
        }

        #endregion Initialization

        #region Database Operations
        new public async Task DeleteAsync( int id )
        {
            ShipDetailDatabaseService shipDetailDbs = await ServiceProvider.GetShipDetailDatabaseServiceAsync();
            await shipDetailDbs.DeleteWithParentTaskGroupIdAsync( id );

            await base.DeleteAsync( id );
        }


        public async Task DeleteWithFleetIdAsync( int id )
        {
            List<TaskGroup> taskGroups = await GetChildrenUsingPropertyNameAsync( id, nameof( TaskGroup.FleetId ) );
            foreach( TaskGroup taskGroup in taskGroups )
            {
                await DeleteAsync( taskGroup.Id );
            }
        }
        #endregion Database Operations
    }
}
