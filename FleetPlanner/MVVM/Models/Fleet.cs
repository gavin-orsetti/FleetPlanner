using SQLite;

using SQLiteNetExtensions.Attributes;

using SQLitePCL;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FleetPlanner.MVVM.Models
{
    [Table( nameof( Fleet ) )]
    public class Fleet : IStorable
    {
        [PrimaryKey, AutoIncrement]
        public int Id
        {
            get; set;
        }

        [NotNull]
        public string Name
        {
            get; set;
        }

        public string Affiliation
        {
            get; set;
        }

        public string AreaOfOperation
        {
            get; set;
        }

        public string Manifesto
        {
            get; set;
        }

        public int CrewTotalMax
        {
            get;
            set;
        }

        public int CrewTotalMin
        {
            get;
            set;
        }

        public int NpcCrewMin
        {
            get;
            set;
        }

        public int NpcCrewMax
        {
            get;
            set;
        }

        public long ExpectedProfit
        {
            get;
            set;
        }

        public int TaskGroupCount
        {
            get;
            set;
        }

        public int ShipCount
        {
            get;
            set;
        }

        public int ShipsOwned
        {
            get;
            set;
        }

        public int ShipsActive
        {
            get;
            set;
        }

        public int ShipsInactive
        {
            get;
            set;
        }

        public string Notes
        {
            get;
            set;
        }


    }
}
