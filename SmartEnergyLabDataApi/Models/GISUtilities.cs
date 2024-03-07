using System.Diagnostics;
using System.Text.Json;
using System.Web;
using CommonInterfaces.Models;
using HaloSoft.EventLogger;
using SmartEnergyLabDataApi.Data;

namespace SmartEnergyLabDataApi.Models
{
    public class GISUtilities
    {
        private static double toRadians(
               double angleIn10thofaDegree)
        {
            // Angle in 10th
            // of a degree
            return (angleIn10thofaDegree *
                           Math.PI) / 180;
        }
        public static double distance(double lat1, double lon1, double lat2, double lon2 )
        {

            // The math module contains
            // a function named toRadians
            // which converts from degrees
            // to radians.
            lon1 = toRadians(lon1);
            lon2 = toRadians(lon2);
            lat1 = toRadians(lat1);
            lat2 = toRadians(lat2);

            // Haversine formula
            double dlon = lon2 - lon1;
            double dlat = lat2 - lat1;
            double a = Math.Pow(Math.Sin(dlat / 2), 2) +
                       Math.Cos(lat1) * Math.Cos(lat2) *
                       Math.Pow(Math.Sin(dlon / 2), 2);

            double c = 2 * Math.Asin(Math.Sqrt(a));

            // Radius of earth in
            // kilometers. Use 3956
            // for miles
            double r = 6371;

            // calculate the result
            return (c * r);
        }

        public static bool IsPointInPolygon(double lat, double lng, double[] lats, double[] lngs) {

            int i, j;
            bool c = false;
            for (i = 0, j = lats.Length - 1; i < lats.Length; j = i++)
            {
                if ((((lats[i] <= lat) && (lat < lats[j])) |
                    ((lats[j] <= lat) && (lat < lats[i]))) &&
                    (lng < (lngs[j] - lngs[i]) * (lat - lats[i]) / (lats[j] - lats[i]) + lngs[i]))
                    c = !c;
            }
            return c;
        }

        public static void ConvertToGeoJson(string srcFile, string geoJsonFile) {

            var processStartInfo = new ProcessStartInfo();
            processStartInfo.CreateNoWindow = true;
            processStartInfo.UseShellExecute = false;
            processStartInfo.Arguments = $"-f GeoJSON \"{geoJsonFile}\" \"{srcFile}\"";
            // Can't get the service to pick up ogr2ogr so have to mention it explicitly
            if ( AppEnvironment.Instance.Context == Context.Production) {
                processStartInfo.FileName = "/home/roberto/anaconda3/bin/ogr2ogr";
            } else {
                processStartInfo.FileName = "ogr2ogr";
            }

            // enable raising events because Process does not raise events by default
            processStartInfo.UseShellExecute = false;
            var process = new Process();
            process.StartInfo = processStartInfo;

            process.Start();

            process.WaitForExit();
        }


    }

}