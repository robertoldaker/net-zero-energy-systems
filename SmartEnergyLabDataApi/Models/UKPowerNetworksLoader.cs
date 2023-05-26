using System.Text;
using System.Text.Json;
using HaloSoft.EventLogger;
using SmartEnergyLabDataApi.Data;

namespace SmartEnergyLabDataApi.Models
{
    public class UKPowerNetworkLoader {

        private HttpClient _httpClient;
        private object _httpClientLock = new object();
        private string _baseUrl = "https://ukpowernetworks.opendatasoft.com";
        
        public void Load() {
            loadGSPs();
        }

        private void loadGSPs() {

            var gspRecords = loadData<GSPRecord>("ukpn-grid-supply-points");
            foreach( var gspRecord in gspRecords) {
                if ( gspRecord.geo_shape.type=="Polygon") {
                    gspRecord.geo_shape.polygonCoords = gspRecord.geo_shape.coordinates.Deserialize<double[][][]>();                    
                } else if ( gspRecord.geo_shape.type == "MultiPolygon") {
                    gspRecord.geo_shape.multiPolygonCoords = gspRecord.geo_shape.coordinates.Deserialize<double[][][][]>();
                }
            }

            // Add GSP
            using( var da = new DataAccess()) {
                var toAdd = new List<GridSupplyPoint>();
                // Need a way of working out the GA since no info in api data
                var ga = da.Organisations.GetGeographicalArea(DNOAreas.London);
                foreach( var gspRecord in gspRecords) {
                    var gsp = da.SupplyPoints.GetGridSupplyPointByName(gspRecord.gsp);
                    if ( gsp==null) {
                        gsp = new GridSupplyPoint(gspRecord.gsp,null,null,ga,ga.DistributionNetworkOperator);
                        Logger.Instance.LogInfoEvent($"Added new GSP=[{gspRecord.gsp}]");
                        toAdd.Add(gsp);
                    }
                    // Position of GSP
                    gsp.GISData.Latitude = gspRecord.geo_point_2d[0];
                    gsp.GISData.Longitude = gspRecord.geo_point_2d[1];
                    // Boundary
                    if ( gspRecord.geo_shape.type=="MultiPolygon" ) {
                        int length = gspRecord.geo_shape.multiPolygonCoords[0][0].Length;
                        gsp.GISData.BoundaryLatitudes = new double[length];
                        gsp.GISData.BoundaryLongitudes = new double[length];
                        int index=0;
                        foreach( var coord in gspRecord.geo_shape.multiPolygonCoords[0][0] ) {
                            gsp.GISData.BoundaryLongitudes[index] = coord[0];
                            gsp.GISData.BoundaryLatitudes[index] = coord[1];
                            index++;
                        }
                    } else if ( gspRecord.geo_shape.type=="Polygon") {
                        int length = gspRecord.geo_shape.polygonCoords[0].Length;
                        gsp.GISData.BoundaryLatitudes = new double[length];
                        gsp.GISData.BoundaryLongitudes = new double[length];
                        int index=0;
                        foreach( var coord in gspRecord.geo_shape.polygonCoords[0] ) {
                            gsp.GISData.BoundaryLongitudes[index] = coord[0];
                            gsp.GISData.BoundaryLatitudes[index] = coord[1];
                            index++;
                        }
                    } else {
                        Logger.Instance.LogInfoEvent($"Unexpected geometry type=[{gspRecord.geo_shape.type}] for entry [{gspRecord.gsp}]");
                    }
                }

                foreach( var gsp in toAdd) {
                    da.SupplyPoints.Add(gsp);
                }

                //
                da.CommitChanges();
            }
        }

        private List<T> loadData<T>(string dataset) where T : class {
            int rows = 10;
            int start=0;
            Container<T> container;
            List<T> records=new List<T>();
            string methodStr;
            do {
                methodStr = getMethodStr(dataset,rows,start);
                container = get<Container<T>>(methodStr);
                if ( container.records.Length>0) {
                    foreach( var record in container.records) {
                        records.Add(record.fields);
                    }
                }
                start += container.records.Length;
            } while( container.records.Length>0 );
            //
            return records;
        }

        private string getMethodStr(string dataset,int rows, int start) {
            return $"/api/records/1.0/search/?dataset=ukpn-grid-supply-points&facet=gsp&rows={rows}&start={start}";
        }

        private class GSPRecord {
            public string gsp {get; set;}
            public double[] geo_point_2d {get; set;}
            public GeoShape geo_shape {get; set;}

        }

        private class GeoShape {
            public string type {get; set;}
            public JsonElement coordinates{ get; set; }

            public double[][][][] multiPolygonCoords {get; set;}
            public double[][][] polygonCoords {get; set;}


        }

        private class Fields<T> {
            public T fields {get; set;}
        }

        private class Container<T> {
            public Fields<T>[] records {get; set;}
        }

        private T get<T>(string method, params string[] queryParams) where T : class
        {
            T data = null;
            //
            HttpResponseMessage response;
            //
            method = appendQueryString(method, queryParams);
            //
            var client = getHttpClient();
            //
            using (HttpRequestMessage message = getRequestMessage(HttpMethod.Get, method)) {
                //
                response = client.SendAsync(message).Result;
                //
            }
            //
            if (response.IsSuccessStatusCode) {
                var str = response.Content.ReadAsStringAsync().Result;
                data = JsonSerializer.Deserialize<T>(str);

            } else {
                var str = response.Content.ReadAsStringAsync().Result;
                var message = $"Problem calling method [{method}] [{response.StatusCode}] [{response.ReasonPhrase}] [{str}]";
                Logger.Instance.LogErrorEvent(message);
                throw new Exception(message);
            }
            return data;
        }

        private HttpClient getHttpClient()
        {
            if (_httpClient == null) {
                lock (_httpClientLock) {
                    _httpClient = new HttpClient();
                    _httpClient.BaseAddress = new Uri(_baseUrl);
                }
            }
            //
            return _httpClient;
        }

        private HttpRequestMessage getRequestMessage(HttpMethod httpMethod, string method, object data = null)
        {
            HttpRequestMessage message = new HttpRequestMessage(httpMethod, method);
            if (data != null) {
                string reqStr;
                if (data is string) {
                    reqStr = (string)data;
                } else {
                    reqStr = JsonSerializer.Serialize(data);
                }
                message.Content = new StringContent(reqStr, Encoding.UTF8, "application/json");
            }
            return message;
        }


        private static string appendQueryString(string method, params string[] nameValuePairs)
        {
            string s = method;
            //
            if (nameValuePairs.Length > 0) {
                s += "?" + getNameValuePairs(nameValuePairs);
            }
            return s;
        }

        private static string appendQueryString(string method, Dictionary<string, string> dict)
        {
            string s = method;
            //
            bool isFirst = true;
            foreach (var d in dict) {
                if (isFirst) {
                    s += "?";
                    isFirst = false;
                } else {
                    s += "&";
                }
                s += Uri.EscapeDataString(d.Key) + "=" + Uri.EscapeDataString(d.Value);
            }
            return s;
        }

        private static string getNameValuePairs(params string[] nameValuePairs)
        {
            //
            if ((nameValuePairs.Length % 2) != 0) {
                throw new Exception("Wrong number of parameters");
            }
            string[] strs = new string[nameValuePairs.Length / 2];
            int count = 0;
            for (int index = 0; index < nameValuePairs.Length - 1; index += 2) {
                //
                string name = nameValuePairs[index];
                if (name == null) { name = ""; }
                string value = nameValuePairs[index + 1];
                if (value == null) { value = ""; }
                strs[count++] = Uri.EscapeDataString(name) + "=" + Uri.EscapeDataString(value);
            }
            //
            return string.Join("&", strs);
        }

    }
}