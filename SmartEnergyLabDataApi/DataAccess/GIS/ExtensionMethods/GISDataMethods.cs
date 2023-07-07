using HaloSoft.EventLogger;
using SmartEnergyLabDataApi.Model;
using static SmartEnergyLabDataApi.Models.UKPowerNetworkLoader;

namespace SmartEnergyLabDataApi.Data
{
    public static class GISDataMethods {
        public static IList<GISBoundary> GetBoundariesAndUpdate(this GISData gisData, DataAccess da, int numBoundaries, List<GISBoundary> boundariesToAdd) {
            return da.GIS.GetBoundariesAndUpdate(gisData.Id, numBoundaries, boundariesToAdd);
        }
        public static GISBoundary GetFirstBoundary(this GISData gisData, DataAccess da) {
            return da.GIS.GetBoundaries(gisData.Id)[0];
        }
        public static IList<GISBoundary> GetBoundaries(this GISData gisData, DataAccess da) {
            return da.GIS.GetBoundaries(gisData.Id);
        }
        public static int GetMaxBoundaryLength(this GISData gisData, DataAccess da) {
            return da.GIS.GetMaxBoundaryLength(gisData.Id);
        }

        public static void UpdateBoundaryPoints(this GISData gisData, double[][][][] elements, 
                IList<GISBoundary> boundaries, 
                IList<GISBoundary> boundariesToAdd,
                IList<GISBoundary> boundariesToDelete) {
            var numBoundaries = elements.Length;
            if ( numBoundaries>1) {
                //??Logger.Instance.LogInfoEvent($"Num boundaries > 1 [{numBoundaries}]");
            }
            gisData.adjustBoundaryLists(numBoundaries,boundaries,boundariesToAdd,boundariesToDelete);

            for(int i=0; i<numBoundaries;i++) {
                int length = elements[i][0].Length;
                boundaries[i].Latitudes = new double[length];
                boundaries[i].Longitudes = new double[length];
                for(int index=0; index<length; index++) {
                    var eastings = elements[i][0][index][0];
                    var northings = elements[i][0][index][1];
                    var latLong=LatLonConversions.ConvertOSToLatLon(eastings,northings);
                    boundaries[i].Latitudes[index] = latLong.Latitude;
                    boundaries[i].Longitudes[index] = latLong.Longitude;
                }
            }

            var maxIndex = getMaxIndex(elements);

            // set longitude/lat as average of boundary with most points until we can get a better fix?
            if ( boundaries[maxIndex].Latitudes.Length!=0 ) {
                gisData.Latitude = boundaries[maxIndex].Latitudes.Sum()/boundaries[maxIndex].Latitudes.Length;
            }
            if ( boundaries[maxIndex].Longitudes.Length!=0 ) {
                gisData.Longitude = boundaries[maxIndex].Longitudes.Sum()/boundaries[maxIndex].Longitudes.Length;
            }
        }

        public static void UpdateBoundaryPoints(this GISData gisData, GeoShape geoShape, 
                IList<GISBoundary> boundaries, 
                IList<GISBoundary> boundariesToAdd,
                IList<GISBoundary> boundariesToDelete) {
            if ( geoShape.type=="MultiPolygon" ) {
                int numBoundaries = geoShape.multiPolygonCoords.Length;
                gisData.adjustBoundaryLists(numBoundaries,boundaries,boundariesToAdd,boundariesToDelete);
                for( int i=0;i<numBoundaries;i++) {
                    int length = geoShape.multiPolygonCoords[i][0].Length;
                    boundaries[i].Latitudes = new double[length];
                    boundaries[i].Longitudes = new double[length];
                    int index=0;
                    foreach( var coord in geoShape.multiPolygonCoords[i][0] ) {
                        boundaries[i].Longitudes[index] = coord[0];
                        boundaries[i].Latitudes[index] = coord[1];
                        index++;
                    }
                }
            } else if ( geoShape.type=="Polygon") {
                int length = geoShape.polygonCoords[0].Length;
                gisData.adjustBoundaryLists(1,boundaries,boundariesToAdd,boundariesToDelete);
                var boundary = boundaries[0];
                boundary.Latitudes = new double[length];
                boundary.Longitudes = new double[length];
                int index=0;
                foreach( var coord in geoShape.polygonCoords[0] ) {
                    boundary.Longitudes[index] = coord[0];
                    boundary.Latitudes[index] = coord[1];
                    index++;
                }
            } else {
                throw new Exception($"Unexpected geometry type=[{geoShape.type}]");
            }
        }

        private static int getMaxIndex(double[][][][] elements) {
            int maxIndex=0;
            int maxLength = 0;
            for(int i=0;i<elements.Length;i++) {
                if ( elements[i][0].Length>maxLength) {
                    maxIndex=i;
                    maxLength = elements[i][0].Length;
                }
            }                
            return maxIndex;
        }


        private static void adjustBoundaryLists(this GISData gisData,int numBoundaries, 
            IList<GISBoundary> boundaries, 
            IList<GISBoundary> boundariesToAdd,
            IList<GISBoundary> boundariesToDelete) {
            if ( boundaries.Count<numBoundaries) {
                // Add some boundaries if we haven't enough
                //??Logger.Instance.LogInfoEvent($"Adding boundaries [{numBoundaries}/{boundaries.Count}] gisDataId=[{gisData.Id}]");
                int toAdd = numBoundaries - boundaries.Count;
                for ( int i=0;i<toAdd; i++) {
                    var boundary = new GISBoundary(gisData);
                    //Session.Save(boundary);
                    boundariesToAdd.Add(boundary);
                    boundaries.Add(boundary);
                }
            } else if ( boundaries.Count>numBoundaries) {
                // Remove too many boundaries
                //??Logger.Instance.LogInfoEvent($"Removing boundaries [{numBoundaries}/{boundaries.Count}]  gisDataId=[{gisData.Id}]");
                for ( int i=numBoundaries;i<boundaries.Count; i++) {
                    boundariesToDelete.Add(boundaries[i]);
                }
            }

        }
    }
}