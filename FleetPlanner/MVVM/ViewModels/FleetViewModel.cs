using FleetPlanner.MVVM.Models;
using FleetPlanner.MVVM.ViewModels;
using FleetPlanner.MVVM.ViewModels;
using FleetPlanner.MVVM.ViewModels;
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
    public class FleetViewModel : ViewModelBase
    {
        public FleetViewModel()
        { }

        #region Fields
        public AsyncCommand RefreshParentView;
        #endregion Fields

        #region Properties & Commands
        private Fleet fleet;
        public Fleet Fleet => fleet ??= new Fleet();

        private int id;
        public int Id
        {
            get => id;
            set => SetProperty( ref id, value );
        }

        private string name;
        public string Name
        {
            get => name ??= string.Empty;
            set => SetProperty( ref name, value );
        }

        private string affiliation;
        public string Affiliation
        {
            get => affiliation ??= string.Empty;
            set => SetProperty( ref affiliation, value );
        }

        private string manifesto;
        public string Manifesto
        {
            get => manifesto ??= string.Empty;
            set => SetProperty( ref manifesto, value );
        }

        private string areaOfOperation;
        public string AreaOfOperation
        {
            get => areaOfOperation ??= string.Empty;
            set => SetProperty( ref areaOfOperation, value );
        }

        private int crewTotalMax;
        public int CrewTotalMax
        {
            get => crewTotalMax;
            set => SetProperty( ref crewTotalMax, value );
        }

        private int crewTotalMin;
        public int CrewTotalMin
        {
            get => crewTotalMin;
            set => SetProperty( ref crewTotalMin, value );
        }

        private int npcCrewMin;
        public int NpcCrewMin
        {
            get => npcCrewMin;
            set => SetProperty( ref npcCrewMin, value );
        }

        private int npcCrewMax;
        public int NpcCrewMax
        {
            get => npcCrewMax;
            set => SetProperty( ref npcCrewMax, value );
        }

        private long expectedProfit;
        public long ExpectedProfit
        {
            get => expectedProfit;
            set => SetProperty( ref expectedProfit, value );
        }

        private int taskGroupCount;
        public int TaskGroupCount
        {
            get => taskGroupCount;
            set => SetProperty( ref taskGroupCount, value );
        }

        private int shipCount;
        public int ShipCount
        {
            get => shipCount;
            set => SetProperty( ref shipCount, value );
        }

        private int shipsOwned;
        public int ShipsOwned
        {
            get => shipsOwned;
            set => SetProperty( ref shipsOwned, value );
        }

        private int shipsActive;
        public int ShipsActive
        {
            get => shipsActive;
            set => SetProperty( ref shipsActive, value );
        }

        private int shipsInactive;
        public int ShipsInactive
        {
            get => shipsInactive;
            set => SetProperty( ref shipsInactive, value );
        }

        private string notes;
        public string Notes
        {
            get => notes ??= string.Empty;
            set => SetProperty( ref notes, value );
        }

        private ObservableRangeCollection<TaskGroup> taskGroups;
        public ObservableRangeCollection<TaskGroup> TaskGroups
        {
            get => taskGroups ??= [];
            set => SetProperty( ref taskGroups, value );
        }



        private AsyncCommand<int> goToEditFleetCommand;
        public AsyncCommand<int> GoToEditFleetCommand => goToEditFleetCommand ??= new AsyncCommand<int>( GoToEditFleet );

        private AsyncCommand<int> deleteCommand;
        public AsyncCommand<int> DeleteCommand => deleteCommand ??= new AsyncCommand<int>( Delete );

        private AsyncCommand<int> goToAddddNewTaskGroupCommand;
        public AsyncCommand<int> GoToAddNewTaskGroupCommand => goToAddddNewTaskGroupCommand ??= new AsyncCommand<int>( GoToAddNewTaskGroup );

        #endregion Properties & Commands

        #region Methods

        private async Task Delete( int id )
        {
            if( id <= 0 )
            {
                throw new Exception( "Error 0: Object does not exist in database" );
            }

            FleetDatabaseService dBService = await ServiceProvider.GetFleetDatabaseServiceAsync();

            if( await dBService.DeleteAsync<Fleet>( id ) )
            {
                if( RefreshParentView != null )
                {
                    try
                    {
                        await RefreshParentView.ExecuteAsync();
                    }
                    catch( Exception )
                    {

                        throw;
                    }
                }
                else
                {
                    await Shell.Current.GoToAsync( Routes.BackOne );
                }
            }
        }


        private async Task GoToEditFleet( int id )
        {
            Dictionary<string, object> queryParams = Fleet != null
                ? new()
                {
                    { Routes.FleetQueryParams.PopulatedViewModel, Fleet}
                }
                : new()
                {
                    { Routes.FleetQueryParams.EditViewModel, id }
                };

            await Shell.Current.GoToAsync( $"{Routes.EditFleetPage_PageName}", queryParams );
        }

        private async Task GoToAddNewTaskGroup( int id )
        {
            Dictionary<string, object> queryParams = new()
            {
                { Routes.TaskGroupQueryParams.FleetId, id }
            };

            await Shell.Current.GoToAsync( Routes.TaskGroup_AddNew_PageName, queryParams );
        }

        #region Query Handling

        private protected override async Task EvaluateQueryParams( KeyValuePair<string, object> kvp )
        {
            switch( kvp.Key )
            {
                case Routes.FleetQueryParams.PopulatedViewModel:
                    fleet = kvp.Value as Fleet;
                    await Populate();
                    break;

                default:
                    break;
            }
        }

        private async Task Populate()
        {
            await GetTaskGroups( Fleet.Id );

            Id = Fleet.Id;
            Name = Fleet.Name;
            Affiliation = Fleet.Affiliation;
            AreaOfOperation = Fleet.AreaOfOperation;
            Manifesto = Fleet.Manifesto;
            CrewTotalMax = Fleet.CrewTotalMax;
            CrewTotalMin = Fleet.CrewTotalMin;
            NpcCrewMax = Fleet.NpcCrewMax;
            NpcCrewMin = Fleet.NpcCrewMin;
            ExpectedProfit = Fleet.ExpectedProfit;
            ShipCount = Fleet.ShipCount;
            TaskGroupCount = Fleet.TaskGroupCount;
            ShipsOwned = Fleet.ShipsOwned;
            ShipsActive = Fleet.ShipsActive;
            ShipsInactive = Fleet.ShipsInactive;
            Notes = Fleet.Notes;
        }

        private async Task GetTaskGroups( int id )
        {
            TaskGroupDatabaseService dbService = await ServiceProvider.GetTaskGroupDatabaseServiceAsync();

            List<TaskGroup> tgs = await dbService.GetChildrenUsingId( id, nameof( TaskGroup.FleetId ) );

            TaskGroups.Clear();
            TaskGroups.AddRange( tgs );
        }

        #endregion Query Handling

        #endregion Methods
    }
}
