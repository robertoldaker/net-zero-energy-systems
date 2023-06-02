using System.Text;
using System.Text.Json;
using HaloSoft.EventLogger;
using SmartEnergyLabDataApi.Data;
using System.Text.Json.Serialization;

namespace SmartEnergyLabDataApi.Models
{
    public class UKPowerNetworkLoader {

        private HttpClient _httpClient;
        private object _httpClientLock = new object();
        private string _baseUrl = "https://ukpowernetworks.opendatasoft.com"; 
        private TaskRunner _taskRunner;    
        private List<LTDSDemandRecord> _ltdsRecords;
        private readonly Dictionary<string,DNOAreas> _licenceAreasDict = new Dictionary<string, DNOAreas>() {
            {"Eastern Power Networks (EPN)",DNOAreas.EastEngland},
            {"South Eastern Power Networks (SPN)", DNOAreas.SouthEastEngland},
            {"London Power Networks (LPN)", DNOAreas.London}
        };

        private const int NTASKS = 5;
        private int _tasksDone=0;

        public UKPowerNetworkLoader(TaskRunner taskRunner) {
            _taskRunner = taskRunner;
        }
        
        public void Load() {
            updateMessage("Loading LTDS demand records ...");
            _ltdsRecords = loadData<LTDSDemandRecord>("/api/records/1.0/search/?dataset=ltds-table-3a-load-data-observed&q=&facet=licencearea&facet=gridsupplypoint&facet=substation&facet=season&facet=year&refine.season=Winter&refine.year=22-23");
            checkCancelled();
            updateProgress();
            loadGSPs();
            loadPrimaries();
            loadSecondarySites();
        }

        private void checkCancelled() {
            _taskRunner?.CheckCancelled();
        }

        private void updateMessage(string message) {
            _taskRunner?.Update(message);
        }

        private void updateProgress() {
            _tasksDone++;
            int percent = (100*_tasksDone) / NTASKS;
            _taskRunner?.Update(percent);
        }

        private void loadGSPs() {

            updateMessage("Loading GSP records ...");
            var gspRecords = loadData<GSPRecord>($"/api/records/1.0/search/?dataset=ukpn-grid-supply-points&facet=gsp");
            foreach( var gspRecord in gspRecords) {
                if ( gspRecord.geo_shape.type=="Polygon") {
                    gspRecord.geo_shape.polygonCoords = gspRecord.geo_shape.coordinates.Deserialize<double[][][]>();                    
                } else if ( gspRecord.geo_shape.type == "MultiPolygon") {
                    gspRecord.geo_shape.multiPolygonCoords = gspRecord.geo_shape.coordinates.Deserialize<double[][][][]>();
                }
            }            

            // Add GSP
            checkCancelled();
            updateProgress();
            updateMessage("Processing GSP records ...");
            using( var da = new DataAccess()) {
                var toAdd = new List<GridSupplyPoint>();

                // Need a way of working out the GA since no info in api data                
                foreach( var gspRecord in gspRecords) {
                    var gsp = da.SupplyPoints.GetGridSupplyPointByName(gspRecord.gsp);
                    if ( gsp==null) {
                        GeographicalArea ga=null;
                        var ltdsRecord = _ltdsRecords.Where( m=>m.grid_supply_point == gspRecord.gsp).FirstOrDefault();
                        if ( ltdsRecord!=null ) {
                            if ( _licenceAreasDict.TryGetValue(ltdsRecord.license_area, out DNOAreas area) ) {
                                ga = da.Organisations.GetGeographicalArea(area);
                            } else {
                                Logger.Instance.LogErrorEvent($"Unexpected licence area [{ltdsRecord.license_area}] for GSP=[{gspRecord.gsp}], ignoring ...");
                                continue;
                            }
                        } else {
                            Logger.Instance.LogErrorEvent($"Could not find LTDS record for GSP=[{gspRecord.gsp}], ignoring ...");
                            continue;
                        }
                        gsp = new GridSupplyPoint(ImportSource.UKPowerNetworksOpenData,gspRecord.gsp,null,null,ga,ga.DistributionNetworkOperator);
                        Logger.Instance.LogInfoEvent($"Added new GSP=[{gspRecord.gsp}]");
                        toAdd.Add(gsp);
                    }
                    
                    // Position of GSP
                    gsp.GISData.Latitude = gspRecord.geo_point_2d[0];
                    gsp.GISData.Longitude = gspRecord.geo_point_2d[1];
                    // Boundary
                    try {
                        loadBoundaryData(gsp.GISData,gspRecord.geo_shape);
                    } catch (Exception e) {
                        Logger.Instance.LogErrorEvent(e.Message + $", for entry [{gsp.Name}]");
                    }
                    checkCancelled();
                }

                foreach( var gsp in toAdd) {
                    da.SupplyPoints.Add(gsp);
                    checkCancelled();
                }

                //
                checkCancelled();
                da.CommitChanges();
            }
            updateProgress();
        }

        private void loadBoundaryData(GISData gisData, GeoShape geoShape) {
            if ( geoShape.type=="MultiPolygon" ) {
                int length = geoShape.multiPolygonCoords[0][0].Length;
                gisData.BoundaryLatitudes = new double[length];
                gisData.BoundaryLongitudes = new double[length];
                int index=0;
                foreach( var coord in geoShape.multiPolygonCoords[0][0] ) {
                    gisData.BoundaryLongitudes[index] = coord[0];
                    gisData.BoundaryLatitudes[index] = coord[1];
                    index++;
                }
            } else if ( geoShape.type=="Polygon") {
                int length = geoShape.polygonCoords[0].Length;
                gisData.BoundaryLatitudes = new double[length];
                gisData.BoundaryLongitudes = new double[length];
                int index=0;
                foreach( var coord in geoShape.polygonCoords[0] ) {
                    gisData.BoundaryLongitudes[index] = coord[0];
                    gisData.BoundaryLatitudes[index] = coord[1];
                    index++;
                }
            } else {
                throw new Exception($"Unexpected geometry type=[{geoShape.type}]");
            }
        }

        private void loadPrimaries() {
            updateMessage("Loading Primary area records ...");
            var primAreaRecords = loadData<PrimaryAreaRecord>("/api/records/1.0/search/?dataset=ukpn_primary_postcode_area&facet=demandrag",
                        100,(loaded,total,records)=>{
                            updateMessage($"Loaded Primary area records [{loaded}] of [{total}]...");
            });
            updateProgress();
            foreach( var primRecord in primAreaRecords) {
                if ( primRecord.geo_shape.type=="Polygon") {
                    primRecord.geo_shape.polygonCoords = primRecord.geo_shape.coordinates.Deserialize<double[][][]>();                    
                } else if ( primRecord.geo_shape.type == "MultiPolygon") {
                    primRecord.geo_shape.multiPolygonCoords = primRecord.geo_shape.coordinates.Deserialize<double[][][][]>();
                }
            }

            // Add primaries
            updateMessage("Processing Primary area records ...");
            checkCancelled();
            using( var da = new DataAccess()) {
                var toAdd = new List<PrimarySubstation>();
                foreach( var primRecord in primAreaRecords) {
                    var pss = da.Substations.GetPrimarySubstation(ImportSource.UKPowerNetworksOpenData,primRecord.primary_feeder);
                    if ( pss==null) {
                        var ltdsRecord = _ltdsRecords.Where( m=>m.site_functional_location == primRecord.primary_site_functional_location).FirstOrDefault();
                        if ( ltdsRecord==null) {
                            ltdsRecord = _ltdsRecords.Where( m=>m.substation == primRecord.primary_substation_name).FirstOrDefault();
                        }
                        if ( ltdsRecord!=null) {
                            var gsp = da.SupplyPoints.GetGridSupplyPointByName(ltdsRecord.grid_supply_point);
                            if ( gsp!=null) {
                                pss = new PrimarySubstation(ImportSource.UKPowerNetworksOpenData,primRecord.primary_feeder,primRecord.primary_site_functional_location,gsp);
                                Logger.Instance.LogInfoEvent($"Added new Primary=[{primRecord.primary_substation_name}]");
                                toAdd.Add(pss);
                            } else {
                                Logger.Instance.LogWarningEvent($"Could not find GSP with name [{ltdsRecord.grid_supply_point}] for primary substation [{primRecord.primary_substation_name}], ignoring ....");
                                continue;
                            }
                        } else {
                            Logger.Instance.LogWarningEvent($"Could not find LTDS record with for primary substation [{primRecord.primary_site_functional_location}], ignoring ....");
                            continue;
                        }
                    }
                    pss.Name = primRecord.primary_substation_name;
                    // Position of GSP
                    pss.GISData.Latitude = primRecord.geo_point_2d[0];
                    pss.GISData.Longitude = primRecord.geo_point_2d[1];
                    // Boundary
                    try {
                        loadBoundaryData(pss.GISData,primRecord.geo_shape);
                    } catch (Exception e) {
                        Logger.Instance.LogErrorEvent(e.Message + $", for entry [{pss.Name}]");
                    }
                    checkCancelled();
                }

                foreach( var pss in toAdd) {
                    da.Substations.Add(pss);
                    checkCancelled();
                }

                //
                checkCancelled();
                da.CommitChanges();
            }
            updateProgress();
        }

        private void loadSecondarySites() {
            updateMessage("Loading Secondary Site records ...");
            var secondarySiteRecords = loadData<SecondarySiteRecord>(
                            "/api/records/1.0/search/?dataset=ukpn-secondary-sites&facet=dno&facet=substationdesign&facet=substationvoltage&facet=numberoftransformers&facet=localauthority&facet=indooroutdoor",
                            1000,(loaded,total,records)=>{
                                updateMessage($"Processing Secondary Site records [{loaded}] of [{total}]...");
                                processSecondarySites(records);
            });
            //
            updateProgress();
        }

        private void processSecondarySites(IEnumerable<SecondarySiteRecord> records) {
            using( var da = new DataAccess() ) {
                var toAdd = new List<DistributionSubstation>();
                foreach( var record in records) {
                    if ( record.llsoaname==null || record.geopoint==null ) {
                        Logger.Instance.LogInfoEvent("Ignoring blank record");
                        continue;
                    }
                    var dss = da.Substations.GetDistributionSubstation(ImportSource.UKPowerNetworksOpenData,record.functional_location);
                    if ( dss==null) {
                        var pss = da.Substations.GetPrimarySubstation(ImportSource.UKPowerNetworksOpenData,record.primary_feeder);
                        if ( pss!=null ) {
                            dss = new DistributionSubstation(ImportSource.UKPowerNetworksOpenData,record.functional_location,null,pss);
                            Logger.Instance.LogInfoEvent($"Added new Distribution substation=[{record.llsoaname}]");
                            toAdd.Add(dss);
                        } else {
                            Logger.Instance.LogWarningEvent($"Could not find Primary substation [{record.primary_feeder}], ignoring secondary site [{record.functional_location}]");
                            continue;
                        }
                    }
                    //
                    dss.Name = record.llsoaname;
                    dss.GISData.Latitude = record.geopoint[0];
                    dss.GISData.Longitude = record.geopoint[1];
                    checkCancelled();
                }

                checkCancelled();
                //
                foreach( var pss in toAdd) {
                    da.Substations.Add(pss);
                }

                //
                checkCancelled();

                //
                //??da.CommitChanges();
            }
        }

        private List<T> loadData<T>(string methodUrl,int rows=100, Action<int,int,IEnumerable<T>>? progress=null) where T : class {
            int start=0;
            Container<T> container;
            List<T> records=new List<T>();
            string methodStr;
            do {
                methodStr = methodUrl + $"&rows={rows}&start={start}";
                container = get<Container<T>>(methodStr);
                checkCancelled();
                if ( container.records.Length>0) {
                    foreach( var record in container.records) {
                        records.Add(record.fields);
                    }
                    progress?.Invoke(records.Count,container.nhits,container.records.Select(m=>m.fields));
                }
                start += container.records.Length;
            } while( container.records.Length>0 );
            //
            return records;
        }

        private class SecondarySiteRecord {
            public string llsoaname {get; set;}

            public double[] geopoint {get; set;}
                        
            public string postcode {get; set;}

            [JsonPropertyName("primaryfeeder")]
            public string primary_feeder {get; set;}
            
            public int customer_count {get; set;}

            [JsonPropertyName("functionallocation")]
            public string functional_location {get; set;}
        }

        private class LTDSDemandRecord {
            public string substation {get; set;}
            
            [JsonPropertyName("licencearea")]
            public string license_area {get; set;}

            [JsonPropertyName("gridsupplypoint")]
            public string grid_supply_point {get; set;}

            [JsonPropertyName("sitefunctionallocation")]
            public string site_functional_location {get; set;}
        }

        private class PrimaryAreaRecord {
            [JsonPropertyName("primary")]
            public string primary_feeder {get; set;}

            [JsonPropertyName("primarysubstationname")]
            public string primary_substation_name {get; set;}

            [JsonPropertyName("primarysitefunctionallocation")]
            public string primary_site_functional_location {get; set;}
            public double[] geo_point_2d {get; set;}
            public GeoShape geo_shape {get; set;}
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
            public int nhits {get; set;}
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