using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using NHibernate;
using NHibernate.Cfg;
using System.IO;
using System.Diagnostics;
using MySql.Data.MySqlClient;
using NHibernate.Tool.hbm2ddl;
using Npgsql;

namespace HaloSoft.DataAccess
{
    public class DataAccessBase : IDisposable
    {
        private static DbConnection _dbConnection;
        private ISession _session;
        private ITransaction _transaction;
        private static object _checkDbLock = new object();

        public DataAccessBase()
        {
            setSession();
            Configuration = new Configuration(this);
        }

        public Configuration Configuration { get; private set; }

        public static void Initialise(DbConnection connection, Action<int, int> schemaUpdated, Action<int, int> startupFunc)
        {
            _dbConnection = connection;
            //
            //
            if ( _dbConnection.DbProvider == DbProvider.PostgreSQL) {
                NpgsqlConnection.GlobalTypeMapper.UseJsonNet(new[] { typeof(Dictionary<string, string>) });
            }
            //
            var cfg = _dbConnection.GetConfiguration();

            // Check schema version
            if (DataAccessBase.CheckDatabase(out int oldVersion, out int newVersion)) {
                schemaUpdated(oldVersion, newVersion);
                using (var dab = new DataAccessBase()) {
                    dab.Configuration.GetDbVersion().SchemaVersion = newVersion;
                    dab.CommitChanges();
                }
            }

            // Check startup script
            using (var dab = new DataAccessBase()) {
                var dbVersion = dab.Configuration.GetDbVersion();
                var curScriptVersion = dbVersion.ScriptVersion;
                var newScriptVersion = DbConnection.ScriptVersion;
                if (curScriptVersion != newScriptVersion) {
                    dbVersion.ScriptVersion = newScriptVersion;
                    startupFunc(curScriptVersion, newScriptVersion);
                    dab.CommitChanges();
                }
            }
        }

        protected void setSession()
        {
            _session = _dbConnection.SessionFactory.OpenSession();
            try {
                _transaction  = _session.BeginTransaction();
            } catch( Exception e ) {
                Console.WriteLine(e.Message);
            }
        }

        //protected static NHibernate.Cfg.Configuration GetConfiguration(string dataBaseName)
        //{
            //var config = new NHibernate.Cfg.Configuration();
            //string connection = GetConnection(dataBaseName);
            //NHibernate.Mapping.Attributes.HbmSerializer.Default.Validate = true;
            //config.SetProperty("connection.provider","NHibernate.Connection.DriverConnectionProvider");
            //config.SetProperty("connection.driver_class", "NHibernate.Driver.SqlClientDriver");
            //config.SetProperty("connection.connection_string",connection);
            //config.SetProperty("dialect","NHibernate.Dialect.MsSql2005Dialect");
            //config.SetProperty("show_sql","true");
            //config.SetProperty("use_outer_join", "true");
            //config.SetProperty("query.substitutions", "true 1, false 0");
            //config.SetProperty("adonet.batch_size", "0");
            // Cache stuff
            //config.SetProperty("cache.provider_class", "NHibernate.Caches.EnyimMemcached.MemCacheProvider, NHibernate.Caches.EnyimMemcached");
            //config.SetProperty("cache.provider_class", "Alachisoft.NCacheExpress.Integrations.NHibernate.Cache.NCacheProvider,Alachisoft.NCacheExpress.Integrations.NHibernate.Cache");
            //config.SetProperty("cach.provider_class", "NHibernate.Caches.SysCache.SysCacheProvider, NHibernate.Caches.SysCache");
            //config.SetProperty("cache.use_second_level_cache", "true");
            //config.SetProperty("cache.use_query_cache", "true");
            //config.SetProperty("expiration", "86400");
            //config.SetProperty("regionPrefix", dataBaseName);
            //config.AddAssembly(typeof(DataAccessBase).Assembly);
            //MemoryStream s = NHibernate.Mapping.Attributes.HbmSerializer.Default.Serialize( Assembly.GetExecutingAssembly());
            //byte[] bytes = s.ToArray();
            //string str = Encoding.UTF8.GetString(bytes);
            //config.AddInputStream( s); 
            //return config;
           // if (dataBaseName == "SBT_System")
           // {
           //     return GetSystemConfiguration();
           // }
           // else
           // {
           //     return GetClientConfiguration(dataBaseName);
           // }            
        //}

        protected static string _rootFolder;

        public static void CreateDatabaseSchema(NHibernate.Cfg.Configuration config)
        {
            //
            NHibernate.Tool.hbm2ddl.SchemaExport schema = new NHibernate.Tool.hbm2ddl.SchemaExport(config);
            schema.Create(false, true);
        }

        public static bool GetUpdateSql(NHibernate.Cfg.Configuration config, out string sql)
        {
            NHibernate.Tool.hbm2ddl.SchemaUpdate schema = new NHibernate.Tool.hbm2ddl.SchemaUpdate(config);
            //
            //
            List<string> sqlCommands = new List<string>();
            var actions = new Action<string>(m => sqlCommands.Add(m));
            schema.Execute(actions, false);
            var exceptions = schema.Exceptions;
#if DEBUG
//            Debug.WriteLine("db update start" + actionsStr.Count);
//            foreach (var astr in actionsStr)
//            {
//                Debug.WriteLine(astr);
//            }
//            Debug.WriteLine("db update end" + actionsStr.Count);
#endif
            // Actualy throw exceptions here if we had any (appears the .Execute method records them but doesn't throw them)
            if (exceptions.Count > 0)
            {
                throw exceptions[0];
            }
            //
            //
            sql = "";
            foreach (var str in sqlCommands)
            {
                sql += str + ";\r\n";
            }
            return (sqlCommands.Count == 0);
        }

        public static bool UpdateDatabase(NHibernate.Cfg.Configuration config, out string sql)
        {
            NHibernate.Tool.hbm2ddl.SchemaUpdate schema = new NHibernate.Tool.hbm2ddl.SchemaUpdate(config);
            //
            List<string> actionsStr = new List<string>();
            var actions = new Action<string>(m => actionsStr.Add(m));
            schema.Execute(actions, true);
            var exceptions = schema.Exceptions;

            // Actualy throw exceptions here if we had any (appears the .Execute method records them but doesn't throw them)
            if (exceptions.Count > 0)
            {
                throw exceptions[0];
            }
            //
            sql = "";
            foreach( var str in actionsStr )
            {
                sql += str + ";\r\n";
            }
            return (actionsStr.Count == 0);
        }


        public static string GetMySqlDumpPath()
        {
            if (System.Environment.OSVersion.Platform == PlatformID.Unix)
            {
                return "mysqldump";
            }
            else
            {
                return Path.Combine(_rootFolder, @"ExternalPrograms\MySql", "mysqldump.exe");
            }
        }

        public static IList<T> ExtractFromEnumerable<T>(IEnumerable<T> query, ref int startIndex, ref int count, out int totalCount)
        {
            totalCount = query.Count();

            if (startIndex >= totalCount)
            {
                startIndex = 0;
            }

            if (startIndex + count > totalCount)
            {
                count = totalCount - startIndex;
            }

            List<T> sales = query.Skip(startIndex).Take(count).ToList();

            //
            return sales;
        }

        public virtual void CommitChanges()
        {
            try
            {
                _transaction.Commit();
            }
            catch
            {
                _transaction.Rollback();
                throw;
            }
            finally 
            {
                _session.Close();
            }
        }

        public ISession Session
        {
            get
            {
                return _session;
            }
        }

        public static DbConnection DbConnection
        {
            get
            {
                return _dbConnection;
            }
        }

        public virtual void ReOpen()
        {
            if (_transaction.IsActive)
            {
                _transaction.Rollback();
            }
            _transaction.Dispose();
            if (_session.IsOpen)
            {
                _session.Close();
            }
            _session = _dbConnection.SessionFactory.OpenSession();
            _transaction = _session.BeginTransaction();
        }

        public virtual void DatabaseSchemaUpdated(int? oldVersion, int? newVersion)
        {

        }

        private static bool CheckDatabase(out int curVersion, out int newVersion)
        {
            DbVersion dbVersion=null;
            curVersion = 0;
            newVersion = 0;
            try {
                using( var da = new DataAccessBase() ) {
                    dbVersion = da.Configuration.GetDbVersion();
                    curVersion = dbVersion.SchemaVersion;
                }
            } catch {

            }
            if (dbVersion == null || _dbConnection.SchemaVersion > dbVersion.SchemaVersion) {
                lock (_checkDbLock) {
                    updateDatabase();
                    newVersion = _dbConnection.SchemaVersion;
                    return true;
                }
            } else {
                return false;
            }
        }

        private static string updateDatabase()
        {
            string message = "";
            updateDatabaseSchema();
            //
            return message;
        }

        private static void updateDatabaseSchema()
        {
            _dbConnection.UpdateDatabaseSchema();
        }

        public static void RunSql(string sql)
        {
            if ( _dbConnection.DbProvider == DbProvider.MariaDb) {
                runMySql(sql);
            } else if ( _dbConnection.DbProvider == DbProvider.PostgreSQL) {
                runPostgreSQL(sql);
            } else {
                throw new NotImplementedException($"RunSql found unexpected dbProvider {_dbConnection.DbProvider}");
            }
        }
        private static void runMySql(string sql)
        {
            var connStr = _dbConnection.GetConnectionString();
            var con = new MySqlConnection(connStr);
            using (con) {
                con.Open();
                using (var cmd = new MySqlCommand(sql, con)) {
                    int resp = cmd.ExecuteNonQuery();
                }
                con.Close();
            }
        }

        private static void runPostgreSQL(string sql)
        {
            var connStr = _dbConnection.GetConnectionString();
            using (var con = new NpgsqlConnection(connStr)) {
                con.Open();

                using var cmd = new NpgsqlCommand(sql, con);

                var version = cmd.ExecuteNonQuery().ToString();
            }
        }
        
        public static void RunPostgreSQLQuery(string sql, Action<NpgsqlDataReader> rowRead)
        {
            var connStr = _dbConnection.GetConnectionString();
            using (var con = new NpgsqlConnection(connStr)) {
                con.Open();

                using var cmd = new NpgsqlCommand(sql, con);

                var reader = cmd.ExecuteReader();
                if ( reader.HasRows ) {
                    while( reader.Read()) {
                        rowRead.Invoke(reader);
                    }
                }
                
            }
        }

        public static string GetTableName(string name)
        {
            if (_dbConnection.DbProvider == DbProvider.PostgreSQL) {
                return name.ToLower();
            } else {
                return name;
            }
        }

        public void Dispose()
        {
            if ( _transaction!=null) {
                _transaction.Dispose();
            }
            Session.Dispose();
        }

        public static void PerformCleanup() {
            if ( _dbConnection.DbProvider == DbProvider.PostgreSQL) {
                runPostgreSQL("VACUUM FULL ANALYZE");
            }
        }

    }
}
