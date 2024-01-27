using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FleetPlanner.MVVM.Models
{
    public class Ship : IStorable
    {
        public int Id
        {
            get; set;
        }

        public string Model
        {
            get; set;
        }

        public int Make
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

        public int Crew_Min
        {
            get; set;
        }

        public int Crew_Max
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
