using FleetPlanner.MVVM.Models;
using FleetPlanner.Services;

using MvvmHelpers;
using MvvmHelpers.Commands;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using static FleetPlanner.Helpers.Constants;

namespace FleetPlanner.MVVM.ViewModels
{
    public class NewTaskGroupViewModel : TaskGroupViewModel
    {
        private string searchString;
        public string SearchString
        {
            get => searchString ??= string.Empty;
            set
            {
                if( value != string.Empty || value != null )
                {
                    DisplayedShips.Clear();
                    DisplayedShips.AddRange( Ships.Where( x => x.Name.ToLower().Contains( value.ToLower() ) ) );
                }

                if( value == string.Empty || value == null )
                {
                    DisplayedShips.Clear();
                    DisplayedShips.AddRange( Ships );
                }

                SetProperty( ref searchString, value );
            }
        }

        private List<SelectableShipViewModel> selectedShips => Ships.Where( x => x.Selected ).ToList();

        private ObservableRangeCollection<SelectableShipViewModel> ships;
        public ObservableRangeCollection<SelectableShipViewModel> Ships => ships ??= [];

        private ObservableRangeCollection<SelectableShipViewModel> displayedShips;
        public ObservableRangeCollection<SelectableShipViewModel> DisplayedShips => displayedShips ??= [];

        private AsyncCommand saveCommand;
        public AsyncCommand SaveCommand => saveCommand ??= new AsyncCommand( Save );

        #region Methods

        private async Task Save()
        {
            TaskGroupDatabaseService taskGroupDbs = await Services.ServiceProvider.GetTaskGroupDatabaseServiceAsync();
            ShipDetailDatabaseService shipDetailDbs = await Services.ServiceProvider.GetShipDetailDatabaseServiceAsync();
            Task_Group = CreateTaskGroup();
            await taskGroupDbs.Insert( Task_Group );
            Task_Group = await taskGroupDbs.GetLastInsert();

            List<ShipDetail> shipDetails = CreateShipDetails();
            await shipDetailDbs.InsertMultiple( shipDetails );

            await Shell.Current.GoToAsync( Routes.BackOne );
        }

        private List<ShipDetail> CreateShipDetails()
        {
            List<ShipDetail> shipDetails = [];

            foreach( SelectableShipViewModel ssvm in selectedShips )
            {
                for( int i = ssvm.Quantity; i > 0; i-- )
                {
                    shipDetails.Add( new ShipDetail
                    {
                        ShipId = ssvm.Id,
                        FleetId = FleetId,
                        TaskGroupId = Task_Group.Id,
                        Callsign = ssvm.Name, // TODO: Make this generate a random name (like 'Iron Donkey') based off a list of words
                        Assignment = string.Empty,
                        PersonalAttachmentRating = 0,
                        Integrality = 0,
                        Notes = string.Empty,
                        NpcCrewMax = 0,
                        NpcCrewMin = 0,
                        PlayerCrewMax = ssvm.Crew_max,
                        PlayerCrewMin = ssvm.Crew_min,
                        CrewTotalMax = ssvm.Crew_max,
                        CrewTotalMin = ssvm.Crew_min,
                        HourlyIncome = 0,
                        ExpectedProfit = 0,
                        Purchased = false,
                        Currency = (int)Currency.UEC,
                        CashPurchase = false,
                        MeltValue = 0,
                        InsuranceType = (int)InsuranceType.Three_Month,
                        AnnualInsuranceCost = 0
                    } );
                }
            }

            return shipDetails;
        }

        private TaskGroup CreateTaskGroup()
        {
            return new TaskGroup()
            {
                Name = Name,
                FleetId = FleetId,
                Objective = Objective,
                AreaOfOperation = AreaOfOperation,
                Integrality = Integrality,
                ProfitHourly = 0,
                CrewCount_Max = CalculateMaxCrewCount(),
                CrewCount_Min = CalculateMinCrewCount(),
                CrewCount_NPC = 0,
                ShipCount = selectedShips.Count(),
                Notes = string.Empty
            };
        }

        private int CalculateMaxCrewCount()
        {
            int count = 0;
            foreach( SelectableShipViewModel ship in selectedShips )
            {
                count += ship.Crew_max;
            }
            return count;
        }
        private int CalculateMinCrewCount()
        {
            int count = 0;
            foreach( SelectableShipViewModel ship in selectedShips )
            {
                count += ship.Crew_min;
            }
            return count;
        }

        private async Task LoadShips()
        {
            ShipDatabaseService shipDbService = await Services.ServiceProvider.GetShipDatabaseServiceAsync();
            List<Ship> shipModels = await shipDbService.GetAll();
            List<SelectableShipViewModel> ssvms = [];

            foreach( Ship s in shipModels )
            {
                SelectableShipViewModel ssvm = new( s );
                ssvms.Add( ssvm );
            }

            Ships.Clear();
            Ships.AddRange( ssvms );

            DisplayedShips.Clear();
            DisplayedShips.AddRange( ssvms );

        }

        #region Query Handling
        private protected override async Task EvaluateQueryParams( KeyValuePair<string, object> kvp )
        {
            switch( kvp.Key )
            {
                case Routes.TaskGroupQueryParams.FleetId:
                    await LoadShips();
                    FleetId = Convert.ToInt32( kvp.Value );
                    break;
                case Routes.TaskGroupQueryParams.Fleet:
                    Console.WriteLine( "Fleet Passed" );
                    break;
                default:
                    break;
            }

            await base.EvaluateQueryParams( kvp );
        }
        #endregion Query Handling
        #endregion Methods

    }
}
