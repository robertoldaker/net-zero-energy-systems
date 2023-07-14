using HaloSoft.DataAccess;
using HaloSoft.EventLogger;
using NHibernate.Criterion;

namespace SmartEnergyLabDataApi.Data
{
    public class NationalGrid : DataSet
    {
        public NationalGrid(DataAccessBase da) : base(da) {

        }

        public void Add(GridOverheadLine ohl) 
        {
            Session.Save(ohl);
        }

        public void Delete(GridOverheadLine ohl)
        {
            Session.Delete(ohl);
        }


        public void Add(GridSubstation gss) 
        {
            Session.Save(gss);
        }

        public void Delete(GridSubstation gss)
        {                   
            Session.Delete(gss);
        }

        public GridSubstation GetGridSubstation(string reference) {
            return Session.QueryOver<GridSubstation>().Where( m=>m.Reference == reference).Take(1).SingleOrDefault();
        }

    }
}