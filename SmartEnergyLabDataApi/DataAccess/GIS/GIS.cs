using HaloSoft.DataAccess;
using HaloSoft.EventLogger;
using NHibernate;
using NHibernate.Criterion;

namespace SmartEnergyLabDataApi.Data
{
    public class GIS : DataSet
    {
        public GIS(DataAccess da) : base(da)
        {

        }

        public DataAccess DataAccess
        {
            get
            {
                return (DataAccess) _dataAccess;
            }
        }

        public void Add(GISData obj) {
            Session.Save(obj);
        }

        public IList<GISBoundary> GetBoundariesAndUpdate(int gisDataId, int numBoundaries, List<GISBoundary> boundariesToAdd) {
            var boundaries = Session.QueryOver<GISBoundary>().Where( m=>m.GISData.Id==gisDataId).List();
            if ( boundaries.Count<numBoundaries) {
                // Add some boundaries if we haven't enough
                var gisData = base.Get<GISData>(gisDataId);
                if ( gisData!=null ) {
                    //??Logger.Instance.LogInfoEvent($"Adding boundaries [{numBoundaries - boundaries.Count}]");
                    int toAdd = numBoundaries - boundaries.Count;
                    for ( int i=0;i<toAdd; i++) {
                        var boundary = new GISBoundary(gisData);
                        //Session.Save(boundary);
                        boundariesToAdd.Add(boundary);
                        boundaries.Add(boundary);
                    }
                } else {
                    throw new Exception($"Could not find GISData object for id [{gisDataId}]");
                } 
            } else if ( boundaries.Count>numBoundaries) {
                // Remove too many boundaries
                var gisData = base.Get<GISData>(gisDataId);
                if ( gisData!=null ) {
                    //??Logger.Instance.LogInfoEvent($"Removing boundaries [{boundaries.Count-numBoundaries}]");
                    for ( int i=numBoundaries;i<boundaries.Count; i++) {
                        Session.Delete(boundaries[i]);
                    }
                } else {
                    throw new Exception($"Could not find GISData object for id [{gisDataId}]");
                } 
            }
            return boundaries;
        }
        public IList<GISBoundary> GetBoundaries(int gisDataId) {
            var boundaries = Session.QueryOver<GISBoundary>().Where( m=>m.GISData.Id==gisDataId).List();
            if ( boundaries.Count==0) {
                var gisData = base.Get<GISData>(gisDataId);
                if ( gisData!=null ) {
                    var boundary = new GISBoundary(gisData);
                    Session.Save(boundary);
                    boundaries.Add(boundary);
                } 
            }
            return boundaries;
        }
        public IList<GISBoundary> GetBoundaries(int[] gisDataIds) {
            var boundaries = Session.QueryOver<GISBoundary>().Where( m=>m.GISData.IsIn(gisDataIds)).List();
            return boundaries;
        }

        public Dictionary<int,IList<GISBoundary>> GetBoundaryDict(int[] gisDataIds) {
            var boundaries = Session.QueryOver<GISBoundary>().Where( m=>m.GISData.Id.IsIn(gisDataIds)).List();
            var dict = new Dictionary<int,IList<GISBoundary>>();
            foreach( var b in boundaries) {
                if ( !dict.ContainsKey(b.GISData.Id) ) {
                    var list = new List<GISBoundary>();
                    list.Add(b);
                    dict.Add(b.GISData.Id,list);
                } else {
                    dict[b.GISData.Id].Add(b);
                }
            }
            //
            return dict;
        }

        public int GetMaxBoundaryLength(int gisDataId) {
            var boundaries = Session.QueryOver<GISBoundary>().Where( m=>m.GISData.Id==gisDataId).List();
            if ( boundaries!=null && boundaries.Count>0 ) {
                return boundaries.Max(m=>m.Latitudes.Length);
            } else {
                return 0;
            }
        }

        public IList<GISData> GetGISData(int skip, int take) {
            return Session.QueryOver<GISData>().OrderBy(m=>m.Id).Asc.Skip(skip).Take(take).List();
        }
        
        public int GetGISDataCount() {
            return Session.QueryOver<GISData>().RowCount();
        }

        public void Add(GISBoundary boundary) {
            Session.Save(boundary);
        }
        public void Delete(GISBoundary boundary) {
            Session.Delete(boundary);
        }
        
        public void Add(GISLine line) {
            Session.Save(line);
        }
        public void Delete(GISLine line) {
            Session.Delete(line);
        }
        public Dictionary<int,IList<GISLine>> GetLineDict(int[] gisDataIds) {
            var lines = Session.QueryOver<GISLine>().Where( m=>m.GISData.Id.IsIn(gisDataIds)).List();
            var dict = new Dictionary<int,IList<GISLine>>();
            foreach( var b in lines) {
                if ( !dict.ContainsKey(b.GISData.Id) ) {
                    var list = new List<GISLine>();
                    list.Add(b);
                    dict.Add(b.GISData.Id,list);
                } else {
                    dict[b.GISData.Id].Add(b);
                }
            }
            //
            return dict;
        }

        public Tuple<int,int> GetNumMultiBoundariesDist() {
            //
            var list = Session.QueryOver<DistributionSubstation>().Fetch(SelectMode.Fetch, m=>m.GISData).List();
            var mbs = list.Where( m=>m.GISData.NumBoundaries>1).Count();
            return new Tuple<int, int>(mbs,list.Count);
        }

        public Tuple<int,int>  GetNumMultiBoundariesPrimary() {
            //
            var list = Session.QueryOver<PrimarySubstation>().Fetch(SelectMode.Fetch, m=>m.GISData).List();
            var mbs = list.Where( m=>m.GISData.NumBoundaries>1).Count();
            return new Tuple<int,int>(mbs,list.Count);
        }

        public Tuple<int,int> GetNumMultiBoundariesGSP() {
            //
            var list = Session.QueryOver<GridSupplyPoint>().Fetch(SelectMode.Fetch, m=>m.GISData).List();
            var mbs = list.Where( m=>m.GISData.NumBoundaries>1).Count();
            return new Tuple<int,int>(mbs,list.Count);
        }

        public IList<GISBoundary> GetPrimaryBoundaries(ImportSource source) {
            GISData gisData=null;

            //??var gisIds = Session.QueryOver<GISData>().Left.JoinAlias(m=>m.PrimarySubstation,()=>pss).
            //??        Where(m=>m.PrimarySubstation!=null && pss.Source == source).Select(m=>m.Id).List<int>().ToArray();
            var gisIds = Session.QueryOver<PrimarySubstation>().Left.JoinAlias(m=>m.GISData,()=>gisData).
                    Where(m=>m.Source == source && m.GISData!=null).Select(m=>m.GISData.Id).List<int>().ToArray();

            var list = Session.QueryOver<GISBoundary>().Where( m=>m.GISData.Id.IsIn(gisIds)).Fetch(SelectMode.Fetch,m=>m.GISData).List();
            return list;
        }

        public PrimarySubstation GetPrimarySubstation(int gisId) {
            return Session.QueryOver<PrimarySubstation>().Where( m=>m.GISData.Id == gisId).Take(1).SingleOrDefault();
        }


    }
}
