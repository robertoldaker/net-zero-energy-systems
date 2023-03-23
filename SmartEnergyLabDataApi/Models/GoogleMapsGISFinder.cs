using System.Text.Json;
using System.Web;
using HaloSoft.EventLogger;
using SmartEnergyLabDataApi.Data;

namespace SmartEnergyLabDataApi.Models
{
    public class GoogleMapsGISFinder
    {
        private HttpClient _client;
        private string _key;
        private UriBuilder _builder;

        public GoogleMapsGISFinder()
        {
            _client = new HttpClient();
            _builder = new UriBuilder("https://maps.googleapis.com/maps/api/geocode/json");
            _key = "AIzaSyBlmuxJKaYg1JvwRuXJFqdFVxClUR2Phps";
        }

        public Geometry Lookup(string address)
        {
            var query = HttpUtility.ParseQueryString(string.Empty);
            query["address"] = address;
            query["key"] = _key;
            _builder.Query = query.ToString();
            var url = _builder.ToString();
            var response = _client.GetStringAsync(url).Result;

            var result = JsonSerializer.Deserialize<Geocode>(response);
            var results = result?.results;
            if ( results!=null && results.Count>0) {
                return results[0].geometry;
            } else {
                return null;
            }
        }

        public class Geocode
        {
            public List<GeocodeResult> results { get; set; }
        }

        public class GeocodeResult
        {
            public Geometry geometry { get; set; }
        }

        public class Geometry
        {
            public Location location { get; set; }
            public Viewport viewport { get; set; }
        }

        public class Location
        {

            public double lat { get; set; }
            public double lng { get; set; }
        }

        public class Viewport
        {
            public Location northeast { get; set; }
            public Location southwest { get; set; }

            public bool isIn(Location l)
            {
                var latOK = l.lat <= northeast.lat && l.lat >= southwest.lat;
                var lngOK = l.lng <= northeast.lng && l.lng >= southwest.lng;
                return latOK && lngOK;
            }
        }
    }

}