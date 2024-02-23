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
    public class GlobalViewModel : BaseViewModel
    {
        public GlobalViewModel()
        {
            LoadServicesCommand.ExecuteAsync();
        }

        #region Delegates
        private void DeleteFleetViewModel( int id )
        {
            FleetViewModel_Populated vm = PopulatedFleetViewModels.Where( x => x.Id == id ).FirstOrDefault();
            PopulatedFleetViewModels.Remove( vm );
        }

        private void DeleteTaskGroupViewModel( int id )
        {
            TaskGroupViewModel_Populated vm = PopulatedTaskGroupViewModels.Where( x => x.Id == id ).FirstOrDefault();
            PopulatedTaskGroupViewModels.Remove( vm );
        }

        private void DeleteShipDetailViewModel( int id )
        {
            ShipDetailViewModel_Populated sd = PopulatedShipDetailViewModels.Where( x => x.Id == id ).First();
            PopulatedShipDetailViewModels.Remove( sd );
        }
        #endregion Delegates

        #region Fields and Properties
        private FleetDatabaseService fleetDbs;
        private TaskGroupDatabaseService taskGroupDbs;
        private ShipDetailDatabaseService shipDetailDbs;
        private ShipBalanceSheetDatabaseService shipBalanceSheetDbs;
        private ShipDatabaseService shipDbs;

        public ObservableRangeCollection<FleetViewModel_Populated> PopulatedFleetViewModels { get; } = [];
        public ObservableRangeCollection<ShipDetailViewModel_Populated> PopulatedShipDetailViewModels { get; } = [];
        public ObservableRangeCollection<TaskGroupViewModel_Populated> PopulatedTaskGroupViewModels { get; } = [];
        public ObservableRangeCollection<ShoppingListShipDetailViewModel> ShoppingListShipDetails { get; } = [];

        public Fleet SelectedFleet { get; set; }

        private bool hideWhileBusy;
        public bool HideWhileBusy
        {
            get => hideWhileBusy;
            set => SetProperty( ref hideWhileBusy, value );
        }

        new public bool IsBusy
        {
            get => base.IsBusy;
            set
            {
                base.IsBusy = value;
                HideWhileBusy = !value;
            }
        }
        #endregion Fields and Properties


        #region Commands
        private AsyncCommand loadServicesCommand;
        private AsyncCommand LoadServicesCommand => loadServicesCommand ??= new AsyncCommand( LoadServices );
        #endregion Commands

        private async Task LoadServices()
        {
            shipDbs = await ServiceProvider.GetShipDatabaseServiceAsync();
            fleetDbs = await ServiceProvider.GetFleetDatabaseServiceAsync();
            taskGroupDbs = await ServiceProvider.GetTaskGroupDatabaseServiceAsync();
            shipDetailDbs = await ServiceProvider.GetShipDetailDatabaseServiceAsync();
            shipBalanceSheetDbs = await ServiceProvider.GetShipBalanceSheetDatabaseServiceAsync();
        }

        public async Task LoadTaskGroupViewModelsUsingFleetId( int fleetId, Action<TaskGroupViewModel_Populated> selectionChangedAction, Func<int, Task> RetaskAction, Func<int, Task> deleteGroupAction )
        {
            List<TaskGroup> taskGroups = await taskGroupDbs.GetChildrenUsingPropertyNameAsync( fleetId, nameof( TaskGroup.FleetId ) );

            PopulatedTaskGroupViewModels.Clear();

            foreach( TaskGroup taskGroup in taskGroups )
            {
                TaskGroupViewModel_Populated tgvm = new( this, taskGroup, selectionChangedAction, RetaskAction, deleteGroupAction );
                PopulatedTaskGroupViewModels.Add( tgvm );
            }
        }

        public async Task LoadShipDetailsUsingTaskGroupIdAsync( int id )
        {
            IsBusy = true;
            List<ShipDetail> shipDetails = await shipDetailDbs.GetChildrenUsingPropertyNameAsync( id, nameof( ShipDetail.TaskGroupId ) );

            PopulatedShipDetailViewModels.Clear();

            foreach( ShipDetail shipDetail in shipDetails )
            {
                ShipDetailViewModel_Populated sdvm = new ShipDetailViewModel_Populated( shipDetail, DeleteShipDetailViewModel, this );
                await sdvm.PopulateCommand.ExecuteAsync();
                PopulatedShipDetailViewModels.Add( sdvm );
            }

            IsBusy = false;
        }
    }
}
