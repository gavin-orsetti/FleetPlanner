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
    public class FleetViewModel : BaseViewModel
    {
        public FleetViewModel()
        {
            fleet = new Fleet();
        }

        #region Fields
        public AsyncCommand RefreshParentView;
        #endregion Fields

        #region Properties & Commands
        readonly Fleet fleet;
        public Fleet Fleet => fleet;

        private int? id;
        public int Id
        {
            get => id ??= 0;
            set => SetProperty( ref id, value );
        }

        private string name;
        public string Name
        {
            get => name ??= string.Empty;
            set => SetProperty( ref name, value );
        }

        private string affiliation;
        public string Affiliation
        {
            get => affiliation ??= string.Empty;
            set => SetProperty( ref affiliation, value );
        }

        private string manifesto;
        public string Manifesto
        {
            get => manifesto ??= string.Empty;
            set => SetProperty( ref manifesto, value );
        }

        private string areaOfOperation;
        public string AreaOfOperation
        {
            get => areaOfOperation ??= string.Empty;
            set => SetProperty( ref areaOfOperation, value );
        }

        private ObservableRangeCollection<TaskGroup> taskGroups;
        public ObservableRangeCollection<TaskGroup> TaskGroups
        {
            get => taskGroups ??= [];
            set => SetProperty( ref taskGroups, value );
        }

        private AsyncCommand<int> goToFleetCommand;
        public AsyncCommand<int> GoToFleetCommand => goToFleetCommand ??= new AsyncCommand<int>( GoToFleet );

        private AsyncCommand<int> goToEditFleetCommand;
        public AsyncCommand<int> GoToEditFleetCommand => goToEditFleetCommand ??= new AsyncCommand<int>( GoToEditFleet );

        private AsyncCommand<int> deleteCommand;
        public AsyncCommand<int> DeleteCommand => deleteCommand ??= new AsyncCommand<int>( Delete );

        #endregion Properties & Commands

        #region Methods

        private async Task GoToFleet( int id )
        {
            await Shell.Current.GoToAsync( $"{Routes.FleetPage_PageName}" );
        }

        private async Task GoToEditFleet( int id )
        {
            await Shell.Current.GoToAsync( $"{Routes.EditFleetPage_PageName}" );
        }

        private async Task Delete( int id )
        {
            FleetDatabaseService dBService = await Services.ServiceProvider.GetFleetDatabaseServiceAsync();

            if( await dBService.DeleteAsync<Fleet>( id ) )
            {
                if( RefreshParentView != null )
                {
                    try
                    {
                        await RefreshParentView.ExecuteAsync();
                    }
                    catch( Exception )
                    {

                        throw;
                    }
                }
                else
                {
                    await Shell.Current.GoToAsync( Routes.BackOne );
                }
            }
        }

        #endregion Methods
    }
}
