using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FleetPlanner.MVVM.Models
{
    public class Ship : IDatabaseItem
    {
        public int Id
        {
            get; set;
        }

        public string Name
        {
            get; set;
        }

        public int Manufacturer
        {
            get; set;
        }

        public string Description
        {
            get; set;
        }

        public string Role
        {
            get; set;
        }

        public int Size
        {
            get; set;
        }

        public int Crew_min
        {
            get; set;
        }

        public int Crew_max
        {
            get; set;
        }

        public int Scu
        {
            get; set;
        }

        public int Stowage
        {
            get; set;
        }

        public int LivePriceUSD
        {
            get; set;
        }

        public int LivePriceAuec
        {
            get; set;
        }


    }
}
