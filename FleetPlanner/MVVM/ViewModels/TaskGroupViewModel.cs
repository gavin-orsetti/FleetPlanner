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
    public class TaskGroupViewModel( GlobalViewModel global ) : ViewModelBase( global )
    {
        private TaskGroup taskGroup = new();
        public TaskGroup Task_Group { get => taskGroup; protected set => SetProperty( ref taskGroup, value ); }

        private int? id;
        public int Id
        {
            get => id ??= Task_Group.Id;
            protected set => SetProperty( ref id, value );
        }

        private string name;
        public string Name
        {
            get => name ??= Task_Group.Name ??= string.Empty;
            set => SetProperty( ref name, value );
        }

        private int? fleetId;
        public int FleetId
        {
            get => fleetId ??= Task_Group.FleetId;
            set => SetProperty( ref fleetId, value );
        }

        private string objective;
        public string Objective
        {
            get => objective ??= Task_Group.Objective ??= string.Empty;
            set => SetProperty( ref objective, value );
        }

        private string areaOfOperation;
        public string AreaOfOperation
        {
            get => areaOfOperation ??= Task_Group.AreaOfOperation ??= string.Empty;
            set => SetProperty( ref areaOfOperation, value );
        }

        private int? integrality;
        public int Integrality
        {
            get => integrality ??= Task_Group.Integrality;
            set => SetProperty( ref integrality, value );
        }

        private long profitHourly;
        public long ProfitHourly
        {
            get => profitHourly;
            set => SetProperty( ref profitHourly, value );
        }

        private int crewCount_Max;
        public int CrewCount_Max
        {
            get => crewCount_Max;
            set => SetProperty( ref crewCount_Max, value );
        }

        private int crewCount_Min;
        public int CrewCount_Min
        {
            get => crewCount_Min;
            set => SetProperty( ref crewCount_Min, value );
        }

        private int crewCount_NPC;
        public int CrewCount_NPC
        {
            get => crewCount_NPC;
            set => SetProperty( ref crewCount_NPC, value );
        }

        private int shipCount;
        public int ShipCount
        {
            get => shipCount;
            set => SetProperty( ref shipCount, value );
        }

        private string notes;
        public string Notes
        {
            get => notes ??= Task_Group.Notes ??= string.Empty;
            set => SetProperty( ref notes, value );
        }



        #region Commands

        private AsyncCommand<int> goToTaskGroupCommand;
        public AsyncCommand<int> GoToTaskGroupCommand => goToTaskGroupCommand ??= new AsyncCommand<int>( GoToTaskGroup );

        private AsyncCommand<int> goToEditTaskGroupCommand;
        public AsyncCommand<int> GoToEditTaskGroupCommand => goToEditTaskGroupCommand ??= new AsyncCommand<int>( GoToEditTaskGroup );



        #endregion Commands

        #region Methods

        protected async Task DeleteYourself( int id, bool navigateBack = true )
        {
            TaskGroupDatabaseService taskGroupDbs = await ServiceProvider.GetTaskGroupDatabaseServiceAsync();

            await taskGroupDbs.DeleteAsync( id );
            if( navigateBack )
                await Shell.Current.GoToAsync( Routes.BackOne );
        }

        private async Task GoToTaskGroup( int id )
        {
            // This is intentionally not awaited, since we want this method to continue while the GlobalViewModel does its work in the background.
            // Perhaps there is a more elegant way to do this, but I don't know how.
            Global.LoadShipDetailsUsingTaskGroupIdAsync( Task_Group.Id );

            Dictionary<string, object> queryParams = new()
            {
                { Routes.TaskGroupQueryParams.TaskGroup, Task_Group }
            };

            await Shell.Current.GoToAsync( Routes.TaskGroup_Page_PageName, queryParams );
        }

        private async Task GoToEditTaskGroup( int id )
        {
            Dictionary<string, object> queryParams = new()
            {
                { Routes.TaskGroupQueryParams.TaskGroup, Task_Group }
            };

            await Shell.Current.GoToAsync( Routes.TaskGroup_EditPage_PageName, queryParams );
        }

        #region Query Handling
        private protected override async Task EvaluateQueryParams( KeyValuePair<string, object> kvp )
        {
            switch( kvp.Key )
            {
                case Routes.TaskGroupQueryParams.TaskGroupId:
                    Console.WriteLine( "Task_Group Id Passed" );
                    break;
                case Routes.TaskGroupQueryParams.TaskGroup:
                    Console.WriteLine( "Task Group Passed" );
                    break;
                default:
                    break;
            }
        }
        #endregion Query Handling
        #endregion Methods

    }
}
