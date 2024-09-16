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

        public int GetNumGridSubstations(GridSubstationSource source) {
            return Session.QueryOver<GridSubstation>().
                And( m=>m.Source == source).
                RowCount();
        }

        public void DeleteSubstations(GridSubstationSource source) {
            var subs = Session.QueryOver<GridSubstation>().
                Where( m=>m.Source == source).
                List();
            foreach ( var sub in subs) {
                Session.Delete(sub);
            }
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

        public GridSubstationLocation GetGridSubstationLocation(string reference, Dataset dataset=null, bool includeDerived=false) {
            var q = Session.QueryOver<GridSubstationLocation>().Where( m=>m.Reference == reference); 
            if ( dataset!=null && includeDerived) {
                var datasetIds = DataAccess.Datasets.GetInheritedDatasetIds(dataset.Id);
                q = q.Where( m=>m.Dataset.Id.IsIn(datasetIds));
            } else {
                q = q.Where( m=>m.Dataset == dataset);
            }
            return q.Take(1).SingleOrDefault();
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

        public int GetNumGridSubstationLocations(GridSubstationLocationSource source) {
            return Session.QueryOver<GridSubstationLocation>().
                Where( m=>m.Dataset == null).
                And( m=>m.Source == source).
                RowCount();
        }

        public IList<GridSubstationLocation> GetGridSubstationLocationsBySource(GridSubstationLocationSource source) {
            //
            var q = Session.QueryOver<GridSubstationLocation>().Where( m=>m.Source == source);
            return q.List();
        }

        public bool GridSubstationLocationExists(int datasetId, string reference, out Dataset? dataset) {
            // need to look at all datasets belonging to the user
            var derivedIds = DataAccess.Datasets.GetDerivedDatasetIds(datasetId);
            var inheritedIds = DataAccess.Datasets.GetInheritedDatasetIds(datasetId);
            var loc = Session.QueryOver<GridSubstationLocation>().
                Where( m=>m.Reference.IsInsensitiveLike(reference)).
                Where( m=>m.Dataset.Id.IsIn(derivedIds) || m.Dataset.Id.IsIn(inheritedIds)).
                Fetch(SelectMode.Fetch,m=>m.Dataset).
                Take(1).
                SingleOrDefault();
            if ( loc!=null) {
                dataset = loc.Dataset;
            } else {
                dataset = null;
            }
            return loc!=null;
        }

        public void DeleteLocations(GridSubstationLocationSource source) {
            var locs = Session.QueryOver<GridSubstationLocation>().
                Where(m=>m.Dataset == null).
                Where( m=>m.Source == source).
                List();
            foreach ( var loc in locs) {
                Session.Delete(loc);
            }
        }

        public DatasetData<GridSubstationLocation> GetLocationDatasetData(int datasetId,System.Linq.Expressions.Expression<Func<GridSubstationLocation, bool>> expression=null) {
            var locQuery = Session.QueryOver<GridSubstationLocation>();
            if ( expression!=null) {
                locQuery = locQuery.Where(expression);
            }
            var locDi = new DatasetData<GridSubstationLocation>(DataAccess,datasetId,m=>m.Id.ToString(), locQuery);
            return locDi;        
        }
    }
}