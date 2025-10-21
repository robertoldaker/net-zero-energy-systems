using System.Text;
using System.Text.Json;
using System.Web;
using HaloSoft.EventLogger;
using Microsoft.AspNetCore.Mvc.TagHelpers;
using Org.BouncyCastle.Asn1.Cms;
using SmartEnergyLabDataApi.Data;

namespace SmartEnergyLabDataApi.Models
{
    public class GoogleMapsGISFinder
    {
        private HttpClient _client;
        private HttpClient _newClient;
        private string _key;
        private UriBuilder _geocodeBuilder,_placeBuilder,_textSearchBuilder;
        private string _textSearchNewUrl;

        public GoogleMapsGISFinder()
        {
            _client = new HttpClient();
            _geocodeBuilder = new UriBuilder("https://maps.googleapis.com/maps/api/geocode/json");
            _placeBuilder = new UriBuilder("https://maps.googleapis.com/maps/api/place/findplacefromtext/json");
            _textSearchBuilder = new UriBuilder("https://maps.googleapis.com/maps/api/place/textsearch/json");
            _textSearchNewUrl = "https://places.googleapis.com/v1/places:searchText";
            _key = loadKey();
            //
            _newClient = new HttpClient();
            _newClient.DefaultRequestHeaders.Add("X-Goog-Api-Key", _key);
        }

        private string loadKey()
        {
            //
            // this file needs to appear in the home directory and also will not be in the source code repository
            // since the repository is now open source
            //
            string fileName = "smart-energy-lab (api key).txt";
            string file = Path.Combine(AppEnvironment.Instance.RootFolder, fileName);
            if (File.Exists(file)) {
                string text = File.ReadAllText(file).Trim();
                return text;
            } else {
                throw new Exception($"Cannot find google api key file [{fileName}]");
            }
        }

        public Geometry Lookup(string address)
        {
            var query = HttpUtility.ParseQueryString(string.Empty);
            query["address"] = address;
            query["key"] = _key;
            _geocodeBuilder.Query = query.ToString();
            var url = _geocodeBuilder.ToString();
            var response = _client.GetStringAsync(url).Result;

            var result = JsonSerializer.Deserialize<Geocode>(response);
            var results = result?.results;
            if ( results!=null && results.Count>0) {
                return results[0].geometry;
            } else {
                if ( result?.error_message!=null ) {
                    throw new Exception($"Problem using google geocoder api [{result.error_message}]");
                } else {
                    throw new Exception("Unspecified problem using google geocoder api");
                }
            }
        }
        public Geocode LookupAll(string address)
        {
            var query = HttpUtility.ParseQueryString(string.Empty);
            query["address"] = address;
            query["components"] = "country:GB";
            query["key"] = _key;
            _geocodeBuilder.Query = query.ToString();
            var url = _geocodeBuilder.ToString();
            var response = _client.GetStringAsync(url).Result;

            var result = JsonSerializer.Deserialize<Geocode>(response);
            if ( !string.IsNullOrEmpty(result.error_message) ) {
                throw new Exception($"Problem calling GoogleMapsGISFinder [{result.error_message}]");
            }
            return result;
        }

        public string PlaceLookupRaw(string text)
        {
            var query = HttpUtility.ParseQueryString(string.Empty);
            query["input"] = text;
            query["fields"]="name,geometry";
            query["inputtype"] = "textquery";
            query["key"] = _key;
            _placeBuilder.Query = query.ToString();
            var url = _placeBuilder.ToString();
            var response = _client.GetStringAsync(url).Result;

            return response;
        }

        public string TextSearchRaw(string text)
        {
            var query = HttpUtility.ParseQueryString(string.Empty);
            query["query"] = text;
            query["key"] = _key;
            _textSearchBuilder.Query = query.ToString();
            var url = _textSearchBuilder.ToString();
            var response = _client.GetStringAsync(url).Result;

            return response;
        }

        public string TextSearchNewRaw(string text)
        {
            var headers = new Dictionary<string,string>() {
                {"X-Goog-FieldMask","places.displayName,places.location,places.formattedAddress,places.viewport"}
                //??{"X-Goog-FieldMask","*"}
            };

            var body = new {
                textQuery = text
            };
            string response="";
            using(var req = getRequestMessage(HttpMethod.Post,_textSearchNewUrl, body, headers)) {
                var resp = _newClient.SendAsync(req).Result;
                if ( resp.IsSuccessStatusCode ) {
                    response = resp.Content.ReadAsStringAsync().Result;
                } else {
                    throw new Exception($"Invalid status code [{resp.StatusCode}] [{resp.ReasonPhrase}]");
                }
            }
            return response;
        }

        public TextSearch TextSearchNew(string text)
        {
            var response = TextSearchNewRaw(text);
            var result = JsonSerializer.Deserialize<TextSearch>(response);
            return result;
        }

        private HttpRequestMessage getRequestMessage(HttpMethod httpMethod, string url, object data = null, Dictionary<string,string> headers = null)
        {
            HttpRequestMessage message = new HttpRequestMessage(httpMethod, url);
            if (data != null) {
                string reqStr;
                if (data is string) {
                    reqStr = (string)data;
                } else {
                    reqStr = JsonSerializer.Serialize(data);
                }
                message.Content = new StringContent(reqStr, Encoding.UTF8, "application/json");
            }
            if ( headers!=null) {
                foreach( var key in headers.Keys) {
                    message.Headers.Add(key,headers[key]);
                }
            }
            return message;
        }

        public PlaceLookupContainer PlaceLookup(string text)
        {
            var response = PlaceLookupRaw(text);
            var result = JsonSerializer.Deserialize<PlaceLookupContainer>(response);
            if ( !string.IsNullOrEmpty(result.error_message) ) {
                throw new Exception($"Problem calling GoogleMapsGISFinder.PlaceLookup [{result.error_message}]");
            }
            return result;
        }

        public class Geocode
        {
            public List<GeocodeResult> results { get; set; }
            public string error_message {get; set;}
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

        public class PlaceLookupContainer {
            public List<PlaceLookupResult> candidates { get; set; }
            public string error_message {get; set;}
            public string status {get; set;}
        }

        public class PlaceLookupResult {
            public Geometry geometry {get; set;}
            public string name {get; set;}
        }

        public class TextSearch {
            public List<Place> places { get; set;}
        }

        public class Place {
            public LocationNew location {get; set;}
            public ViewportNew viewport {get; set;}
            public DisplayName displayName {get; set;}
            public string formattedAddress {get; set;}

        }

        public class DisplayName {
            public string text {get; set;}
            public string languageCode {get; set;}
        }

        public class LocationNew
        {

            public double latitude { get; set; }
            public double longitude { get; set; }
        }

        public class ViewportNew
        {
            public LocationNew low { get; set; }
            public LocationNew high { get; set; }

        }

    }


}
