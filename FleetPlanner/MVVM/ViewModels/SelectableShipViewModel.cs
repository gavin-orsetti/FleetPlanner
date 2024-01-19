using FleetPlanner.MVVM.Models;

using MvvmHelpers.Commands;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Command = MvvmHelpers.Commands.Command;

namespace FleetPlanner.MVVM.ViewModels
{
    public class SelectableShipViewModel( Ship ship ) : ShipViewModel( ship )
    {
        private bool _selected = false;
        public bool Selected
        {
            get => _selected;
            set
            {
                SetProperty( ref _selected, value );

                if( value && Quantity < 1 )
                {
                    Quantity = 1;
                }

                if( !value && Quantity > 0 )
                {
                    Quantity = 0;
                }

            }
        }

        private int quantity;
        public int Quantity
        {
            get => quantity;
            set
            {
                if( Selected && value < 1 )
                {
                    SetProperty( ref quantity, 0 );
                    Selected = false;
                }

                else if( !Selected && value > 0 )
                {
                    SetProperty( ref quantity, value );
                    Selected = true;
                }
                else
                {
                    SetProperty( ref quantity, value, validateValue: ( _, newValue ) => newValue >= 0 );
                }
            }
        }

        private Command addQuantityCommand;
        public Command AddQuantityCommand => addQuantityCommand ??= new Command( AddQuantity );

        private void AddQuantity()
        {
            Quantity += 1;
        }

        private Command removeQuantityCommand;
        public Command RemoveQuantityCommand => removeQuantityCommand ??= new Command( RemoveQuantity );

        private void RemoveQuantity()
        {
            Quantity -= 1;
        }
    }
}
