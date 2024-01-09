using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FleetPlanner.Services
{
    public interface IDatabaseService<S, T> : IService<S>
    {
        /// <summary>
        /// Looks up the specified row in the database table and returns the result or throws an error.
        /// </summary>
        /// <param name="id">The row id of the desired record</param>
        /// <returns>The requested row</returns>
        public Task<T> GetRow( int id );

        /// <summary>
        /// Compiles a list of all the rows with matching ids and returns them as type T.
        /// </summary>
        /// <param name="range">A series of row ids</param>
        /// <returns>A list of records of type T</returns>
        public Task<List<T>> GetRange( List<int> range );

        /// <summary>
        /// Returns all the records contained in the table T
        /// </summary>
        /// <returns></returns>
        public Task<List<T>> GetAll();
    }
}
