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
        public ShipBalanceSheetViewModel( ShipBalanceSheet bs, Func<int, Task> delete )
        {
            balanceSheet = bs;
            Id = bs.Id;
            ShipDetailId = bs.ShipDetailId;
            Key = bs.Key;
            Value = bs.Value;
            IsPositive = bs.IsPositive;

            deleteCommand = new AsyncCommand<int>( delete );
        }
        private Action valueUpdated;
        public Action ValueUpdated
        {
            get => valueUpdated ??= () => { }; // We need this empty lambda because we call ValueUpdated from our property setters, which are triggered before we add anything to the action and throw a NullReferenceException if we try and call an empty action.
            set => SetProperty( ref valueUpdated, value );
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
            set
            {
                SetProperty( ref storedValue, value );
                ValueUpdated.Invoke();
            }
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
                Sign = value ? NegativeSign : PositiveSign; // if the value is positive we want to show the negative sign so the user knows that tapping the icon will make the number negative and visa versa
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

        private FontImageSource sign;
        public FontImageSource Sign
        {
            get => sign;
            set => SetProperty( ref sign, value );
        }

        private FontImageSource negativeSign;
        public FontImageSource NegativeSign
        {
            get
            {
                if( negativeSign != null )
                    return negativeSign;

                FontImageSource fis = new FontImageSource();
                fis.FontFamily = "MaterialSharp";
                fis.Glyph = UraniumUI.Icons.MaterialIcons.MaterialSharp.Exposure;
                NegativeSign = fis;
                return fis;
            }
            set => SetProperty( ref negativeSign, value );

        }

        private FontImageSource positiveSign;
        public FontImageSource PositiveSign
        {
            get
            {
                if( positiveSign != null )
                    return positiveSign;

                FontImageSource fis = new FontImageSource();
                fis.FontFamily = "MaterialSharp";
                fis.Glyph = UraniumUI.Icons.MaterialIcons.MaterialSharp.Iso;
                PositiveSign = fis;
                return fis;
            }
            set => SetProperty( ref positiveSign, value );
        }

        private AsyncCommand updateCommand;
        public AsyncCommand UpdateCommand => updateCommand ??= new AsyncCommand( Update );

        private Command flipSignCommand;
        public Command FlipSignCommand => flipSignCommand ??= new Command( FlipSign );

        private AsyncCommand<int> deleteCommand;
        public AsyncCommand<int> DeleteCommand => deleteCommand;

        private void FlipSign()
        {
            IsPositive = !IsPositive;
            ValueUpdated.Invoke();
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
