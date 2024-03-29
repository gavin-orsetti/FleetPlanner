﻿using FleetPlanner.Helpers;
using FleetPlanner.MVVM.Models;
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
    public class ShoppingListShipDetailViewModel : ShipDetailViewModel_Populated
    {
        public ShoppingListShipDetailViewModel( ShipDetail ship, Action<int> deleteAction, Ship sModel ) : base( ship, deleteAction )
        {
            PopulateTextCommand.ExecuteAsync( ship.TaskGroupId );
            tertiary = Constants.Tertiary;
            warning = Constants.Warning;
            shipModel = sModel;

            if( ship.CashPurchase )
            {
                BuyUsdBtnColor = tertiary;
                UsdBtnStyleClass = "TertiaryElevatedButton";
                BuyAuecBtnColor = warning;
                AuecBtnStyleClass = "WarningElevatedButton";
            }
            else
            {
                BuyUsdBtnColor = warning;
                UsdBtnStyleClass = "WarningElevatedButton";
                BuyAuecBtnColor = tertiary;
                AuecBtnStyleClass = "TertiaryElevatedButton";
            }
        }

        private readonly Ship shipModel;
        private Color tertiary;
        private Color warning;

        private Color buyUsdBtnColor;
        public Color BuyUsdBtnColor
        {
            get => buyUsdBtnColor;
            set => SetProperty( ref buyUsdBtnColor, value );
        }
        private Color buyAuecBtnColor;
        public Color BuyAuecBtnColor
        {
            get => buyAuecBtnColor;
            set => SetProperty( ref buyAuecBtnColor, value );
        }

        private string usdBtnStyleClass;
        public string UsdBtnStyleClass
        {
            get => usdBtnStyleClass;
            set => SetProperty( ref usdBtnStyleClass, value );
        }

        private string auecBtnStyleClass;
        public string AuecBtnStyleClass
        {
            get => auecBtnStyleClass;
            set => SetProperty( ref auecBtnStyleClass, value );
        }

        private string taskGroupName;
        public string TaskGroupName
        {
            get => taskGroupName;
            set => SetProperty( ref taskGroupName, value );
        }

        private string shipPriceUsd;
        public string ShipPriceUsd
        {
            get => shipPriceUsd;
            set => SetProperty( ref shipPriceUsd, value );
        }

        private string shipPriceUec;
        public string ShipPriceUec
        {
            get => shipPriceUec;
            set => SetProperty( ref shipPriceUec, value );
        }

        private AsyncCommand<int> populateCommand;
        private AsyncCommand<int> PopulateTextCommand => populateCommand ??= new AsyncCommand<int>( PopulateText );

        private AsyncCommand purchaseUsingAuecCommand;
        public AsyncCommand PurchaseUsingAuecCommand => purchaseUsingAuecCommand ??= new AsyncCommand( PurchaseUsingAuec );

        private AsyncCommand purchaseUsingCashCommand;
        public AsyncCommand PurchaseUsingCashCommand => purchaseUsingCashCommand ??= new AsyncCommand( PurchaseUsingCash );

        private async Task PurchaseUsingAuec()
        {
            ShipDetailDatabaseService shipDetailDbs = await ServiceProvider.GetShipDetailDatabaseServiceAsync();

            Purchased = true;
            CurrencyAsEnum = Constants.Currency.UEC;
            CashPurchase = false;
            MeltValue = 0;
            InsuranceTypeAsEnum = Constants.InsuranceType.ThreeMonth;
            AnnualInsuranceCost = 0;

            _deleteAction.Invoke( Id );

            UpdateShipDetail();

            await shipDetailDbs.Update( ShipDetail );
        }

        private async Task PurchaseUsingCash()
        {
            ShipDetailDatabaseService shipDetailDbs = await ServiceProvider.GetShipDetailDatabaseServiceAsync();

            Purchased = true;
            CurrencyAsEnum = Constants.UserCurrency;
            CashPurchase = true;
            MeltValue = shipModel.LivePriceUSD;
            InsuranceTypeAsEnum = Constants.InsuranceType.ThreeMonth;
            AnnualInsuranceCost = 0;

            _deleteAction.Invoke( Id );

            UpdateShipDetail();

            await shipDetailDbs.Update( ShipDetail );
        }

        private void UpdateShipDetail()
        {
            ShipDetail.Purchased = Purchased;
            ShipDetail.Currency = (int)CurrencyAsEnum;
            ShipDetail.CashPurchase = CashPurchase;
            ShipDetail.MeltValue = MeltValue;
            ShipDetail.InsuranceType = (int)InsuranceTypeAsEnum;
            ShipDetail.AnnualInsuranceCost = AnnualInsuranceCost;
        }

        private async Task PopulateText( int id )
        {
            TaskGroupDatabaseService taskGroupDbs = await ServiceProvider.GetTaskGroupDatabaseServiceAsync();

            TaskGroup tg = await taskGroupDbs.GetRow( id );
            TaskGroupName = tg.Name;

            ShipPriceUsd = shipModel.LivePriceUSD.ToString( "N0" );
            ShipPriceUec = shipModel.LivePriceAuec.ToString( "N0" );

        }

    }
}
