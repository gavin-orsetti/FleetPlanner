using FleetPlanner.MVVM.Models;

using SQLite;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FleetPlanner.Services
{
    public interface IDatabaseService<S, T> : IService<S> where T : IStorable, new()
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

        /// <summary>
        /// Gets all the objects from the database table matching Type <typeparamref name="T"/> that have the parentID in the passed in column
        /// </summary>
        /// <param name="parentId"></param>
        /// <param name="columnName"></param>
        /// <returns></returns>
        public Task<List<T>> GetChildrenUsingPropertyName( int parentId, string columnName );

        /// <summary>
        /// Inserts a row into the database table matching the item type
        /// </summary>
        /// <param name="item"> The item to insert</param>
        /// <returns>true if item was inserted successfully</returns>
        Task<bool> Insert( T item );

        /// <summary>
        /// Deletes the item with the passed in Id from the table matching type T
        /// </summary>
        /// <typeparam name="T">The Type of the item to delete</typeparam>
        /// <param name="id">the id of the item to delete</param>
        /// <returns>true on successs false on failure</returns>
        Task<bool> DeleteAsync<T>( int id );
    }
}
