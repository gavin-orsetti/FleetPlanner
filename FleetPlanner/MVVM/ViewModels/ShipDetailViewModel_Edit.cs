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

        private AsyncCommand saveCommand;
        public AsyncCommand SaveCommand => saveCommand ??= new AsyncCommand( Save );

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
            Console.WriteLine( CurrencyAsEnum.ToString() );
            Console.WriteLine( (int)CurrencyAsEnum );
            ShipDetail.Currency = (int)CurrencyAsEnum;
            ShipDetail.CashPurchase = CashPurchase;
            ShipDetail.MeltValue = MeltValue;
            ShipDetail.InsuranceType = (int)InsuranceTypeAsEnum;
            ShipDetail.AnnualInsuranceCost = AnnualInsuranceCost;

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
                    Console.WriteLine( "--------------------------------------------- EDIT SHIP DETAIL PAGE ---------------------------------------------" );
                    await Populate( (int)kvp.Value );
                    break;
                case Routes.ShipDetailQueryParams.Object:
                    await Populate( (ShipDetail)kvp.Value );
                    Console.WriteLine( "--------------------------------------------- EDIT SHIP DETAIL PAGE ---------------------------------------------" );
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
