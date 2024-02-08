using FleetPlanner.MVVM.Models;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FleetPlanner.Services
{
    public class ShipBalanceSheetDatabaseService : DatabaseService<ShipBalanceSheetDatabaseService, ShipBalanceSheet>
    {
        private ShipBalanceSheetDatabaseService() { }

        new public static async Task<ShipBalanceSheetDatabaseService> Create()
        {
            if( service != null )
            {
                return service;
            }

            service = new ShipBalanceSheetDatabaseService();
            await service.Init();
            return service;
        }
        protected override async Task Init()
        {
            instance = DatabaseAccess.Instance();

            try
            {
                mapping = await instance.CreateTable<ShipBalanceSheet>();
            }
            catch( Exception e )
            {

                throw e.InnerException;
            }
        }

        public async Task DeleteWithShipIdAsync( int shipDetailId )
        {
            List<ShipBalanceSheet> sheets = await GetChildrenUsingPropertyNameAsync( shipDetailId, nameof( ShipBalanceSheet.ShipDetailId ) );
            await instance.DeleteRange<ShipBalanceSheet>( sheets.Select( x => x.Id ).ToList() );
        }
    }
}
