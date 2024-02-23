using FleetPlanner.MVVM.Models;
using FleetPlanner.Services;

using MvvmHelpers.Commands;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using ServiceProvider = FleetPlanner.Services.ServiceProvider;

namespace FleetPlanner.MVVM.ViewModels
{
    public class TaskGroupViewModel_Edit( GlobalViewModel global ) : ViewTaskGroupViewModel( global )
    {
        private AsyncCommand updateCommand;
        public AsyncCommand UpdateCommand => updateCommand ??= new AsyncCommand( Update );

        private AsyncCommand addShipsCommand;
        public AsyncCommand AddShipsCommand => addShipsCommand ??= new AsyncCommand( AddShips );

        #region Methods
        private async Task AddShips()
        {
            Dictionary<string, object> queryParams = new()
            {
                { Routes.TaskGroupQueryParams.TaskGroup, Task_Group }
            };

            await Shell.Current.GoToAsync( Routes.TaskGroup_Edit_AddShipsPage_PageName, queryParams );
        }

        private async Task Update()
        {
            TaskGroupDatabaseService taskGroupDbs = await ServiceProvider.GetTaskGroupDatabaseServiceAsync();


            Task_Group.Name = Name;
            Task_Group.FleetId = FleetId;
            Task_Group.Objective = Objective;
            Task_Group.AreaOfOperation = AreaOfOperation;
            Task_Group.Integrality = Integrality;
            Task_Group.ProfitHourly = CalculateExpectedProfit();
            Task_Group.CrewCount_Max = CalculateMaxCrewCount();
            Task_Group.CrewCount_Min = CalculateMinCrewCount();
            Task_Group.CrewCount_NPC = CalculateNpcCrewCount();
            Task_Group.ShipCount = CountShips();
            Task_Group.Notes = Notes;

            await taskGroupDbs.Update( Task_Group );

            await Shell.Current.GoToAsync( Routes.BackOne );
        }

        private async Task Update( bool goBack = true )
        {
            TaskGroupDatabaseService taskGroupDbs = await ServiceProvider.GetTaskGroupDatabaseServiceAsync();


            Task_Group.Name = Name;
            Task_Group.FleetId = FleetId;
            Task_Group.Objective = Objective;
            Task_Group.AreaOfOperation = AreaOfOperation;
            Task_Group.Integrality = Integrality;
            Task_Group.ProfitHourly = CalculateExpectedProfit();
            Task_Group.CrewCount_Max = CalculateMaxCrewCount();
            Task_Group.CrewCount_Min = CalculateMinCrewCount();
            Task_Group.CrewCount_NPC = CalculateNpcCrewCount();
            Task_Group.ShipCount = CountShips();
            Task_Group.Notes = Notes;

            await taskGroupDbs.Update( Task_Group );

            if( goBack )
                await Shell.Current.GoToAsync( Routes.BackOne );
        }


        private long CalculateExpectedProfit()
        {
            long profit = 0;
            foreach( ShipDetailViewModel ship in ShipDetailShips )
            {
                profit += ship.ExpectedProfit;
            }

            return profit;
        }

        private int CalculateNpcCrewCount()
        {
            int count = 0;
            foreach( ShipDetailViewModel ship in ShipDetailShips )
            {
                count += ship.NpcCrewMin;
            }

            return count;
        }

        private int CalculateMaxCrewCount()
        {
            int count = 0;
            foreach( ShipDetailViewModel ship in ShipDetailShips )
            {
                count += ship.CrewTotalMax;
            }
            return count;
        }
        private int CalculateMinCrewCount()
        {
            int count = 0;
            foreach( ShipDetailViewModel ship in ShipDetailShips )
            {
                count += ship.CrewTotalMin;
            }
            return count;
        }

        private int CountShips()
        {
            int count = 0;

            foreach( ShipDetailViewModel ship in ShipDetailShips )
            {
                count += 1;
            }

            return count;
        }

        #region Query Handling
        private protected override async Task EvaluateQueryParams( KeyValuePair<string, object> kvp )
        {
            switch( kvp.Key )
            {
                // This case has to be a bit complicated because to correctly load the task group we need to do the following:
                // 1. Get the newly created ships and add them to the list of ships
                // 2. Update the task group to include the defaults assigned to the new ships (things like crew etc.)
                // 3. Load the new data into the task group.
                //
                // To do this we use the new Populate method which is, I am sure, very inefficient.
                case Routes.CommonQueryParams.Refresh:
                    if( Task_Group.Id <= 0 )
                    { Task_Group = await LoadTaskGroup( (int)kvp.Value ); }

                    await Populate();
                    break;
                default:
                    break;
            }

            await base.EvaluateQueryParams( kvp );
        }

        new private protected async Task Populate()
        {
            await GetShips();
            await Update( false );
            base.Populate();

        }
        #endregion Query Handling
        #endregion Methods

    }
}
