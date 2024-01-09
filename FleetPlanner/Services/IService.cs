using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FleetPlanner.Services
{
    public interface IService<S>
    {
        /// <summary>
        /// Initializes an instance of the service and returns it to the caller.
        /// </summary>
        /// <returns>A fully initialized instance of the service</returns>
        public static abstract Task<S> Create();
    }
}
