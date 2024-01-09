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
        #endregion Commands
        #endregion Properties & Fields


        #region Methods

        private async Task Refresh()
        {
            Console.WriteLine( "ViewModel Refreshed" );
        }

        private async Task LoadFleets()
        {
            FleetDatabaseService dbService = await Services.ServiceProvider.GetFleetDatabaseServiceAsync();

            List<Fleet> fleetList = await dbService.GetAll();

            Fleets.Clear();
            foreach( Fleet fleet in fleetList )
            {
                FleetViewModel fvm = new FleetViewModel( fleet );
                Fleets.Add( fvm );
                Console.WriteLine( fvm.Name );
            }

            Console.WriteLine( "--------------" );

            Console.WriteLine( "Fleets Loaded" );
        }

        #endregion Methods

    }
}
