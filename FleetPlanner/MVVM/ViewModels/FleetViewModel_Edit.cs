using FleetPlanner.MVVM.Models;
using FleetPlanner.Services;

using MvvmHelpers.Commands;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using ServiceProvider = FleetPlanner.Services.ServiceProvider;

namespace FleetPlanner.MVVM.ViewModels
{
    public class FleetViewModel_Edit : FleetViewModel
    {
        private AsyncCommand saveCommand;
        public AsyncCommand SaveCommand => saveCommand ??= new AsyncCommand( Save );

        private async Task Save()
        {
            FleetDatabaseService fleetDbs = await ServiceProvider.GetFleetDatabaseServiceAsync();

            Fleet.Name = Name;
            Fleet.AreaOfOperation = AreaOfOperation;
            Fleet.Affiliation = Affiliation;
            Fleet.Manifesto = Manifesto;
            Fleet.Notes = Notes;

            await fleetDbs.Update( Fleet );
            await Shell.Current.GoToAsync( Routes.BackOne );
        }

        private protected override async Task EvaluateQueryParams( KeyValuePair<string, object> kvp )
        {
            switch( kvp.Key )
            {
                case Routes.FleetQueryParams.EditViewModel:
                    fleet = (Fleet)kvp.Value;
                    await Populate();
                    break;
                case Routes.FleetQueryParams.Id:
                    Id = (int)kvp.Value;
                    await Populate();
                    break;
                default:
                    break;
            }
        }

        new private protected async Task Populate()
        {
            FleetDatabaseService fleetDbs = await ServiceProvider.GetFleetDatabaseServiceAsync();
            fleet = await fleetDbs.GetRow( Id );

            await base.Populate();
        }
    }
}
