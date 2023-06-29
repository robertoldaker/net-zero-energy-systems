using SmartEnergyLabDataApi.Models;

namespace SmartEnergyLabDataApi.Data
{
    public static class GISBoundaryMethods {
        public static bool IsPtInside( this GISBoundary boundary, double lat, double lng) {
            var pointIn = GISUtilities.IsPointInPolygon(lat,lng,boundary.Latitudes,boundary.Longitudes);
            return pointIn;
        }
    }
}
 