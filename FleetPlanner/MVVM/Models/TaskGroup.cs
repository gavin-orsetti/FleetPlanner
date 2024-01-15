using SQLite;

using SQLiteNetExtensions.Attributes;

namespace FleetPlanner.MVVM.Models
{
    public class TaskGroup : IStorable
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }

        public int FleetId { get; set; }

        public string Name { get; set; }
        public string Objective { get; set; }

        public string AreaOfOperation { get; set; }

        public int Integrality { get; set; }
        public int ProfitHourly { get; set; }
        public int CrewCount_Full { get; set; }
        public int CrewCount_Min { get; set; }
        public int CrewCount_NPC { get; set; }
        public int ShipCount { get; set; }

        //public List<ShipDetail> Ships { get; set; }

    }
}