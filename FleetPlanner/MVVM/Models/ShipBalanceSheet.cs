using SQLite;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FleetPlanner.MVVM.Models
{
    public class ShipBalanceSheet : IStorable
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }
        [Indexed]
        public int ShipDetailId { get; set; }
        public string Key { get; set; }
        public uint Value { get; set; }
        public bool IsPositive { get; set; }

    }
}
