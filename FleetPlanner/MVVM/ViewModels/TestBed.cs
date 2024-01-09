using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using FleetPlanner.MVVM.Models;

using MvvmHelpers;
using MvvmHelpers.Commands;

using Command = Microsoft.Maui.Controls.Command;

namespace FleetPlanner.MVVM.ViewModels
{
    public class TestBed : BaseViewModel
    {
        public TestBed()
        {
            Task.Run( async () => await Refresh() );
        }

        #region Properties & Fields

        private ObservableRangeCollection<ShipViewModel> ships;
        public ObservableRangeCollection<ShipViewModel> Ships
        {
            get => ships ??= [];
            set => SetProperty( ref ships, value );
        }


        #region Commands
        private AsyncCommand refreshCommand;
        public AsyncCommand RefreshCommand => refreshCommand ??= new AsyncCommand( Refresh );

        private AsyncCommand loadShipsCommand;
        public AsyncCommand LoadShipsCommand => loadShipsCommand ??= new AsyncCommand( LoadShips );
        #endregion Commands
        #endregion Properties & Fields


        #region Methods

        private async Task Refresh()
        {
            Console.WriteLine( "ViewModel Refreshed" );
        }

        private async Task LoadShips()
        {
            Ships.Clear();

            List<Ship> shipList = await Services.ServiceProvider.GetShips();

            foreach( Ship ship in shipList )
            {
                ShipViewModel svm = new ShipViewModel( ship );
                Ships.Add( svm );
                Console.WriteLine( svm.Name );
            }

            Console.WriteLine( "--------------" );

            Console.WriteLine( "Ships Loaded" );
        }

        #endregion Methods


    }
}
