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
    public class ShoppingListViewModel : ViewModelBase
    {
        private ObservableRangeCollection<ShipDetailViewModel_Populated> ships;
        public ObservableRangeCollection<ShipDetailViewModel_Populated> Ships => ships ??= [];

        private ObservableRangeCollection<FleetViewModel_Populated> fleets;
        public ObservableRangeCollection<FleetViewModel_Populated> Fleets => fleets ??= [];

        private AsyncCommand populateCommand;
        public AsyncCommand PopulateCommand => populateCommand ??= new AsyncCommand( Populate );

        #region Methods
        private void Delete( int id )
        {
            Console.WriteLine( "Delete Called" ); // TODO: Make this remove an entry from the Ships collection (without deleting it from the db of course)
        }
        #region Query Handling
        private protected override async Task EvaluateQueryParams( KeyValuePair<string, object> kvp )
        {
            switch( kvp.Key )
            {
                case Routes.ShoppingListQueryParams.LoadList:
                    await Populate();
                    break;
                default:
                    break;
            }
        }

        private protected async Task Populate()
        {
            FleetDatabaseService fleetDbs = await ServiceProvider.GetFleetDatabaseServiceAsync();
            List<Fleet> f = await fleetDbs.GetAll();
            List<FleetViewModel_Populated> f_p = [];
            foreach( Fleet fleet in f )
            {
                FleetViewModel_Populated fvm_p = new FleetViewModel_Populated( fleet );
                f_p.Add( fvm_p );
            }

            Fleets.Clear();
            Fleets.AddRange( f_p );
        }
        #endregion Query Handling
        #endregion Methods
    }
}
