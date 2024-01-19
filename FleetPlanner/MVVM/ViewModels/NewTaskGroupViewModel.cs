using FleetPlanner.MVVM.Models;
using FleetPlanner.Services;

using MvvmHelpers;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FleetPlanner.MVVM.ViewModels
{
    public class NewTaskGroupViewModel : TaskGroupViewModel
    {
        private string searchString;
        public string SearchString
        {
            get => searchString ??= string.Empty;
            set
            {
                if( value != string.Empty || value != null )
                {
                    DisplayedShips.Clear();
                    DisplayedShips.AddRange( Ships.Where( x => x.Name.ToLower().Contains( value.ToLower() ) ) );
                }

                if( value == string.Empty || value == null )
                {
                    DisplayedShips.Clear();
                    DisplayedShips.AddRange( Ships );
                }

                SetProperty( ref searchString, value );
            }
        }

        private ObservableRangeCollection<ShipViewModel> ships;
        public ObservableRangeCollection<ShipViewModel> Ships => ships ??= [];

        private ObservableRangeCollection<ShipViewModel> displayedShips;
        public ObservableRangeCollection<ShipViewModel> DisplayedShips => displayedShips ??= [];

        private List<string> shipNames;
        public List<string> ShipNames => shipNames ??= [];



        #region Methods

        private async Task LoadShips()
        {
            ShipDatabaseService shipDbService = await Services.ServiceProvider.GetShipDatabaseServiceAsync();
            List<Ship> shipModels = await shipDbService.GetAll();
            List<ShipViewModel> svms = [];
            ShipNames.Clear();

            foreach( Ship s in shipModels )
            {
                ShipViewModel svm = new( s );
                svms.Add( svm );
                ShipNames.Add( s.Name );

                Console.WriteLine( s.Name );
            }

            Ships.Clear();
            Ships.AddRange( svms );
            DisplayedShips.AddRange( svms );
        }

        #region Query Handling
        private protected override async Task EvaluateQueryParams( KeyValuePair<string, object> kvp )
        {
            switch( kvp.Key )
            {
                case Routes.TaskGroupQueryParams.FleetId:
                    await LoadShips();
                    FleetId = Convert.ToInt32( kvp.Value );
                    break;
                case Routes.TaskGroupQueryParams.Fleet:
                    Console.WriteLine( "Fleet Passed" );
                    break;
                default:
                    break;
            }

            await base.EvaluateQueryParams( kvp );
        }
        #endregion Query Handling
        #endregion Methods

    }
}
