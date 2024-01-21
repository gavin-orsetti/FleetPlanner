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
        public long ProfitHourly { get; set; }
        public int CrewCount_Max { get; set; }
        public int CrewCount_Min { get; set; }
        public int CrewCount_NPC { get; set; }
        public int ShipCount { get; set; }
        public string Notes { get; set; }



    }
}