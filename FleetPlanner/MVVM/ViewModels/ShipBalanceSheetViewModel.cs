using FleetPlanner.MVVM.Models;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FleetPlanner.MVVM.ViewModels
{
    public class ShipBalanceSheetViewModel : ViewModelBase
    {
        public ShipBalanceSheetViewModel( ShipBalanceSheet bs )
        {
            balanceSheet = bs;
            Id = bs.Id;
            ShipDetailId = bs.ShipDetailId;
            Key = bs.Key;
            Value = bs.Value;
            IsPositive = bs.IsPositive;
        }

        private ShipBalanceSheet balanceSheet;

        private int id;
        public int Id
        {
            get => id;
            protected set => SetProperty( ref id, value );
        }

        private int shipDetailId;
        public int ShipDetailId
        {
            get => shipDetailId;
            protected set => SetProperty( ref shipDetailId, value );
        }

        private string key;
        public string Key
        {
            get => key;
            set => SetProperty( ref key, value );
        }

        private uint value;
        public uint Value
        {
            get => value;
            set => SetProperty( ref value, value );
        }

        private bool isPositive;
        public bool IsPositive
        {
            get => isPositive;
            set => SetProperty( ref isPositive, value );
        }
    }
}
