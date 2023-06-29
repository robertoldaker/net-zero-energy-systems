using HaloSoft.DataAccess;

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

        public IList<GISBoundary> GetBoundariesAndUpdate(int gisDataId, int numBoundaries=1) {
            var boundaries = Session.QueryOver<GISBoundary>().Where( m=>m.GISData.Id==gisDataId).List();
            if ( boundaries.Count<numBoundaries) {
                // Add some boundaries if we haven't enough
                var gisData = base.Get<GISData>(gisDataId);
                if ( gisData!=null ) {
                    for ( int i=0;i<numBoundaries - boundaries.Count; i++) {
                        var boundary = new GISBoundary(gisData);
                        Session.Save(boundary);
                        boundaries.Add(boundary);
                    }
                } 
            } else if ( boundaries.Count>numBoundaries) {
                // Remove too many boundaries
                var gisData = base.Get<GISData>(gisDataId);
                if ( gisData!=null ) {
                    for ( int i=numBoundaries;i<boundaries.Count; i++) {
                        Session.Delete(boundaries[i]);
                    }
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
        public int GetMaxBoundaryLength(int gisDataId) {
            var boundaries = Session.QueryOver<GISBoundary>().Where( m=>m.GISData.Id==gisDataId).List();
            if ( boundaries!=null ) {
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
    }
}
