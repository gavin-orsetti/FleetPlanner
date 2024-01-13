using FleetPlanner.MVVM.Models;

using SQLite;

using SQLiteNetExtensions.Extensions;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FleetPlanner.Services
{
    public class DatabaseService
    {
        private SQLiteAsyncConnection db;

        private static DatabaseService instance;

        #region Start Up
        public static async Task<DatabaseService> Instance()
        {
            if( instance == null )
            {
                await Create();
            }

            return instance;
        }

        private static async Task<DatabaseService> Create()
        {
            if( instance == null )
                await Init();

            return instance;
        }

        private static async Task Init()
        {
            instance = new DatabaseService
            {
                db = new SQLiteAsyncConnection( Config.DatabasePath, Config.Flags )
            };
        }

        #endregion Start Up

        #region Operations

        #region Create

        public async Task<TableMapping> CreateTable<T>() where T : IStorable, new()
        {
            if( db.Table<T>() != null )
                await db.GetMappingAsync<T>();

            await db.CreateTableAsync<T>();

            return await db.GetMappingAsync<T>();
        }

        public async Task<bool> Insert<T>( T item ) where T : IStorable, new()
        {
            return await db.InsertAsync( item, typeof( T ) ) >= 1;
        }

        #endregion Create
        #region Read
        public async Task<List<T>> GetAll<T>() where T : IStorable, new()
        {
            List<T> results = await db.Table<T>().ToListAsync();
            return [ .. results.OrderBy( x => x.Id ) ];
        }

        public async Task<T> FindRow<T>( int id ) where T : IStorable, new()
        {
            return await db.GetAsync<T>( id );
        }
        #endregion Read

        #region Update
        #endregion

        #region Delete
        public async Task<bool> DeleteRowAsync<T>( int id )
        {
            int i = await db.DeleteAsync<T>( id );

            return i > 0;
        }
        #endregion Delete

        #endregion Operations
    }
}
