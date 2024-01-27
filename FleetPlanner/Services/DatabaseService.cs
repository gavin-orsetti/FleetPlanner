using FleetPlanner.MVVM.Models;

using SQLite;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FleetPlanner.Services
{
    public abstract class DatabaseService<S, T> : IDatabaseService<S, T> where T : IStorable, new()
                                                                         where S : class
    {

        protected static S service;
        protected TableMapping mapping;
        protected DatabaseAccess instance;

        protected abstract Task Init();


        public static Task<S> Create()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Gets all the records in the table.
        /// </summary>
        /// <returns>A list of all records</returns>
        public async Task<List<T>> GetAll()
        {
            return await instance.GetAll<T>();
        }

        /// <summary>
        /// Gets all the objects of type T with the associated Id
        /// </summary>
        /// <param name="range"></param>
        /// <returns> List of T objects where the ids match those passed in.</returns>
        public async Task<List<T>> GetRange( List<int> range )
        {
            List<T> items = await GetAll();

            return items.Where( x => range.Contains( x.Id ) ) as List<T>;
        }

        public async Task<T> GetLastInsert()
        {
            return await instance.GetLastRow<T>();
        }

        public async Task<List<T>> GetChildrenUsingPropertyName( int parentId, string propertyName )
        {
            string tableName = mapping.TableName;
            TableMapping.Column cn = mapping.FindColumnWithPropertyName( propertyName );

            return await instance.GetContainedObjectsList<T>( parentId, tableName, cn.Name );
        }

        /// <summary>
        /// This uses the "FindAsync" method of the SQLite-net project which means it can return null. Make sure to do a null check.
        /// </summary>
        /// <param name="id"></param>
        /// <returns>The item of type T with the associated id or null</returns>
        public async Task<T> GetRow( int id )
        {
            return await instance.FindRow<T>( id );
        }

        /// <summary>
        /// Attempts to insert an item into the database table that matches type T
        /// </summary>
        /// <param name="item">The item to insert</param>
        /// <returns>true on success, false on failure</returns>
        public async Task<bool> Insert( T item )
        {
            try
            {
                return await instance.Insert( item );
            }
            catch( Exception )
            {

                throw;
            }
        }

        /// <summary>
        /// Inserts all the items in the list to the database.
        /// </summary>
        /// <param name="items"></param>
        /// <returns></returns>
        public async Task<bool> InsertMultiple( List<T> items )
        {
            bool allSuccess = false;

            foreach( T item in items )
            {
                allSuccess = await Insert( item );
            }

            return allSuccess;
        }

        public async Task Update( T item )
        {
            await instance.UpdateRow( item );
        }

        public async Task UpdateMultiple( List<T> items )
        {
            await instance.UpdateMultiple( items );
        }

        /// <summary>
        /// Deletes the row with the specified id from the table mathing type T
        /// </summary>
        /// <typeparam name="T">The table type to delete a row from</typeparam>
        /// <param name="id">Id of the row to delete</param>
        /// <returns>true on success, false on failure</returns>
        public async Task<bool> DeleteAsync<T>( int id )
        {
            return await instance.DeleteRowAsync<T>( id );
        }
    }
}
