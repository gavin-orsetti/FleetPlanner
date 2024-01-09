using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using FleetPlanner.MVVM.Models;

namespace FleetPlanner.MVVM.ViewModels
{
    public class FleetViewModel( Fleet fleet )
    {
        private readonly Fleet fleet;

        public Fleet Fleet => fleet;
    }
}
