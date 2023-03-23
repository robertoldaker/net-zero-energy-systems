using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.SqlClient;
using System.Threading;
using System.Security.Principal;
using System.Configuration;
using MySql.Data.MySqlClient;

namespace HaloSoft.DataAccess
{
    public class Connections
    {
        //
        public const string USERNAME = "hmrc_mtd_test_server";
        public const string PASSWORD = "hmrc_mtd_test_server";
        private static string _dataSource=null;
        private static object _dataSourceLock = new object();

        //
        public static string GetClientConnection(int clientId)
        {
            return GetConnectionString( GetClientDbName(clientId) );
        }

        //
        public static string GetClientDbName(int clientId)
        {
            return "sbt_data_" + clientId.ToString();
        }

        //
        public static string GetConnection()
        {
            return GetConnectionString(GetDbName());
        }
        //
        public static string GetDbName()
        {
            return "hmrc_mtd_test_server";
        }
        //
        public static string GetSystemRefDbName(int versionId)
        {
            return "hmrc_mtd_test_server_ref_" + versionId.ToString();
        }

        //
        public static string GetConnectionString( string dataBase )
        {
            string dataSource = GetDataSource();
            string connection = string.Format("Server={0};Port=3306;Uid={1};Pwd={2};SslMode=none;", dataSource, USERNAME, PASSWORD);
            if (!string.IsNullOrWhiteSpace(dataBase))
            {
                connection += string.Format("Database={0}", dataBase);
            }
            return connection;
        }

        //
        public static void SetDataSource(string dataSource)
        {
            _dataSource = dataSource;
        }

        /// <summary>
        /// Obtains the data source
        /// </summary>
        /// <returns></returns>
        public static string GetDataSource()
        {
            lock (_dataSourceLock)
            {
                if ( _dataSource==null )
                {
                    throw new Exception("No data source defined. Please use Conifugration.SetDataSource() to set the location of the database server");
                }
                return _dataSource;
            }
        }

        public static bool TestConnection()
        {
            string connectionString = GetConnection();
            MySqlConnection dbConnection = new MySqlConnection(connectionString);
            //
            try
            {
                dbConnection.Open();
            }
            catch (Exception)
            {
                return false;
            }
            //
            dbConnection.Close();
            return true;
        }

        public static void WaitForConnection(int timeout)
        {
            int trys = 0;
            while (trys < timeout)
            {
                try
                {
                    MySqlConnection dbConnection = new MySqlConnection(GetConnection());
                    dbConnection.Open();
                    dbConnection.Close();
                    return;
                }
                catch (Exception)
                {
                    trys++;
                    Thread.Sleep(1000);
                }
            }
            throw new Exception("Timeout waiting for db service to start");
            //
        }

    }
}

