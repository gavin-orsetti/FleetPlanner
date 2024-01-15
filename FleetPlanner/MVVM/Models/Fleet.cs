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

        public string Manifesto
        {
            get; set;
        }

        public string AreaOfOperation
        {
            get; set;
        }

        [OneToMany( CascadeOperations = CascadeOperation.CascadeRead )]
        public List<TaskGroup> TaskGroups
        {
            get; set;
        }
    }
}
