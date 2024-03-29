﻿using FleetPlanner.Helpers;
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
    public class DatabaseAccess
    {
        private SQLiteAsyncConnection db;

        private static DatabaseAccess instance;

        #region Start Up
        public static DatabaseAccess Instance()
        {
            return instance ??= new DatabaseAccess
            {
                db = new SQLiteAsyncConnection( Config.DatabasePath, Config.Flags )
            };
        }



        #endregion Start Up

        #region Operations

        #region Create

        public async Task<TableMapping> CreateTable<T>() where T : IStorable, new()
        {
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

        public async Task<T> GetLastRow<T>() where T : IStorable, new()
        {

            List<T> rows = await db.Table<T>().ToListAsync();
            T row = rows.OrderByDescending( x => x.Id ).FirstOrDefault();
            return row;
        }

        public async Task<List<T>> GetContainedObjectsList<T>( int parentId, string tableName, string columnName ) where T : IStorable, new()
        {
            string query = $"{Constants.SELECT_ALL} {Constants.FROM} {tableName} {Constants.WHERE} {columnName} {Constants.EQUALS} ?";
            List<T> items = await db.QueryAsync<T>( query, [ parentId ] );

            return items;
        }
        #endregion Read

        #region Update
        public async Task UpdateRow<T>( T item ) where T : IStorable, new()
        {
            await db.UpdateAsync( item );
        }

        public async Task UpdateMultiple<T>( List<T> items ) where T : IStorable, new()
        {
            await db.UpdateAllAsync( items );
        }

        #endregion

        #region Delete
        public async Task<bool> DeleteRowAsync<T>( int id )
        {
            int i = await db.DeleteAsync<T>( id );

            return i > 0;
        }

        public async Task DeleteRange<T>( List<int> ids ) where T : IStorable, new()
        {
            try
            {
                foreach( int id in ids )
                {
                    await db.DeleteAsync<T>( id );
                }
            }
            catch( Exception )
            {

                throw;
            }
        }
        #endregion Delete

        #endregion Operations
    }
}
