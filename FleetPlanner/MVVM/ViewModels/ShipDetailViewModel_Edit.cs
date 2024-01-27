using FleetPlanner.Services;

using MvvmHelpers.Commands;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using ServiceProvider = FleetPlanner.Services.ServiceProvider;

namespace FleetPlanner.MVVM.ViewModels
{
    public class ShipDetailViewModel_Edit : ShipDetailViewModel
    {

        private AsyncCommand saveCommand;
        public AsyncCommand SaveCommand => saveCommand ??= new AsyncCommand( Save );

        #region Methods

        private async Task Save()
        {
            if( Id > 0 )
            {
                await Update();
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

            await shipDetailDbs.Update( ShipDetail );
        }

        #region Query Handling
        private protected override async Task EvaluateQueryParams( KeyValuePair<string, object> kvp )
        {
            switch( kvp.Key )
            {
                case Routes.ShipDetailQueryParams.Id:
                    await Populate( (int)kvp.Value );
                    break;
                default:
                    break;
            }
        }

        new private async Task Populate( int id )
        {
            await base.Populate( id );

            List<ShipBalanceSheetViewModel> sbsVMs = await GetShipBalanceSheetItemViewModels();

            BalanceSheet.Clear();
            BalanceSheet.AddRange( sbsVMs );
        }

        #endregion Query Handling
        #endregion Methods
    }
}
