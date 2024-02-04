using FleetPlanner.MVVM.Models;
using FleetPlanner.Services;

using MvvmHelpers;
using MvvmHelpers.Commands;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using ServiceProvider = FleetPlanner.Services.ServiceProvider;

namespace FleetPlanner.MVVM.ViewModels
{
    public class ReTaskViewModel : ViewModelBase
    {
        private ShipDetail shipDetail;
        private ObservableRangeCollection<TaskGroupViewModel_Populated> taskGroups;
        public ObservableRangeCollection<TaskGroupViewModel_Populated> TaskGroups => taskGroups ??= new();
        private TaskGroupViewModel_Populated selectedTaskGroup;

        private bool isInvoking = false;
        #region Methods
        private void SelectionChanged( TaskGroupViewModel_Populated selection )
        {
            if( !isInvoking )
            {
                isInvoking = true;

                if( selectedTaskGroup != null )
                {
                    selectedTaskGroup.IsChecked = false;
                }

                selectedTaskGroup = selection;

                isInvoking = false;
            }
        }

        private async Task Retask( int id )
        {
            if( shipDetail != null )
            {

                ShipDetailDatabaseService shipDetailDbs = await ServiceProvider.GetShipDetailDatabaseServiceAsync();
                shipDetail.TaskGroupId = id;

                await shipDetailDbs.Update( shipDetail );
            }
            else
            {
                Console.WriteLine( "SHIP DETAIL NULL!!!" );
            }

            await Shell.Current.GoToAsync( Routes.BackOne );
        }

        #region Query Handling
        private protected override async Task EvaluateQueryParams( KeyValuePair<string, object> kvp )
        {
            switch( kvp.Key )
            {
                case Routes.ReTaskQueryParams.Ship:
                    shipDetail = (ShipDetail)kvp.Value;
                    await Populate();
                    break;
                default:
                    break;
            }
        }


        private async Task Populate()
        {
            TaskGroupDatabaseService taskGroupDbs = await ServiceProvider.GetTaskGroupDatabaseServiceAsync();

            List<TaskGroup> tgs = await taskGroupDbs.GetAll();
            List<TaskGroupViewModel_Populated> tgVms = new();

            foreach( TaskGroup t in tgs )
            {
                if( t.Id == shipDetail.TaskGroupId )
                    continue;

                TaskGroupViewModel_Populated tgVm = new TaskGroupViewModel_Populated( t, SelectionChanged, Retask );
                tgVms.Add( tgVm );
            }

            TaskGroups.Clear();
            TaskGroups.AddRange( tgVms );
        }
        #endregion Query Handling
        #endregion Methods
    }
}
