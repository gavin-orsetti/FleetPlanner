using FleetPlanner.Helpers;
using FleetPlanner.MVVM.Models;
using FleetPlanner.Services;

using MvvmHelpers.Commands;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using static System.Runtime.InteropServices.JavaScript.JSType;

using Command = Microsoft.Maui.Controls.Command;
using ServiceProvider = FleetPlanner.Services.ServiceProvider;

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

        private uint storedValue;
        public uint Value
        {
            get => storedValue;
            set => SetProperty( ref storedValue, value );
        }

        private bool isPositive;
        public bool IsPositive
        {
            get => isPositive;
            set
            {
                SetProperty( ref isPositive, value );
                Color = value ? TertiaryColor : ErrorColor;
                TextColor = value ? OnTertiaryColor : OnErrorColor;
            }
        }

        private Color color;
        public Color Color
        {
            get => color;
            set => SetProperty( ref color, value );
        }

        private Color textColor;
        public Color TextColor
        {
            get => textColor;
            set => SetProperty( ref textColor, value );
        }

        private Color onErrorColor;
        public Color OnErrorColor
        {
            get
            {
                if( onErrorColor != null )
                    return onErrorColor;

                if( Application.Current.Resources.TryGetValue( "OnError", out object c ) )
                    OnErrorColor = (Color)c;

                return onErrorColor ??= Constants.ColorResourceLookupFailed;
            }
            set => SetProperty( ref onErrorColor, value );
        }

        private Color onTertiaryColor;
        public Color OnTertiaryColor
        {
            get
            {
                if( onTertiaryColor != null )
                    return onTertiaryColor;

                if( Application.Current.Resources.TryGetValue( "OnTertiary", out object c ) )
                    OnTertiaryColor = (Color)c;

                return onTertiaryColor ??= Constants.ColorResourceLookupFailed;
            }
            set => SetProperty( ref onTertiaryColor, value );
        }

        private Color errorColor;
        public Color ErrorColor
        {
            get
            {
                if( errorColor != null )
                    return errorColor;

                if( Application.Current.Resources.TryGetValue( "Error", out object c ) )
                    ErrorColor = (Color)c;

                return errorColor ??= Constants.ColorResourceLookupFailed;
            }
            set => SetProperty( ref errorColor, value );
        }

        private Color tertiaryColor;
        public Color TertiaryColor
        {
            get
            {
                if( tertiaryColor != null )
                    return tertiaryColor;

                if( Application.Current.Resources.TryGetValue( "Tertiary", out object c ) )
                    TertiaryColor = (Color)c;

                return tertiaryColor ??= Constants.ColorResourceLookupFailed;
            }
            set => SetProperty( ref tertiaryColor, value );
        }

        private AsyncCommand updateCommand;
        public AsyncCommand UpdateCommand => updateCommand ??= new AsyncCommand( Update );

        private Command flipSignCommand;
        public Command FlipSignCommand => flipSignCommand ??= new Command( FlipSign );

        private void FlipSign()
        {
            IsPositive = !IsPositive;

            if( IsPositive )
            {
                Color = TertiaryColor;
                TextColor = OnTertiaryColor;
            }
            else
            {
                Color = ErrorColor;
                TextColor = OnErrorColor;
            }
        }

        private async Task Update()
        {
            ShipBalanceSheetDatabaseService sbsDbs = await ServiceProvider.GetShipBalanceSheetDatabaseServiceAsync();

            balanceSheet.Key = Key;
            balanceSheet.Value = Value;
            balanceSheet.IsPositive = IsPositive;

            await sbsDbs.Update( balanceSheet );

        }
    }
}
