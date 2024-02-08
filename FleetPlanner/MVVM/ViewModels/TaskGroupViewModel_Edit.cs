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
    public class TaskGroupViewModel_Edit : ViewTaskGroupViewModel
    {
        private AsyncCommand updateCommand;
        public AsyncCommand UpdateCommand => updateCommand ??= new AsyncCommand( Update );

        private async Task Update()
        {
            TaskGroupDatabaseService taskGroupDbs = await ServiceProvider.GetTaskGroupDatabaseServiceAsync();


            Task_Group.Name = Name;
            Task_Group.FleetId = FleetId;
            Task_Group.Objective = Objective;
            Task_Group.AreaOfOperation = AreaOfOperation;
            Task_Group.Integrality = Integrality;
            Task_Group.ProfitHourly = CalculateHourlyProfit();
            Task_Group.CrewCount_Max = CalculateMaxCrewCount();
            Task_Group.CrewCount_Min = CalculateMinCrewCount();
            Task_Group.CrewCount_NPC = CalculateNpcCrewCount();
            Task_Group.ShipCount = CountShips();
            Task_Group.Notes = Notes;
        }

        private long CalculateHourlyProfit()
        {
            long profit = 0;
            foreach( ShipDetailViewModel ship in ShipDetailShips )
            {
                profit += ship.HourlyIncome;
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

    }
}
