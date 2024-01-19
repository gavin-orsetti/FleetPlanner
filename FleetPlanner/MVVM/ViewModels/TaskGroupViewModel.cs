using FleetPlanner.MVVM.Models;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FleetPlanner.MVVM.ViewModels
{
    public class TaskGroupViewModel : ViewModelBase
    {
        private TaskGroup taskGroup;
        public TaskGroup TaskGroup { get { return taskGroup; } }

        private string name;
        public string Name
        {
            get => name ??= string.Empty;
            set => SetProperty( ref name, value );
        }
        private int fleetId;
        public int FleetId
        {
            get => fleetId;
            set => SetProperty( ref fleetId, value );
        }

        private string objective;
        public string Objective
        {
            get => objective ??= string.Empty;
            set => SetProperty( ref objective, value );
        }

        private string areaOfOperation;
        public string AreaOfOperation
        {
            get => areaOfOperation ??= string.Empty;
            set => SetProperty( ref areaOfOperation, value );
        }

        private int integrality;
        public int Integrality
        {
            get => integrality;
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
            get => notes ??= string.Empty;
            set => SetProperty( ref notes, value );
        }


        #region Methods
        #region Query Handling
        private protected override async Task EvaluateQueryParams( KeyValuePair<string, object> kvp )
        {
            switch( kvp.Key )
            {
                case Routes.TaskGroupQueryParams.TaskGroupId:
                    Console.WriteLine( "TaskGroup Id Passed" );
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
