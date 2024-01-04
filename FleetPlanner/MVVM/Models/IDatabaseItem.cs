using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FleetPlanner.MVVM.Models
{
    public interface IDatabaseItem
    {
        public int Id
        {
            get; set;
        }
    }
}
