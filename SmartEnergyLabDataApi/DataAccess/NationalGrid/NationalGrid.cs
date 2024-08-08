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

        public DataAccess DataAccess
        {
            get
            {
                return (DataAccess) _dataAccess;
            }
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

        public void Add(GridSubstationLocation loc) 
        {
            Session.Save(loc);
        }

        public void Delete(GridSubstationLocation loc)
        {                   
            Session.Delete(loc);
        }

        public GridSubstationLocation GetGridSubstationLocation(int id) {
            return Session.Get<GridSubstationLocation>(id);
        }

        public GridSubstationLocation GetGridSubstationLocation(string reference, Dataset dataset=null) {
            return Session.QueryOver<GridSubstationLocation>().
                Where( m=>m.Reference == reference).
                And( m=>m.Dataset==dataset).
                Take(1).SingleOrDefault();
        }

        public IList<GridSubstationLocation> GetGridSubstationLocations() {
            return Session.QueryOver<GridSubstationLocation>().
                Where( m=>m.Dataset==null).
                Fetch(SelectMode.Fetch, m=>m.GISData).
                List();
        }

        public IList<GridSubstationLocation> GetGridSubstationLocations(Dataset dataset) {
            return Session.QueryOver<GridSubstationLocation>().
                Where( m=>m.Dataset.Id == dataset.Id).
                Fetch(SelectMode.Fetch, m=>m.GISData).
                List();
        }

        public IList<GridSubstationLocation> GetGridSubstationLocationsBySource(GridSubstationLocationSource source) {
            //
            var q = Session.QueryOver<GridSubstationLocation>().Where( m=>m.Source == source);
            return q.List();
        }

        public void DeleteLocations(GridSubstationLocationSource source) {
            var locs = GetGridSubstationLocationsBySource(source);
            foreach( var loc in locs) {
                Delete(loc);
            }
        }
    }
}