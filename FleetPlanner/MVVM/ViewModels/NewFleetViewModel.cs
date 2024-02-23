using FleetPlanner.Helpers;
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
    public class NewFleetViewModel( GlobalViewModel global ) : FleetViewModel( global )
    {
        #region Properties
        #region Commands
        private AsyncCommand saveCommand;
        public AsyncCommand SaveCommand => saveCommand ??= new AsyncCommand( Save );
        #endregion Commands
        #endregion Properties

        #region Methods

        private async Task Save()
        {
            Fleet.Name = Name != string.Empty ? Name : Constants.DefaultFleetName;
            Fleet.Affiliation = Affiliation != string.Empty ? Affiliation : Constants.DefaultAffiliation;
            Fleet.AreaOfOperation = AreaOfOperation != string.Empty ? AreaOfOperation : Constants.DefaultAreaOfOperation;
            Fleet.Manifesto = Manifesto != string.Empty ? Manifesto : Constants.DefaultManifesto;
            Fleet.Notes = Notes != string.Empty ? Notes : string.Empty;

            FleetDatabaseService service = await ServiceProvider.GetFleetDatabaseServiceAsync();
            if( await service.Insert( Fleet ) )
            {
                Name = string.Empty;
                Affiliation = string.Empty;
                AreaOfOperation = string.Empty;
                Manifesto = string.Empty;

                await Shell.Current.GoToAsync( Routes.BackOne );
            }
            else
            {
                Console.WriteLine( "Item Insert Failed" );
            }
        }

        #endregion Methods
    }
}
