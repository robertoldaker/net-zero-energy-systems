using HaloSoft.DataAccess;
using HaloSoft.EventLogger;
using NHibernate;
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

        public IList<GridSubstation> GetGridSubstations() {
            return Session.QueryOver<GridSubstation>().Fetch(SelectMode.Fetch, m=>m.GISData).List();
        }

        public void Delete(GridOverheadLine ohl)
        {
            Session.Delete(ohl);
        }

        public GridOverheadLine GetGridOverheadline(string reference) {
            return Session.QueryOver<GridOverheadLine>().Where( m=>m.Reference == reference).Take(1).SingleOrDefault();
        }
    }
}