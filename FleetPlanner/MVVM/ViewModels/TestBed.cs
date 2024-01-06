﻿using System;
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

        private ObservableRangeCollection<Ship> ships;
        public ObservableRangeCollection<Ship> Ships
        {
            get => ships ??= new ObservableRangeCollection<Ship>();
            set => ships = value;
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
            Ships.AddRange( await Services.ServiceProvider.GetShips() );

            foreach( Ship ship in Ships )
            {
                Console.WriteLine( ship.Name );
            }

            Console.WriteLine( "--------------" );

            Console.WriteLine( "Ships Loaded" );
        }

        #endregion Methods


    }
}
