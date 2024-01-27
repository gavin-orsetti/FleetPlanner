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
    }
}
