using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using SQLite;

using SQLiteNetExtensions.Attributes;

using SQLitePCL;

namespace FleetPlanner.MVVM.Models
{
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

        public string Manifesto
        {
            get; set;
        }

        public string AreaOfOperation
        {
            get; set;
        }

        [OneToMany( CascadeOperations = CascadeOperation.CascadeRead )]
        public List<TaskGroup> TaskUnits
        {
            get; set;
        }
    }
}
