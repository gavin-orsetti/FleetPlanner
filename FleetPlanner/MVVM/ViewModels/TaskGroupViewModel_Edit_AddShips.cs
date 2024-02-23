using FleetPlanner.MVVM.Models;
using FleetPlanner.Services;

using MvvmHelpers.Commands;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FleetPlanner.MVVM.ViewModels
{
    public class TaskGroupViewModel_Edit_AddShips( GlobalViewModel global ) : NewTaskGroupViewModel( global )
    {
        new private AsyncCommand saveCommand;
        new public AsyncCommand SaveCommand => saveCommand ??= new AsyncCommand( Save );
        #region Methods

        new private async Task Save()
        {
            ShipDetailDatabaseService shipDetailDbs = await Services.ServiceProvider.GetShipDetailDatabaseServiceAsync();

            List<ShipDetail> shipDetails = CreateShipDetails();
            await shipDetailDbs.InsertMultiple( shipDetails );

            await Shell.Current.GoToAsync( Routes.BackOne );
        }

        #region Query Handling
        private protected override async Task EvaluateQueryParams( KeyValuePair<string, object> kvp )
        {
            switch( kvp.Key )
            {
                case Routes.TaskGroupQueryParams.TaskGroup:
                    Task_Group = (TaskGroup)kvp.Value;
                    //await LoadShips();
                    break;
                default:
                    break;
            }
        }
        #endregion Query Handling
        #endregion Methods
    }
}
