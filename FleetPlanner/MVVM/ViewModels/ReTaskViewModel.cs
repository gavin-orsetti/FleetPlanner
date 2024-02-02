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


        private AsyncCommand<int> selectionChangedCommand;
        public AsyncCommand<int> SelectionChangedCommand => selectionChangedCommand ??= new AsyncCommand<int>( SelectionChanged );

        #region Methods
        private async Task SelectionChanged( int id )
        {
            foreach( TaskGroupViewModel_Populated taskGroup in TaskGroups )
            {
                taskGroup.IsChecked = id == taskGroup.Id;
            }
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

                TaskGroupViewModel_Populated tgVm = new TaskGroupViewModel_Populated( t );
                tgVms.Add( tgVm );
            }

            TaskGroups.Clear();
            TaskGroups.AddRange( tgVms );

        }
        #endregion Query Handling
        #endregion Methods
    }
}
