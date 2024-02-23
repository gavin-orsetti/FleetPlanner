using FleetPlanner.MVVM.Models;

using MvvmHelpers;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FleetPlanner.MVVM.ViewModels
{
    public class ShipViewModel( Ship ship, GlobalViewModel global ) : ViewModelBase( global )
    {
        readonly Ship ship = ship;
        public Ship Ship => ship;

        private int? id;
        public int Id
        {
            get => id ??= Ship.Id;
            private set => SetProperty( ref id, value );
        }

        private string model;
        public string Model
        {
            get => model ??= Ship.Model;
            private set => SetProperty( ref model, value );
        }

        private int? make;
        public int Make
        {
            get => make ??= Ship.Make;
            private set => SetProperty( ref make, value );
        }

        private string description;
        public string Description
        {
            get => description ??= Ship.Description;
            private set => SetProperty( ref description, value );
        }

        private string role;
        public string Role
        {
            get => role ??= Ship.Role;
            private set => SetProperty( ref role, value );
        }

        private int? size;
        public int Size
        {
            get => size ??= Ship.Size;
            private set => SetProperty( ref size, value );
        }

        private int? crew_min;
        public int Crew_min
        {
            get => crew_min ??= Ship.Crew_Min;
            private set => SetProperty( ref crew_min, value );
        }

        private int? crew_max;
        public int Crew_max
        {
            get => crew_max ??= Ship.Crew_Max;
            private set => SetProperty( ref crew_max, value );
        }

        private int? scu;
        public int Scu
        {
            get => scu ??= Ship.Scu;
            private set => SetProperty( ref scu, value );
        }


        private int? stowage;
        public int Stowage
        {
            get => stowage ??= Ship.Stowage;
            private set => SetProperty( ref stowage, value );
        }


        private int? livePriceUSD;
        public int LivePriceUSD
        {
            get => livePriceUSD ??= Ship.LivePriceUSD;
            private set => SetProperty( ref livePriceUSD, value );
        }

        private int? livePriceAuec;
        public int LivePriceAuec
        {
            get => livePriceAuec ??= Ship.LivePriceAuec;
            private set => SetProperty( ref livePriceAuec, value );
        }

    }
}
