using SQLite;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FleetPlanner.MVVM.Models
{
    public class ShipDetail : IStorable
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }
        public int FleetId { get; set; }
        public int TaskGroupId { get; set; }
        public int ShipId { get; set; }
        public string Callsign { get; set; }
        public string Assignment { get; set; }
        public int PersonalAttachmentRating { get; set; }
        public int Integrality { get; set; }
        public string Notes { get; set; }
        public int NpcCrewMax { get; set; }
        public int NpcCrewMin { get; set; }
        public int PlayerCrewMin { get; set; }
        public int PlayerCrewMax { get; set; }
        public int CrewTotalMin { get; set; }
        public int CrewTotalMax { get; set; }
        public long HourlyIncome { get; set; }
        public long ExpectedProfit { get; set; }
        public bool Purchased { get; set; }
        public int Currency { get; set; }
        public bool CashPurchase { get; set; }
        public int MeltValue { get; set; }
        public int InsuranceType { get; set; }
        public int AnnualInsuranceCost { get; set; }
    }
}
