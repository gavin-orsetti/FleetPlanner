using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using FleetPlanner.MVVM.Models;
using FleetPlanner.Services;

using MvvmHelpers;
using MvvmHelpers.Commands;

namespace FleetPlanner.MVVM.ViewModels
{
    public class MainPageViewModel : BaseViewModel
    {
        #region Constructors
        public MainPageViewModel()
        {
            Task.Run( async () => await Refresh() );
        }
        #endregion Constructors

        #region Properties & Fields

        private ObservableRangeCollection<FleetViewModel> fleets;
        public ObservableRangeCollection<FleetViewModel> Fleets
        {
            get => fleets ??= [];
            set => SetProperty( ref fleets, value );
        }


        #region Commands
        private AsyncCommand refreshCommand;
        public AsyncCommand RefreshCommand => refreshCommand ??= new AsyncCommand( Refresh );

        private AsyncCommand loadFleetsCommand;
        public AsyncCommand LoadFleetsCommand => loadFleetsCommand ??= new AsyncCommand( LoadFleets );

        private AsyncCommand goToFleetNewPageCommand;
        public AsyncCommand GoToFleetNewPageCommand => goToFleetNewPageCommand ??= new AsyncCommand( GoToFleetNewPage );
        #endregion Commands
        #endregion Properties & Fields


        #region Methods

        private async Task Refresh()
        {
            await LoadFleets();
        }

        //TODO: Remove calls to Console.WriteLine()
        private async Task LoadFleets()
        {
            FleetDatabaseService dbService = await Services.ServiceProvider.GetFleetDatabaseServiceAsync();

            List<Fleet> fleetList = await dbService.GetAll();

            Fleets.Clear();
            foreach( Fleet fleet in fleetList )
            {
                FleetViewModel fvm = new( fleet );
                Fleets.Add( fvm );
                Console.WriteLine( fvm.Name );
            }

            Console.WriteLine( "--------------" );

            Console.WriteLine( "Fleets Loaded" );
        }

        private async Task GoToFleetNewPage()
        {
            await Shell.Current.GoToAsync( Routes.MyFleets_NewFleet_PageName );
        }
        #endregion Methods

    }
}
