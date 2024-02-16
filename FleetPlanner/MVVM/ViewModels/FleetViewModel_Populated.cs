using FleetPlanner.MVVM.Models;

using MvvmHelpers;
using MvvmHelpers.Commands;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FleetPlanner.MVVM.ViewModels
{
    public class FleetViewModel_Populated( Fleet fleet ) : FleetViewModel
    {
        new private protected Fleet fleet = fleet;
        new public Fleet Fleet => fleet;

        private int? id;
        new public int Id
        {
            get => id ??= Fleet.Id;
            private set => SetProperty( ref id, value );
        }

        private string name;
        new public string Name
        {
            get => name ??= Fleet.Name;
            private set => SetProperty( ref name, value );
        }

        private string affiliation;
        new public string Affiliation
        {
            get => affiliation ??= Fleet.Affiliation;
            private set => SetProperty( ref affiliation, value );
        }

        private string manifesto;
        new public string Manifesto
        {
            get => manifesto ??= Fleet.Manifesto;
            private set => SetProperty( ref manifesto, value );
        }

        public string Manifesto_150
        {
            get => Manifesto != string.Empty && Manifesto.Length > 150 ? Manifesto[ ..150 ].Trim() : Manifesto;
        }

        private string areaOfOperation;
        new public string AreaOfOperation
        {
            get => areaOfOperation ??= Fleet.AreaOfOperation;
            private set => SetProperty( ref areaOfOperation, value );
        }

        private ObservableRangeCollection<TaskGroup> taskGroups;
        new public ObservableRangeCollection<TaskGroup> TaskGroups
        {
            get => taskGroups ??= [];
            private set => SetProperty( ref taskGroups, value );
        }

        //private AsyncCommand<int> goToEditFleetCommand;
        //new public AsyncCommand<int> GoToEditFleetCommand => goToEditFleetCommand ??= new AsyncCommand<int>( GoToEditFleet );

        private AsyncCommand<int> goToFleetCommand;
        public AsyncCommand<int> GoToFleetCommand => goToFleetCommand ??= new AsyncCommand<int>( GoToFleet );

        private AsyncCommand<int> openFleetShoppingListCommand;
        public AsyncCommand<int> OpenFleetShoppingListCommand => openFleetShoppingListCommand ??= new AsyncCommand<int>( OpenFleetShoppingList );

        private async Task GoToFleet( int id )
        {
            Dictionary<string, object> queryParams = new()
            {
                { Routes.FleetQueryParams.PopulatedViewModel, Fleet }
            };

            await Shell.Current.GoToAsync( $"{Routes.Fleet_Page_PageName}", true, queryParams );
        }

        private async Task OpenFleetShoppingList( int id )
        {
            Dictionary<string, object> queryParams = new()
            {
                {Routes.ShoppingListQueryParams.Id, id }

            };

            await Shell.Current.GoToAsync( Routes.ShoppingList_FleetSelectedPage_PageName, queryParams );
        }
    }
}
