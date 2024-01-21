using FleetPlanner.MVVM.Models;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FleetPlanner.Services
{
    public class ShipDetailDatabaseService : DatabaseService<ShipDetailDatabaseService, ShipDetail>
    {
        #region Initialization
        new public static async Task<ShipDetailDatabaseService> Create()
        {
            if( service != null )
            {
                return service;
            }

            service = new ShipDetailDatabaseService();
            await service.Init();
            return service;
        }
        protected override async Task Init()
        {
            instance = DatabaseAccess.Instance();

            try
            {
                mapping = await instance.CreateTable<ShipDetail>();
            }
            catch( Exception )
            {

                throw;
            }
        }
        #endregion Initialization
    }
}
