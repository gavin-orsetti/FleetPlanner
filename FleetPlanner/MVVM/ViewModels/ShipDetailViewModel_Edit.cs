using FleetPlanner.Helpers;
using FleetPlanner.MVVM.Models;
using FleetPlanner.Services;

using MvvmHelpers;
using MvvmHelpers.Commands;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using System.Threading.Tasks;

using ServiceProvider = FleetPlanner.Services.ServiceProvider;

namespace FleetPlanner.MVVM.ViewModels
{
    public class ShipDetailViewModel_Edit : ShipDetailViewModel
    {
        private string make;
        public string Make
        {
            get => make;
            set => SetProperty( ref make, value );
        }

        private string model;
        public string Model
        {
            get => model;
            set => SetProperty( ref model, value );
        }

        private string role;
        public string Role
        {
            get => role;
            set => SetProperty( ref role, value );
        }

        private ObservableRangeCollection<string> currencies;
        public ObservableRangeCollection<string> Currencies => currencies ??=
        [
            Constants.Currency.USD.ToString(),
            Constants.Currency.EUR.ToString(),
            Constants.Currency.GBP.ToString(),
            Constants.Currency.UEC.ToString()
        ];

        private ObservableRangeCollection<string> insuranceTypes;
        public ObservableRangeCollection<string> InsuranceTypes => insuranceTypes ??=
        [
            Constants.InsuranceType.ThreeMonth.ToSplitString(),
            Constants.InsuranceType.SixMonth.ToSplitString(),
            Constants.InsuranceType.TwelveMonth.ToSplitString(),
            Constants.InsuranceType.OneHundredTwentyMonth.ToSplitString(),
            Constants.InsuranceType.Lifetime.ToSplitString()
        ];

        private AsyncCommand saveCommand;
        public AsyncCommand SaveCommand => saveCommand ??= new AsyncCommand( Save );

        private AsyncCommand addNewBalanceSheetItemCommand;
        public AsyncCommand AddNewBalanceSheetItemCommand => addNewBalanceSheetItemCommand ??= new AsyncCommand( AddNewBalanceSheetItem );

        #region Methods

        private async Task Save()
        {
            if( Id > 0 )
            {
                await Update();
            }
            if( Id <= 0 )
            {
                throw new Exception( "Trying to update Ship Detail that does not exist in db" );
            }
        }

        private async Task AddNewBalanceSheetItem()
        {
            ShipBalanceSheetDatabaseService shipBalanceSheetDbs = await ServiceProvider.GetShipBalanceSheetDatabaseServiceAsync();

            ShipBalanceSheet sbs = new()
            {
                ShipDetailId = Id,
                Key = Constants.DefaultBalanceSheetItemKey,
                Value = 0,
                IsPositive = false
            };

            if( await shipBalanceSheetDbs.Insert( sbs ) )
            {
                Console.WriteLine( sbs.Id );
                if( sbs.Id > 0 )
                {
                    ShipBalanceSheetViewModel sbsvm = new ShipBalanceSheetViewModel( sbs );

                    BalanceSheet.Add( sbsvm );
                }
            }

        }

        private async Task Update()
        {
            ShipDetailDatabaseService shipDetailDbs = await ServiceProvider.GetShipDetailDatabaseServiceAsync();

            ShipDetail.Callsign = Callsign;
            ShipDetail.Responsibility = Responsibility;
            ShipDetail.PersonalAttachmentRating = PersonalAttachmentRating;
            ShipDetail.Integrality = Integrality;
            ShipDetail.Notes = Notes;
            ShipDetail.NpcCrewMax = NpcCrewMax;
            ShipDetail.NpcCrewMin = NpcCrewMin;
            ShipDetail.PlayerCrewMin = PlayerCrewMin;
            ShipDetail.PlayerCrewMax = PlayerCrewMax;
            ShipDetail.CrewTotalMax = CrewTotalMax;
            ShipDetail.CrewTotalMin = CrewTotalMin;
            ShipDetail.HourlyIncome = HourlyIncome;
            ShipDetail.ExpectedProfit = ExpectedProfit;
            ShipDetail.Purchased = Purchased;
            ShipDetail.Currency = (int)CurrencyAsEnum;
            ShipDetail.CashPurchase = CashPurchase;
            ShipDetail.MeltValue = MeltValue;
            ShipDetail.InsuranceType = (int)InsuranceTypeAsEnum;
            ShipDetail.AnnualInsuranceCost = AnnualInsuranceCost;

            foreach( ShipBalanceSheetViewModel sbsvm in BalanceSheet )
            {
                await sbsvm.UpdateCommand.ExecuteAsync();
            }

            await shipDetailDbs.Update( ShipDetail );

            Dictionary<string, object> queryParams = new()
            {
                { Routes.CommonQueryParams.Refresh, ShipDetail.Id }
            };

            await Shell.Current.GoToAsync( Routes.BackOne, queryParams );
        }

        #region Query Handling
        private protected override async Task EvaluateQueryParams( KeyValuePair<string, object> kvp )
        {
            switch( kvp.Key )
            {
                case Routes.ShipDetailQueryParams.Id:
                    await Populate( (int)kvp.Value );
                    break;
                case Routes.ShipDetailQueryParams.Object:
                    await Populate( (ShipDetail)kvp.Value );
                    break;
                default:
                    break;
            }
        }

        new private async Task Populate( int id )
        {
            await base.Populate( id );

            (Make, Model, Role) = await GetShipViewModel( ShipDetail.ShipId );

            List<ShipBalanceSheetViewModel> sbsVMs = await GetShipBalanceSheetItemViewModels();

            BalanceSheet.Clear();
            BalanceSheet.AddRange( sbsVMs );
        }

        new private async Task Populate( ShipDetail sd )
        {
            await base.Populate( sd );

            (Make, Model, Role) = await GetShipViewModel( ShipDetail.ShipId );

            List<ShipBalanceSheetViewModel> sbsVMs = await GetShipBalanceSheetItemViewModels();

            BalanceSheet.Clear();
            BalanceSheet.AddRange( sbsVMs );
        }

        #endregion Query Handling
        #endregion Methods
    }
}
