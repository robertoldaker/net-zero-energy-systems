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

        public GridSubstationLocation GetGridSubstationLocation(string reference, Dataset dataset=null) {
            return Session.QueryOver<GridSubstationLocation>().
                Where( m=>m.Reference == reference).
                And( m=>m.Dataset==dataset).
                Take(1).SingleOrDefault();
        }

        public IList<GridSubstationLocation> GetGridSubstationLocations() {
            return Session.QueryOver<GridSubstationLocation>().Fetch(SelectMode.Fetch, m=>m.GISData).List();
        }

        public IList<GridSubstationLocation> GetGridSubstationLocationsForLoadflow() {
            GridSubstationLocation loc=null;
            var sq = QueryOver.Of<Node>().Where(m => (m.Location.Id == loc.Id) ).Select(m => m.Id);
            var locations = Session.QueryOver<GridSubstationLocation>(()=>loc).WithSubquery.WhereExists(sq).List();
            return locations;
        }

        public IList<int> GetGridSubstationLocationsForLoadflowCtrls(int datasetId) {
            //
            var datasetIds = this.DataAccess.Datasets.GetInheritedDatasetIds(datasetId);
            //
            GridSubstationLocation loc=null;
            Branch b=null;
            Node node1=null;
            var sq = QueryOver.Of<Ctrl>().
                JoinAlias(m=>m.Branch,()=>b).
                JoinAlias(()=>b.Node1,()=>node1).
                Where( m=>m.Dataset.Id.IsIn(datasetIds)).
                Where(m=>node1.Location.Id==loc.Id).
                Select(m => m.Id);
            var locationIds = Session.QueryOver<GridSubstationLocation>(()=>loc).
                Where( m=>m.Dataset.Id.IsIn(datasetIds)).
                WithSubquery.WhereExists(sq).Select(m=>m.Id).
                List<int>();
            return locationIds;
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