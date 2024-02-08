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
        private ShipDetailDatabaseService() { }

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
            catch( Exception e )
            {

                throw e.InnerException;
            }
        }
        #endregion Initialization

        #region Delete
        new public async Task<bool> DeleteAsync( int id )
        {
            ShipBalanceSheetDatabaseService shipBalanceSheetDbs = await ServiceProvider.GetShipBalanceSheetDatabaseServiceAsync();

            await shipBalanceSheetDbs.DeleteWithShipIdAsync( id );
            return await base.DeleteAsync( id );
        }

        public async Task DeleteWithParentTaskGroupIdAsync( int id )
        {
            List<ShipDetail> shipDetails = await GetChildrenUsingPropertyNameAsync( id, nameof( ShipDetail.TaskGroupId ) );

            foreach( ShipDetail shipDetail in shipDetails )
            {
                await DeleteAsync( shipDetail.Id );
            }
        }
        #endregion Delete

    }
}
