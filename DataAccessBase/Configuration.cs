using NHibernate.Criterion;
using System;
using System.Collections.Generic;
using System.Text;

namespace HaloSoft.DataAccess
{
    public class Configuration : DataSet
    {
        public Configuration(DataAccessBase dataAccess) : base(dataAccess)
        {

        }

        #region DbVersion
        public DbVersion GetDbVersion()
        {
            var dbVersion = Session.QueryOver<DbVersion>().Take(1).SingleOrDefault();
            if (dbVersion == null) {
                dbVersion = new DbVersion();
                Session.Save(dbVersion);
            }
            return dbVersion;
        }
        #endregion
    }
}
