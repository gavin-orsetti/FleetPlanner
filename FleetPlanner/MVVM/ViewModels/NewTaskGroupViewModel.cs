using FleetPlanner.MVVM.Models;
using FleetPlanner.Services;

using MvvmHelpers;
using MvvmHelpers.Commands;

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

        private ObservableRangeCollection<SelectableShipViewModel> ships;
        public ObservableRangeCollection<SelectableShipViewModel> Ships => ships ??= [];

        private ObservableRangeCollection<SelectableShipViewModel> displayedShips;
        public ObservableRangeCollection<SelectableShipViewModel> DisplayedShips => displayedShips ??= [];

        private AsyncCommand saveCommand;
        public AsyncCommand SaveCommand => saveCommand ??= new AsyncCommand( Save );

        #region Methods

        private async Task Save()
        {
            Console.WriteLine( Ships.Where( x => x.Selected ).Count() );
        }

        private async Task LoadShips()
        {
            ShipDatabaseService shipDbService = await Services.ServiceProvider.GetShipDatabaseServiceAsync();
            List<Ship> shipModels = await shipDbService.GetAll();
            List<SelectableShipViewModel> ssvms = [];

            foreach( Ship s in shipModels )
            {
                SelectableShipViewModel ssvm = new( s );
                ssvms.Add( ssvm );
            }

            Ships.Clear();
            Ships.AddRange( ssvms );

            DisplayedShips.Clear();
            DisplayedShips.AddRange( ssvms );

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
