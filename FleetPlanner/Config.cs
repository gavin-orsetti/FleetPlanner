using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

//using SQLite;

namespace FleetPlanner
{
    public static class Config
    {
        const string baseDir = "FleetPlanner";
        static readonly string MauiProjectDirectory = Path.Combine( baseDir, @"Database\ships.json" );
        private static readonly string shipDbFilePath = Path.GetFullPath( MauiProjectDirectory );
        private static readonly string shipDatabaseFilePath = shipDbFilePath;

        private static readonly string connectionString = $"Data Source={ShipDatabaseFilePath};";

        public static string ShipDatabaseFilePath
        {
            get => shipDatabaseFilePath;
        }

        public static string ConnectionString
        {
            get => connectionString;
        }

        //public const SQLiteOpenFlags ShipDbFlags = SQLiteOpenFlags.ReadOnly;
    }
}
