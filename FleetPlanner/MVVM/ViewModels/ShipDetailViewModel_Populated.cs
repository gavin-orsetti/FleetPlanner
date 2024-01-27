using FleetPlanner.Helpers;
using FleetPlanner.MVVM.Models;
using FleetPlanner.Services;

using MvvmHelpers.Commands;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using static FleetPlanner.Helpers.Constants;

using ServiceProvider = FleetPlanner.Services.ServiceProvider;

namespace FleetPlanner.MVVM.ViewModels
{
    public class ShipDetailViewModel_Populated : ShipDetailViewModel
    {
        public ShipDetailViewModel_Populated( ShipDetail ship )
        {
            ShipDetail = ship;
        }

        private string make;
        public string Make
        {
            get => make ??= ( (ShipManufacturer)shipViewModel.Make ).ToString().SplitCamelCase();
            set => SetProperty( ref make, value );
        }

        private string model;
        public string Model
        {
            get => model ??= shipViewModel.Model;
            set => SetProperty( ref model, value );
        }

        private string role;
        public string Role
        {
            get => role ??= shipViewModel.Role;
            set => SetProperty( ref role, value );
        }

        #region Commands
        private AsyncCommand populateCommand;
        public AsyncCommand PopulateCommand => populateCommand ??= new AsyncCommand( Populate );

        #endregion Commands
        #region Methods

        #region Async Initialization
        private async Task Populate()
        {
            await base.Populate( ShipDetail.Id );

            ShipDatabaseService shipDbs = await ServiceProvider.GetShipDatabaseServiceAsync();
            Ship ship = await shipDbs.GetRow( ShipDetail.ShipId );
            shipViewModel = new ShipViewModel( ship );

            Make = ( (ShipManufacturer)shipViewModel.Make ).ToString().SplitCamelCase();
            Model = shipViewModel.Model;
            Role = shipViewModel.Role;
        }
        #endregion Async Initialization
        #endregion Methods
    }
}
