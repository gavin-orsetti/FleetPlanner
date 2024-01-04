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

        public int role
        {
            get; set;
        }

        public int size
        {
            get; set;
        }

        public int crew_min
        {
            get; set;
        }

        public int crew_max
        {
            get; set;
        }

        public int scu
        {
            get; set;
        }

        public int stowage
        {
            get; set;
        }

        public int livePriceUSD
        {
            get; set;
        }

        public int livePriceAuec
        {
            get; set;
        }


    }
}
