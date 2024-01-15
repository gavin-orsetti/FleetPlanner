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
    public class FleetViewModel : ViewModelBase
    {
        public FleetViewModel()
        { }

        #region Fields
        public AsyncCommand RefreshParentView;
        #endregion Fields

        #region Properties & Commands
        private Fleet fleet;
        public Fleet Fleet => fleet ??= new Fleet();

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


        private AsyncCommand<int> deleteCommand;
        public AsyncCommand<int> DeleteCommand => deleteCommand ??= new AsyncCommand<int>( Delete );

        #endregion Properties & Commands

        #region Methods





        private async Task Delete( int id )
        {
            if( id <= 0 )
            {
                throw new Exception( "Error 0: Object does not exist in database" );
            }

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

        #region Query Handling

        private protected override Task EvaluateQueryParams( KeyValuePair<string, object> kvp )
        {
            switch( kvp.Key )
            {
                case Routes.FleetQueryParams.PopulatedViewModel:
                    fleet = kvp.Value as Fleet;
                    //Populate();
                    break;

                default:
                    break;
            }
            return null;
        }

        private void Populate()
        {
            Id = Fleet.Id;
            Name = Fleet.Name;
            Affiliation = Fleet.Affiliation;
            AreaOfOperation = Fleet.AreaOfOperation;
            Manifesto = Fleet.Manifesto;
            //if( Fleet.TaskGroups != null )
            //{
            //    TaskGroups.Clear();
            //    TaskGroups.AddRange( Fleet.TaskGroups );
            //}
            //else
            //{
            //    Fleet.TaskGroups = new List<TaskGroup>();
            //}
        }

        #endregion Query Handling

        #endregion Methods
    }
}
