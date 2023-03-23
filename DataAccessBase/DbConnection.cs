using MySql.Data.MySqlClient;
using NHibernate;
using NHibernate.Driver;
using NHibernate.Engine;
using NHibernate.SqlTypes;
using NHibernate.Tool.hbm2ddl;
using NHibernate.Type;
using NHibernate.UserTypes;
using Npgsql;
using NpgsqlTypes;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace HaloSoft.DataAccess
{
    public enum DbProvider {  MariaDb, PostgreSQL }
    public class DbConnection
    {
        public string Username { get; set; }
        public string Password { get; set; }
        public string Server { get; set; }
        public string DatabaseName { get; set; }
        public int Port { get; set; }
        public string TableSourceName { get; set; }
        public DbProvider DbProvider { get; set; }

        private readonly int _schemaVersion;
        private readonly int _scriptVersion;

        private object _sessionLock = new object();
        private ISessionFactory _sessionFactory;
        private NHibernate.Cfg.Configuration _cfg;

        public DbConnection(int schemaVersion, int scriptVersion)
        {
            _schemaVersion = schemaVersion;
            _scriptVersion = scriptVersion;
        }

        public int SchemaVersion
        {
            get
            {
                return _schemaVersion;
            }
        }

        public int ScriptVersion
        {
            get
            {
                return _scriptVersion;
            }
        }

        public string GetConnectionString(string dbName=null)
        {
            var port = getPort();
            //??string connection = $"Server={Server};Port={port};Uid={Username};Pwd={Password};SslMode=none;";
            string connection = $"Server={Server};Port={port};Uid={Username};Pwd={Password};";
            if ( DbProvider == DbProvider.MariaDb) {
                connection += "SslMode=none;";
            }
            // use one from parameters if not null, otherwise use one from properties
            var databaseName = dbName==null ? DatabaseName: null;
            // Only add database if we have this set
            if (!string.IsNullOrWhiteSpace(databaseName)) {
                connection += string.Format("Database={0}", databaseName);
            }
            return connection;
        }

        private int getPort()
        {
            if ( Port!=0) {
                return Port;
            } else if ( DbProvider == DbProvider.MariaDb) {
                return 3306;
            } else if ( DbProvider == DbProvider.PostgreSQL) {
                return 5432;
            } else {
                throw new Exception($"Unrecognised provider [{DbProvider}]");
            }
        }

        public ISessionFactory SessionFactory
        {
            get
            {
                lock( _sessionLock) {
                    if (_sessionFactory == null) {
                        _cfg = getConfiguration();
                        _sessionFactory = _cfg.BuildSessionFactory();
                    }
                    return _sessionFactory;
                }
            }
        }

        private NHibernate.Cfg.Configuration GetBasicConfiguration(string connection)
        {
            NHibernate.Cfg.Configuration config = new NHibernate.Cfg.Configuration();
            config.SetProperty("connection.provider", "NHibernate.Connection.DriverConnectionProvider");

            config.SetProperty("connection.driver_class", getDriver());
            config.SetProperty("connection.connection_string", connection);
            config.SetProperty("dialect", $"NHibernate.Dialect.{getDialect()}");
#if DEBUG
            //config.SetProperty("show_sql", "true");
#endif
            config.SetProperty("use_outer_join", "true");
            config.SetProperty("query.substitutions", "true 1, false 0");
            config.SetProperty("adonet.batch_size", "10000");
            //
            //
            return config;
        }

        private string getDriver()
        {
            if ( DbProvider == DbProvider.MariaDb) {
                return "NHibernate.Driver.MySqlDataDriver";
            } else if ( DbProvider == DbProvider.PostgreSQL) {
                return "HaloSoft.DataAccess.NpgsqlDriverExtended,DataAccessBase";
                //return "NHibernate.Driver.NpgsqlDriver";
            }
            else {
                throw new Exception($"Unrecognised provider [{DbProvider}]");
            }
        }
        private string getDialect()
        {
            if (DbProvider == DbProvider.MariaDb) {
                return "MySQL5Dialect";
            }
            else if (DbProvider == DbProvider.PostgreSQL) {
                return "PostgreSQL83Dialect";
            }
            else {
                throw new Exception($"Unrecognised provider [{DbProvider}]");
            }
        }

        private NHibernate.Cfg.Configuration getConfiguration()
        {
            var cfg = GetBasicConfiguration(GetConnectionString());
            NHibernateUtilities.AddMappings(cfg, TableSourceName);
            return cfg;
        }

        public NHibernate.Cfg.Configuration GetConfiguration()
        {
            return getConfiguration();
        }


        public void CreateDatabase(string dbName)
        {
            string connStr = GetConnectionString();
            var con = new MySqlConnection(connStr);
            using (con) {
                con.Open();

                using (var cmd = new MySqlCommand("CREATE DATABASE " + dbName, con)) {
                    cmd.ExecuteNonQuery();
                }
            }
        }

        public void RunSql(string sql, string dbName=null)
        {
            string connStr = GetConnectionString(dbName);
            var con = new MySqlConnection(connStr);
            using (con) {
                con.Open();
                using (var cmd = new MySqlCommand(sql, con)) {
                    int resp = cmd.ExecuteNonQuery();
                }
                con.Close();
            }
        }

        public bool CheckDatabaseExists(string dbName)
        {
            string connStr = GetConnectionString();
            bool result = false;
            var con = new MySqlConnection(connStr);
            using (con) {
                con.Open();

                using (var cmd = new MySqlCommand("SHOW DATABASES LIKE '" + dbName + "'", con)) {
                    var reader = cmd.ExecuteReader();
                    while (reader.Read()) {
                        var name = reader[0] as string;
                        result = true;
                    }
                    reader.Close();
                }
            }
            //
            return result;
        }

        public void UpdateDatabaseSchema()
        {
            var update = new SchemaUpdate(_cfg);
            update.Execute(true, true);
        }

    }

    public class NpgsqlDriverExtended : NpgsqlDriver
    {
        protected override void InitializeParameter(DbParameter dbParam, string name, SqlType sqlType)
        {
            if (sqlType is NpgsqlExtendedSqlType && dbParam is NpgsqlParameter) {
                this.InitializeParameter(dbParam as NpgsqlParameter, name, sqlType as NpgsqlExtendedSqlType);
            }
            else {
                base.InitializeParameter(dbParam, name, sqlType);
            }
        }

        protected virtual void InitializeParameter(NpgsqlParameter dbParam, string name, NpgsqlExtendedSqlType sqlType)
        {
            if (sqlType == null) {
                throw new QueryException(String.Format("No type assigned to parameter '{0}'", name));
            }

            dbParam.ParameterName = FormatNameForParameter(name);
            dbParam.DbType = sqlType.DbType;
            dbParam.NpgsqlDbType = sqlType.NpgDbType;

        }
    }

    [Serializable]
    public class NpgsqlExtendedSqlType : SqlType
    {

        public NpgsqlExtendedSqlType(DbType dbType, NpgsqlDbType npgDbType) : base(dbType)
        {
            this.npgDbType = npgDbType;
        }

        public NpgsqlExtendedSqlType(DbType dbType, NpgsqlDbType npgDbType, int length) : base(dbType, length)
        {
            this.npgDbType = npgDbType;
        }

        public NpgsqlExtendedSqlType(DbType dbType, NpgsqlDbType npgDbType, byte precision, byte scale) : base(dbType, precision, scale)
        {
            this.npgDbType = npgDbType;
        }

        private readonly NpgsqlDbType npgDbType;
        public NpgsqlDbType NpgDbType
        {
            get
            {
                return this.npgDbType;
            }
        }
    }

    public class DoubleArrayType : IUserType
    {
        public SqlType[] SqlTypes
        {
            get { return new SqlType[] { new NpgsqlExtendedSqlType(DbType.Double, NpgsqlTypes.NpgsqlDbType.Array | NpgsqlTypes.NpgsqlDbType.Double) }; }
        }

        public Type ReturnedType => typeof(double[]);

        public bool IsMutable => false;

        public object Assemble(object cached, object owner)
        {
            return null;
        }

        public object DeepCopy(object value)
        {
            return value;
        }

        public object Disassemble(object value)
        {
            return null;
        }

        public new bool Equals(object x, object y)
        {
            return false;
        }

        public int GetHashCode(object x)
        {
            return 0;
        }

        public object NullSafeGet(DbDataReader resultSet, string[] names, ISessionImplementor session, object owner)
        {
            int index = resultSet.GetOrdinal(names[0]);
            if (resultSet.IsDBNull(index)) {
                return null;
            }
            double[] res = resultSet.GetValue(index) as double[];
            if (res != null) {
                return res;
            }
            throw new NotImplementedException();
        }

        public void NullSafeSet(DbCommand cmd, object value, int index, ISessionImplementor session)
        {
            IDbDataParameter parameter = ((IDbDataParameter)cmd.Parameters[index]);
            if (value == null) {
                parameter.Value = DBNull.Value;
            }
            else {
                parameter.Value = (double[]) value;
            }
        }

        public object Replace(object original, object target, object owner)
        {
            return null;
        }
    }

    [Serializable]
    public class JsonType<TSerializable> : IUserType where TSerializable : class
    {
        private readonly Type _serializableClass;

        public JsonType()
        {
            _serializableClass = typeof(TSerializable);
        }

        private static string Serialize(object obj)
        {
            try {
                return JsonSerializer.Serialize(obj);
            }
            catch (Exception e) {
                throw new SerializationException("Could not serialize a serializable property: ", e);
            }
        }

        private object Deserialize(string dbValue)
        {
            try {
                return JsonSerializer.Deserialize(dbValue, _serializableClass);
            }
            catch (Exception e) {
                throw new SerializationException("Could not deserialize a serializable property: ", e);
            }
        }

        public new bool Equals(object x, object y)
        {
            if (ReferenceEquals(x, y)) {
                return true;
            }

            if (x == null | y == null) {
                return false;
            }

            if (IsDictionary(x) && IsDictionary(y)) {
                return EqualDictionary(x, y);
            }

            return x.Equals(y);
        }

        private static bool EqualDictionary(object x, object y)
        {
            var a = x as IDictionary;
            var b = y as IDictionary;

            if (a.Count != b.Count) return false;

            foreach (var key in a.Keys) {
                if (!b.Contains(key)) return false;

                var va = a[key];
                var vb = b[key];

                if (!va.Equals(vb)) return false;
            }

            return true;
        }

        private static bool IsDictionary(object o)
        {
            return typeof(IDictionary).IsAssignableFrom(o.GetType());
        }

        public int GetHashCode(object x)
        {
            return x.GetHashCode();
        }

        public object NullSafeGet(DbDataReader rs, string[] names, ISessionImplementor session, object owner)
        {
            if (names.Length != 1)
                throw new InvalidOperationException($"One column name expected but received {names.Length}");

            if (rs[names[0]] is string value && !string.IsNullOrWhiteSpace(value))
                return Deserialize(value);

            return null;
        }

        public void NullSafeSet(DbCommand cmd, object value, int index, ISessionImplementor session)
        {
            var parameter = cmd.Parameters[index];

            if (parameter is NpgsqlParameter)
                parameter.DbType = SqlTypes[0].DbType;

            if (value == null)
                parameter.Value = DBNull.Value;
            else
                parameter.Value = value;
        }

        public object DeepCopy(object value)
        {
            return Deserialize(Serialize(value));
        }

        public object Replace(object original, object target, object owner)
        {
            return original;
        }

        public object Assemble(object cached, object owner)
        {
            return (cached == null) ? null : Deserialize((string)cached);
        }

        public object Disassemble(object value)
        {
            return (value == null) ? null : Serialize(value);
        }

        public SqlType[] SqlTypes => new[] { new NpgsqlExtendedSqlType(DbType.Object, NpgsqlDbType.Jsonb) };
        public Type ReturnedType => _serializableClass;
        public bool IsMutable => true;
    }

    public class DictionaryStringStringType : JsonType<Dictionary<string,string>>
    {

    }
}
