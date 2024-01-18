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
        #endregion Database Operations
    }
}
