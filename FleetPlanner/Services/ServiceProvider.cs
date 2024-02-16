using FleetPlanner.MVVM.Models;

using MvvmHelpers.Commands;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FleetPlanner.Services
{
    public class ServiceProvider
    {
        private static ShipDatabaseService shipDb;
        private static FleetDatabaseService fleetDb;
        private static TaskGroupDatabaseService taskGroupDb;
        private static ShipDetailDatabaseService shipDetailDb;
        private static ShipBalanceSheetDatabaseService shipBalanceSheetDb;

        public ServiceProvider()
        {
            createAll.ExecuteAsync();
        }

        private static AsyncCommand createAll = new AsyncCommand( CreateAll );

        private static async Task CreateAll()
        {
            _ = await GetShipDatabaseServiceAsync();
            _ = await GetShipDetailDatabaseServiceAsync();
            _ = await GetFleetDatabaseServiceAsync();
            _ = await GetTaskGroupDatabaseServiceAsync();
            _ = await GetShipBalanceSheetDatabaseServiceAsync();
        }

        public static async Task<ShipDatabaseService> GetShipDatabaseServiceAsync()
        {
            return shipDb ??= await ShipDatabaseService.Create();
        }

        public static async Task<FleetDatabaseService> GetFleetDatabaseServiceAsync()
        {
            return fleetDb ??= await FleetDatabaseService.Create();
        }

        public static async Task<TaskGroupDatabaseService> GetTaskGroupDatabaseServiceAsync()
        {
            return taskGroupDb ??= await TaskGroupDatabaseService.Create();
        }

        public static async Task<ShipDetailDatabaseService> GetShipDetailDatabaseServiceAsync()
        {
            return shipDetailDb ??= await ShipDetailDatabaseService.Create();
        }

        public static async Task<ShipBalanceSheetDatabaseService> GetShipBalanceSheetDatabaseServiceAsync()
        {
            return shipBalanceSheetDb ??= await ShipBalanceSheetDatabaseService.Create();
        }

    }
}
