﻿using FleetPlanner.MVVM.Models;
using FleetPlanner.Services;

using MvvmHelpers;
using MvvmHelpers.Commands;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

using Command = MvvmHelpers.Commands.Command;
using ServiceProvider = FleetPlanner.Services.ServiceProvider;

namespace FleetPlanner.MVVM.ViewModels
{
    public class ShoppingListFleetSelectedViewModel : ViewModelBase
    {
        private int? id;
        private List<Ship> shipList;

        private ObservableRangeCollection<ShoppingListShipDetailViewModel> ships;
        public ObservableRangeCollection<ShoppingListShipDetailViewModel> Ships => ships ??= [];


        #region Methods
        private void ToggleIsBusy()
        {
            IsBusy = !IsBusy;
        }

        private void Delete( int id )
        {
            ShoppingListShipDetailViewModel s = Ships.Where( x => x.Id == id ).FirstOrDefault();

            Ships.Remove( s );
        }
        #region Query Handling
        private protected override async Task EvaluateQueryParams( KeyValuePair<string, object> kvp )
        {
            IsBusy = true;
            switch( kvp.Key )
            {
                case Routes.ShoppingListQueryParams.Id:
                    id = (int)kvp.Value;
                    if( id != null && id > 0 )
                    {
                        await Populate( (int)kvp.Value );
                    }
                    break;
                default:
                    break;
            }
            IsBusy = false;
        }

        private protected async Task Populate( int id )
        {

            ShipDetailDatabaseService shipDetailDbs = await ServiceProvider.GetShipDetailDatabaseServiceAsync();
            ShipDatabaseService shipDbs = await ServiceProvider.GetShipDatabaseServiceAsync();
            shipList = await shipDbs.GetAll();
            List<ShipDetail> shps = await shipDetailDbs.GetChildrenUsingPropertyNameAsync( id, nameof( ShipDetail.FleetId ) );
            List<ShoppingListShipDetailViewModel> popShips = [];
            foreach( ShipDetail shp in shps )
            {
                if( shp.Purchased )
                { continue; }
                Ship s = shipList.Where( x => x.Id == shp.ShipId ).FirstOrDefault();
                ShoppingListShipDetailViewModel sdvm = new ShoppingListShipDetailViewModel( shp, Delete, s );
                await sdvm.PopulateCommand.ExecuteAsync();
                popShips.Add( sdvm );
            }

            Ships.Clear();
            Ships.AddRange( popShips );

        }
    }
    #endregion Query Handling
    #endregion Methods
}

