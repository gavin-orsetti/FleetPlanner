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
    public class ViewTaskGroupViewModel( GlobalViewModel global ) : TaskGroupViewModel( global )
    {
        private int crewCount_MaxPlusNPC;
        public int CrewCount_MaxPlusNPC
        {
            get => crewCount_MaxPlusNPC;
            set => SetProperty( ref crewCount_MaxPlusNPC, value );
        }

        private int crewCount_MinPlusNPC;
        public int CrewCount_MinPlusNPC
        {
            get => crewCount_MinPlusNPC;
            set => SetProperty( ref crewCount_MinPlusNPC, value );
        }

        private ObservableRangeCollection<ShipDetailViewModel_Populated> shipDetailShips;
        public ObservableRangeCollection<ShipDetailViewModel_Populated> ShipDetailShips
        {
            get => Global.PopulatedShipDetailViewModels;
            //set => SetProperty( ref shipDetailShips, value );
        }


        #region Commands
        private Microsoft.Maui.Controls.Command<int> deleteShipDetailCommand;
        public Microsoft.Maui.Controls.Command<int> DeleteShipDetailCommand => deleteShipDetailCommand ??= new Microsoft.Maui.Controls.Command<int>( DeleteShipDetail );
        #endregion Commands

        #region Methods
        private void DeleteShipDetail( int id )
        {
            ShipDetailViewModel_Populated sd = ShipDetailShips.Where( x => x.Id == id ).First();
            ShipDetailShips.Remove( sd );
        }

        #region Query Handling
        private protected override async Task EvaluateQueryParams( KeyValuePair<string, object> kvp )
        {
            switch( kvp.Key )
            {
                case Routes.TaskGroupQueryParams.TaskGroup:
                    Task_Group = (TaskGroup)kvp.Value;
                    Populate();
                    break;
                case Routes.TaskGroupQueryParams.TaskGroupId:
                    Task_Group = await LoadTaskGroup( (int)kvp.Value );
                    Populate();
                    break;

                default:
                    break;
            }
        }

        private protected async Task<TaskGroup> LoadTaskGroup( int id )
        {
            TaskGroupDatabaseService taskGroupDbs = await ServiceProvider.GetTaskGroupDatabaseServiceAsync();
            TaskGroup tg = await taskGroupDbs.GetRow( id );

            if( tg == null )
            {
                throw new ArgumentOutOfRangeException( nameof( tg ) );
            }

            return tg;
        }

        private protected void Populate()
        {
            Id = Task_Group.Id;

            //await GetShips();

            Name = Task_Group.Name;
            FleetId = Task_Group.FleetId;
            Objective = Task_Group.Objective;
            AreaOfOperation = Task_Group.AreaOfOperation;
            Integrality = Task_Group.Integrality;
            ProfitHourly = Task_Group.ProfitHourly;
            CrewCount_Max = Task_Group.CrewCount_Max;
            CrewCount_Min = Task_Group.CrewCount_Min;
            CrewCount_NPC = Task_Group.CrewCount_NPC;
            CrewCount_MaxPlusNPC = CrewCount_Max + CrewCount_NPC;
            CrewCount_MinPlusNPC = CrewCount_Min + CrewCount_NPC;
            ShipCount = Task_Group.ShipCount;
            Notes = Task_Group.Notes;
        }

        private protected async Task GetShips()
        {
            ShipDetailDatabaseService shipDetailDbs = await ServiceProvider.GetShipDetailDatabaseServiceAsync();
            List<ShipDetail> shps = await shipDetailDbs.GetChildrenUsingPropertyNameAsync( Task_Group.Id, nameof( ShipDetail.TaskGroupId ) );
            List<ShipDetailViewModel_Populated> sdvm_ps = [];
            foreach( ShipDetail shp in shps )
            {
                ShipDetailViewModel_Populated sdvm_p = new ShipDetailViewModel_Populated( shp, DeleteShipDetail, Global );
                await sdvm_p.PopulateCommand.ExecuteAsync();
                sdvm_ps.Add( sdvm_p );
            }

            ShipDetailShips.Clear();
            ShipDetailShips.AddRange( sdvm_ps );
        }
        #endregion Query Handling
        #endregion Methods
    }
}
