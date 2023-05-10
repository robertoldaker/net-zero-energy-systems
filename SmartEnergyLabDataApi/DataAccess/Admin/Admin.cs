using System.Reflection;
using HaloSoft.DataAccess;
using NHibernate;
using NHibernate.Criterion;
using SmartEnergyLabDataApi.Elsi;

namespace SmartEnergyLabDataApi.Data
{
    public class Admin : DataSet
    {
        public Admin(DataAccess da) : base(da)
        {
            
        }

        public void Add(DataSourceInfo obj) {
            Session.Save(obj);
        }

        public void Delete(DataSourceInfo obj) {
            Session.Delete(obj);
        }

        public DataSourceInfo GetDataSourceInfo(string reference) {
            return Session.QueryOver<DataSourceInfo>().Where( m=>m.Reference == reference).Take(1).SingleOrDefault();
        }

        public IList<DataSourceInfo> GetDataSourceInfos() {
            return Session.QueryOver<DataSourceInfo>().List();
        }
    }
}