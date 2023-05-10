using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NHibernate;

namespace HaloSoft.DataAccess
{
    public class DataSet
    {
        protected DataAccessBase _dataAccess;
        public DataSet(DataAccessBase dataAccess)
        {
            _dataAccess = dataAccess;
        }

        protected ISession Session
        {
            get
            {
                return _dataAccess.Session;
            }
        }

        protected static IList<T> GetQueryAsList<T>(IQueryOver<T> query, ref int skip, ref int take, out int totalCount)
        {
            totalCount = query.RowCount();

            if (skip >= totalCount)
            {
                skip = 0;
            }

            if (skip + take > totalCount)
            {
                take = totalCount - skip;
            }

            IList<T> rows = query.Skip(skip).Take(take).List();

            //
            return rows;
        }

        public T Get<T>(int id) {
            return _dataAccess.Session.Get<T>(id);
        }
    }
}
