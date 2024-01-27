using FleetPlanner.Helpers;
using FleetPlanner.MVVM.Models;
using FleetPlanner.MVVM.ViewModels;
using FleetPlanner.Services;

using MvvmHelpers;
using MvvmHelpers.Commands;

using SQLite;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using System.Threading.Tasks;

using static FleetPlanner.Helpers.Constants;

using ServiceProvider = FleetPlanner.Services.ServiceProvider;

namespace FleetPlanner.MVVM.ViewModels
{
    public class ShipDetailViewModel : ViewModelBase
    {
        private ShipDetail shipDetail = new();
        public ShipDetail ShipDetail { get => shipDetail; protected set => SetProperty( ref shipDetail, value ); }

        protected ShipViewModel shipViewModel;

        private int id;
        public int Id
        {
            get => id;
            protected set => SetProperty( ref id, value );
        }

        private string callsign;
        public string Callsign
        {
            get => callsign ??= string.Empty;
            set => SetProperty( ref callsign, value );
        }

        private int fleetId;
        public int FleetId
        {
            get => fleetId;
            protected set => SetProperty( ref fleetId, value );
        }

        private int taskGroupId;
        public int TaskGroupId
        {
            get => taskGroupId;
            protected set => SetProperty( ref taskGroupId, value );
        }

        private int shipId;
        public int ShipId
        {
            get => shipId;
            protected set => SetProperty( ref shipId, value );
        }

        private string responsibility;
        public string Responsibility
        {
            get => responsibility ??= string.Empty;
            set => SetProperty( ref responsibility, value );
        }

        private int personalAttachmentRating;
        public int PersonalAttachmentRating
        {
            get => personalAttachmentRating;
            set => SetProperty( ref personalAttachmentRating, value );
        }

        private int integrality;
        public int Integrality
        {
            get => integrality;
            set => SetProperty( ref integrality, value );
        }

        private string notes;
        public string Notes
        {
            get => notes ??= string.Empty;
            set => SetProperty( ref notes, value );
        }

        private int npcCrewMax;
        public int NpcCrewMax
        {
            get => npcCrewMax;
            set => SetProperty( ref npcCrewMax, value );
        }

        private int npcCrewMin;
        public int NpcCrewMin
        {
            get => npcCrewMin;
            set => SetProperty( ref npcCrewMin, value );
        }

        private int playerCrewMin;
        public int PlayerCrewMin
        {
            get => playerCrewMin;
            set => SetProperty( ref playerCrewMin, value );
        }

        private int playerCrewMax;
        public int PlayerCrewMax
        {
            get => playerCrewMax;
            set => SetProperty( ref playerCrewMax, value );
        }

        private int crewTotalMin;
        public int CrewTotalMin
        {
            get => crewTotalMin;
            set => SetProperty( ref crewTotalMin, value );
        }

        private int crewTotalMax;
        public int CrewTotalMax
        {
            get => crewTotalMax;
            set => SetProperty( ref crewTotalMax, value );
        }

        private long hourlyIncome;
        public long HourlyIncome
        {
            get => hourlyIncome;
            set => SetProperty( ref hourlyIncome, value );
        }

        private long expectedProfit;
        public long ExpectedProfit
        {
            get => expectedProfit;
            set => SetProperty( ref expectedProfit, value );
        }

        private bool purchased;
        public bool Purchased
        {
            get => purchased;
            set => SetProperty( ref purchased, value );
        }

        private Currency currency;
        protected Currency CurrencyAsEnum => currency;
        public string Currency
        {
            get => currency.ToSplitString();
            set
            {
                string v = value.RemoveSpaces();
                if( Enum.TryParse( v, out Currency c ) )
                {
                    SetProperty( ref currency, c );
                }
            }
        }

        private bool cashPurchase;
        public bool CashPurchase
        {
            get => cashPurchase;
            set => SetProperty( ref cashPurchase, value );
        }

        private int meltValue;
        public int MeltValue
        {
            get => meltValue;
            set => SetProperty( ref meltValue, value );
        }

        private InsuranceType insuranceType;
        protected InsuranceType InsuranceTypeAsEnum => insuranceType;
        public string InsuranceType
        {
            get => insuranceType.ToSplitString();
            set
            {
                string v = value.RemoveSpaces();
                if( Enum.TryParse( v, out InsuranceType t ) )
                    SetProperty( ref insuranceType, t );
            }
        }

        private int annualInsuranceCost;


        public int AnnualInsuranceCost
        {
            get => annualInsuranceCost;
            set => SetProperty( ref annualInsuranceCost, value );
        }

        private ObservableRangeCollection<ShipBalanceSheetViewModel> balanceSheet;

        /// <summary>
        /// This is empty unless you specifically populate it by adding the List of balance sheet items returned from calling GetShipBalanceSheetItemViewModels
        /// </summary>
        public ObservableRangeCollection<ShipBalanceSheetViewModel> BalanceSheet => balanceSheet ??= new();

        private AsyncCommand<int> goToShipDetailCommand;
        public AsyncCommand<int> GoToShipDetailCommand => goToShipDetailCommand ??= new AsyncCommand<int>( GoToShipDetail );

        private AsyncCommand<int> goToEditShipDetailCommand;

        public AsyncCommand<int> GoToEditShipDetailCommand => goToEditShipDetailCommand ??= new AsyncCommand<int>( GoToEditShipDetail );


        #region Methods

        private async Task GoToShipDetail( int id )
        {
            Dictionary<string, object> queryParams = new()
            {
                { Routes.ShipDetailQueryParams.Id, id }
            };

            await Shell.Current.GoToAsync( Routes.ShipDetail_Page_PageName, queryParams );
        }


        private async Task GoToEditShipDetail( int id )
        {
            Dictionary<string, object> queryParams = new()
            {
                { Routes.ShipDetailQueryParams.Id, id }
            };

            await Shell.Current.GoToAsync( Routes.ShipDetail_EditPage_PageName, queryParams );
        }

        #region Query Handling

        private protected override async Task EvaluateQueryParams( KeyValuePair<string, object> kvp )
        {
            switch( kvp.Key )
            {
                case Routes.ShipDetailQueryParams.Id:
                    Console.WriteLine( "Id Passed to Ship Detail Page" );

                    await Populate( (int)kvp.Value );
                    break;
                default:
                    break;
            }
        }

        protected async Task Populate( int id )
        {
            ShipDetailDatabaseService shipDetailDbs = await ServiceProvider.GetShipDetailDatabaseServiceAsync();
            ShipDetail sd = await shipDetailDbs.GetRow( id );

            ShipDetail = sd;
            Id = ShipDetail.Id;
            FleetId = ShipDetail.FleetId;
            TaskGroupId = ShipDetail.TaskGroupId;
            ShipId = ShipDetail.ShipId;
            Callsign = ShipDetail.Callsign;
            Responsibility = ShipDetail.Responsibility;
            PersonalAttachmentRating = ShipDetail.PersonalAttachmentRating;
            Integrality = ShipDetail.Integrality;
            Notes = ShipDetail.Notes;
            NpcCrewMax = ShipDetail.NpcCrewMax;
            NpcCrewMin = ShipDetail.NpcCrewMin;
            PlayerCrewMax = ShipDetail.PlayerCrewMax;
            PlayerCrewMin = ShipDetail.PlayerCrewMin;
            CrewTotalMax = ShipDetail.CrewTotalMax;
            CrewTotalMin = ShipDetail.CrewTotalMin;
            HourlyIncome = ShipDetail.HourlyIncome;
            ExpectedProfit = ShipDetail.ExpectedProfit;
            Purchased = ShipDetail.Purchased;
            Currency = ( (Currency)ShipDetail.Currency ).ToString();
            CashPurchase = ShipDetail.CashPurchase;
            MeltValue = ShipDetail.MeltValue;
            InsuranceType = ( (InsuranceType)ShipDetail.InsuranceType ).ToString();
            AnnualInsuranceCost = ShipDetail.AnnualInsuranceCost;

        }

        protected async Task<List<ShipBalanceSheetViewModel>> GetShipBalanceSheetItemViewModels()
        {
            ShipBalanceSheetDatabaseService shipBalanceSheetDbs = await ServiceProvider.GetShipBalanceSheetDatabaseServiceAsync();
            List<ShipBalanceSheet> sheetItems = await shipBalanceSheetDbs.GetChildrenUsingPropertyName( ShipDetail.Id, nameof( ShipBalanceSheet.ShipDetailId ) );

            List<ShipBalanceSheetViewModel> bsVms = new();

            foreach( ShipBalanceSheet sheetItem in sheetItems )
            {
                ShipBalanceSheetViewModel bsvm = new ShipBalanceSheetViewModel( sheetItem );
                bsVms.Add( bsvm );
            }

            return bsVms;
        }

        #endregion Query Handling
        #endregion Methods
    }
}
