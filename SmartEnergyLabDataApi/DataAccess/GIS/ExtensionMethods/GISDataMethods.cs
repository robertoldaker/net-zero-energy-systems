using SmartEnergyLabDataApi.Model;

namespace SmartEnergyLabDataApi.Data
{
    public static class GISDataMethods {
        public static IList<GISBoundary> GetBoundariesAndUpdate(this GISData gisData, DataAccess da, int numBoundaries) {
            return da.GIS.GetBoundariesAndUpdate(gisData.Id, numBoundaries);
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

        public static void UpdateBoundaryPoints(this GISData gisData, DataAccess da, double[][][][] elements, int maxIndex) {
            var numBoundaries = elements.Length;
            var boundaries = gisData.GetBoundariesAndUpdate(da, numBoundaries);
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

            // set longitude/lat as average of boundary with most points until we can get a better fix?
            if ( boundaries[maxIndex].Latitudes.Length!=0 ) {
                gisData.Latitude = boundaries[maxIndex].Latitudes.Sum()/boundaries[maxIndex].Latitudes.Length;
            }
            if ( boundaries[maxIndex].Longitudes.Length!=0 ) {
                gisData.Longitude = boundaries[maxIndex].Longitudes.Sum()/boundaries[maxIndex].Longitudes.Length;
            }
        }
    }
}