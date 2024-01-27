using FleetPlanner.Helpers;
using FleetPlanner.MVVM.Models;
using FleetPlanner.Services;

using MvvmHelpers;
using MvvmHelpers.Commands;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using System.Threading.Tasks;

using static FleetPlanner.Helpers.Constants;

using ServiceProvider = FleetPlanner.Services.ServiceProvider;

namespace FleetPlanner.MVVM.ViewModels
{
    public class ViewShipDetailViewModel : ShipDetailViewModel
    {
        private string role;
        public string Role
        {
            get => role;
            set => SetProperty( ref role, value );
        }

        private string make;
        public string Make
        {
            get => make;
            set => SetProperty( ref make, value );
        }

        private string model;
        public string Model
        {
            get => model;
            set => SetProperty( ref model, value );
        }

        private ObservableRangeCollection<ShipBalanceSheetViewModel> balanceSheet;
        public ObservableRangeCollection<ShipBalanceSheetViewModel> BalanceSheet => balanceSheet ??= new();

        private AsyncCommand<int> goToReTaskCommand;
        public AsyncCommand<int> GoToReTaskCommand => goToReTaskCommand ??= new AsyncCommand<int>( GoToReTask );

        #region Methods

        private async Task GoToReTask( int id )
        {
            Dictionary<string, object> queryParams = new()
            {
                { Routes.ReTaskQueryParams.Id, id }
            };

            await Shell.Current.GoToAsync( Routes.ReTaskShip_Page_PageName, queryParams );
        }

        #region Query Handling
        private protected override async Task EvaluateQueryParams( KeyValuePair<string, object> kvp )
        {
            switch( kvp.Key )
            {
                case Routes.ShipDetailQueryParams.Id:
                    Console.WriteLine( "Id Passed to View Ship Detail Page" );
                    await Populate( (int)kvp.Value );
                    break;
                default:
                    break;
            }
        }

        new private async Task Populate( int id )
        {
            await base.Populate( id );

            ShipDatabaseService shipDbs = await ServiceProvider.GetShipDatabaseServiceAsync();
            ShipBalanceSheetDatabaseService shipBalanceSheetDbs = await ServiceProvider.GetShipBalanceSheetDatabaseServiceAsync();

            Ship s = await shipDbs.GetRow( ShipDetail.ShipId );

            shipViewModel = new ShipViewModel( s );

            Make = ( (ShipManufacturer)shipViewModel.Make ).ToString().SplitCamelCase();
            Model = shipViewModel.Model;
            Role = shipViewModel.Role;

            List<ShipBalanceSheet> sheetItems = await shipBalanceSheetDbs.GetChildrenUsingPropertyName( ShipDetail.Id, nameof( ShipBalanceSheet.ShipDetailId ) );

            List<ShipBalanceSheetViewModel> bsVms = new();

            foreach( ShipBalanceSheet sheetItem in sheetItems )
            {
                ShipBalanceSheetViewModel bsvm = new ShipBalanceSheetViewModel( sheetItem );
                bsVms.Add( bsvm );
            }

            BalanceSheet.Clear();
            BalanceSheet.AddRange( bsVms );
        }
        #endregion Query Handling
        #endregion Methods

    }
}
