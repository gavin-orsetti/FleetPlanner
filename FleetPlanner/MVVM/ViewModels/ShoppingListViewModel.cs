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
            ShipDetailDatabaseService shipDetailDbs = await ServiceProvider.GetShipDetailDatabaseServiceAsync();
            List<ShipDetail> shps = await shipDetailDbs.GetAll();
            List<ShipDetailViewModel_Populated> popShips = [];
            foreach( ShipDetail shp in shps )
            {
                if( shp.Purchased )
                { continue; }

                ShipDetailViewModel_Populated sdvm = new ShipDetailViewModel_Populated( shp, Delete );
                await sdvm.PopulateCommand.ExecuteAsync();
                popShips.Add( sdvm );
            }

            Ships.Clear();
            Ships.AddRange( popShips );
        }
        #endregion Query Handling
        #endregion Methods
    }
}
