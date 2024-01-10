using System.Text;
using System.Text.Json;
using HaloSoft.EventLogger;
using SmartEnergyLabDataApi.Data;
using System.Text.Json.Serialization;
using NHibernate.Criterion;
using System.Text.RegularExpressions;

namespace SmartEnergyLabDataApi.Models
{
    public class UKPowerNetworkLoader {

        private HttpClient _httpClient;
        private object _httpClientLock = new object();
        private string _baseUrl = "https://ukpowernetworks.opendatasoft.com"; 
        private string _apiKey = "93ad678d8dd616b2deb533bbb43c636ea09eca9faa8ce20b9c696c37";
        private TaskRunner _taskRunner;    
        private List<LTDSDemandRecord> _ltdsRecords;
        private readonly Dictionary<string,DNOAreas> _licenceAreasDict = new Dictionary<string, DNOAreas>() {
            {"Eastern Power Networks (EPN)",DNOAreas.EastEngland},
            {"South Eastern Power Networks (SPN)", DNOAreas.SouthEastEngland},
            {"London Power Networks (LPN)", DNOAreas.London}
        };
        private ProcessResult _distProcessResult = new ProcessResult("Distribution Substations");
        private ProcessResult _gspProcessResult = new ProcessResult("Grid Supply Points");
        private ProcessResult _primProcessResult = new ProcessResult("Primary Substations");
        private Regex _primaryFeederRegEx = new Regex(@"^(\w+\d+) ");
        private Dictionary<string,PrimarySubstation?> _primaryDict = new Dictionary<string, PrimarySubstation?>();

        private const int NTASKS = 6;
        private int _tasksDone=0;

        public UKPowerNetworkLoader(TaskRunner taskRunner) {
            _taskRunner = taskRunner;
        }
        
        public void Load() {
            updateMessage("Loading LTDS demand records ...");
            _ltdsRecords = loadData<LTDSDemandRecord>("/api/explore/v2.1/catalog/datasets/ltds-table-3a-load-data-observed/records?refine=year%3A%2223-24%22",100);
            checkCancelled();
            updateProgress();
            loadGSPs();
            loadPrimaries();
            loadSecondarySites();
        }

        private void checkCancelled() {
            _taskRunner?.CheckCancelled();
        }

        private void updateMessage(string message,bool addToLog=true) {
            _taskRunner?.Update(message,addToLog);
        }

        private void updateProgress() {
            _tasksDone++;
            int percent = (100*_tasksDone) / NTASKS;
            _taskRunner?.Update(percent);
        }

        private void loadGSPs() {

            updateMessage("Loading GSP records ...");
            _gspProcessResult.Reset();
            var gspRecords = loadData<GSPRecord>($"/api/explore/v2.1/catalog/datasets/ukpn-grid-supply-points/records");
            foreach( var gspRecord in gspRecords) {
                var geoShape = gspRecord.geo_shape.geometry;
                if ( geoShape.type=="Polygon") {
                    geoShape.polygonCoords = geoShape.coordinates.Deserialize<double[][][]>();                    
                } else if ( geoShape.type == "MultiPolygon") {
                    geoShape.multiPolygonCoords = geoShape.coordinates.Deserialize<double[][][][]>();
                } 
            }            

            // Add GSP
            checkCancelled();
            updateProgress();
            updateMessage("Processing GSP records ...");
            var boundaryLoader = new GISBoundaryLoader(this);

            //
            using( var da = new DataAccess()) {
                var toAdd = new List<dynamic>();
              
                // Need a way of working out the GA since no info in api data                
                foreach( var gspRecord in gspRecords) {
                    _gspProcessResult.NumProcessed++;
                    var gsp = da.SupplyPoints.GetGridSupplyPointByName(gspRecord.gsp);
                    bool added = false;
                    if ( gsp==null) {
                        GeographicalArea ga=null;
                        var ltdsRecord = _ltdsRecords.Where( m=>m.grid_supply_point == gspRecord.gsp).FirstOrDefault();
                        if ( ltdsRecord!=null ) {
                            if ( _licenceAreasDict.TryGetValue(ltdsRecord.license_area, out DNOAreas area) ) {
                                ga = da.Organisations.GetGeographicalArea(area);
                            } else {
                                Logger.Instance.LogErrorEvent($"Unexpected licence area [{ltdsRecord.license_area}] for GSP=[{gspRecord.gsp}], ignoring ...");
                                _gspProcessResult.NumIgnored++;
                                continue;
                            }
                        } else {
                            Logger.Instance.LogErrorEvent($"Could not find LTDS record for GSP=[{gspRecord.gsp}], ignoring ...");
                            _gspProcessResult.NumIgnored++;
                            continue;
                        }
                        gsp = new GridSupplyPoint(ImportSource.UKPowerNetworksOpenData,gspRecord.gsp,null,null,ga,ga.DistributionNetworkOperator);
                        Logger.Instance.LogInfoEvent($"Added new GSP=[{gspRecord.gsp}]");
                        _gspProcessResult.NumAdded++;
                        added = true;
                        toAdd.Add(new { gsp=gsp,gspRecord=gspRecord});
                    }
                    
                    // update GSP using record
                    var updated=gspRecord.update(gsp);
                    if ( updated && !added ) {
                        _gspProcessResult.NumModified++;
                    }
                    if ( !added ) {
                        // Boundary
                        try {
                            boundaryLoader.AddGISData(gsp.GISData,gspRecord.geo_shape.geometry);
                        } catch (Exception e) {
                            Logger.Instance.LogErrorEvent(e.Message + $", for entry [{gsp.Name}]");
                        }
                    }
                    checkCancelled();
                }

                foreach( var obj in toAdd) {
                    da.SupplyPoints.Add(obj.gsp);
                    // Boundary
                    try {
                        boundaryLoader.AddGISData(obj.gsp.GISData,obj.gspRecord.geo_shape.geometry);
                    } catch (Exception e) {
                        Logger.Instance.LogErrorEvent(e.Message + $", for entry [{obj.gsp.Name}]");
                    }
                    //
                    checkCancelled();
                }
                //
                checkCancelled();

                //
                checkCancelled();
                da.CommitChanges();
            }
            // loads all boundaries separately
            boundaryLoader.Load();
            //
            updateProgress();
            //
            Logger.Instance.LogInfoEvent(_gspProcessResult.ToString());
        }

        private class GISBoundaryLoader {
            private Dictionary<int,GeoShape> _geoShapeDict = new Dictionary<int, GeoShape>();

            private UKPowerNetworkLoader _parentLoader;

            public GISBoundaryLoader(UKPowerNetworkLoader parentLoader) {
                _parentLoader = parentLoader;
            }
            public void AddGISData( GISData data, GeoShape geoShape) {
                if ( _geoShapeDict.ContainsKey(data.Id)) {
                    Logger.Instance.LogWarningEvent($"Duplicate key found in feature dict for gidDataId=[{data.Id}]");
                } else {
                    _geoShapeDict.Add(data.Id,geoShape);
                }
            }

            public void Load() {
                using( var da = new DataAccess() ) {
                    _parentLoader.checkCancelled();
                    var boundaryDict = da.GIS.GetBoundaryDict(_geoShapeDict.Keys.ToArray());
                    _parentLoader.checkCancelled();

                    var boundariesToAdd = new List<GISBoundary>();
                    var boundariesToDelete = new List<GISBoundary>();

                    foreach( var k in _geoShapeDict.Keys) {
                        var gisData = da.GIS.Get<GISData>(k);
                        if ( gisData!=null ) {
                            var geoShape = _geoShapeDict[k];
                            IList<GISBoundary> boundaries;
                            if ( boundaryDict.ContainsKey(k)) {
                                boundaries = boundaryDict[k];
                            } else {
                                boundaries = new List<GISBoundary>();
                            }
                            if ( boundaries!=null) {
                                gisData.UpdateBoundaryPoints(geoShape,boundaries, boundariesToAdd, boundariesToDelete);
                            }
                        }
                        _parentLoader.checkCancelled();
                    }
                    // add boundaries
                    foreach( var boundary in boundariesToAdd) {
                       da.GIS.Add(boundary);
                        _parentLoader.checkCancelled();
                    }
                    // remove boundaries
                    foreach( var boundary in boundariesToDelete) {
                       da.GIS.Delete(boundary);
                        _parentLoader.checkCancelled();
                    }
                    _parentLoader.checkCancelled();
                    //
                    da.CommitChanges();
                }
                //
                //??sw.Stop();
                //
                //??Logger.Instance.LogInfoEvent($"Boundary load for [{_featureDict.Keys.Count}] features done in {sw.Elapsed}s");
            }
        }

        private void loadPrimaries() {
            updateMessage("Loading Primary area records ...");
            _primProcessResult.Reset();
            var primAreaRecords = loadData<PrimaryAreaRecord>("/api/explore/v2.1/catalog/datasets/ukpn_primary_postcode_area/records",
                        100,(loaded,total,records)=>{
                            updateMessage($"Loaded Primary area records [{loaded}] of [{total}]...",false);
            });
            updateProgress();
            foreach( var primRecord in primAreaRecords) {
                var geoShape = primRecord.geo_shape.geometry;
                if ( geoShape.type=="Polygon") {
                    geoShape.polygonCoords = geoShape.coordinates.Deserialize<double[][][]>();                    
                } else if ( geoShape.type == "MultiPolygon" ) {
                    geoShape.multiPolygonCoords = geoShape.coordinates.Deserialize<double[][][][]>();
                } else {
                    Logger.Instance.LogInfoEvent($"Unexpected geo shape type [{geoShape.type}] [{primRecord.primary_substation_name}]");
                }
            }

            // Add primaries
            updateMessage("Processing Primary area records ...");
            var boundaryLoader = new GISBoundaryLoader(this);
            checkCancelled();
            using( var da = new DataAccess()) {
                var toAdd = new List<dynamic>();

                foreach( var primRecord in primAreaRecords) {
                    _primProcessResult.NumProcessed++;
                    bool added = false;
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
                                _primProcessResult.NumAdded++;
                                added = true;
                                toAdd.Add(new { pss=pss, primRecord=primRecord });
                            } else {
                                Logger.Instance.LogWarningEvent($"Could not find GSP with name [{ltdsRecord.grid_supply_point}] for primary substation [{primRecord.primary_substation_name}], ignoring ....");
                                _primProcessResult.NumIgnored++;
                                continue;
                            }
                        } else {
                            Logger.Instance.LogWarningEvent($"Could not find LTDS record with for primary substation [{primRecord.primary_site_functional_location}], ignoring ....");
                            _primProcessResult.NumIgnored++;
                            continue;
                        }
                    }
                    //
                    var updated = primRecord.update(pss);
                    //
                    if ( !added && updated) {
                        _primProcessResult.NumModified++;
                    }
                    // Boundary
                    if ( !added ) {
                        try {
                            boundaryLoader.AddGISData(pss.GISData,primRecord.geo_shape.geometry);
                        } catch (Exception e) {
                            Logger.Instance.LogErrorEvent(e.Message + $", for entry [{pss.Name}]");
                        }
                    }
                    checkCancelled();
                }

                foreach( var obj in toAdd) {
                    da.Substations.Add(obj.pss);
                    //
                    try {
                        boundaryLoader.AddGISData(obj.pss.GISData,obj.primRecord.geo_shape.geometry);
                    } catch (Exception e) {
                        Logger.Instance.LogErrorEvent(e.Message + $", for entry [{obj.pss.Name}]");
                    }
                    checkCancelled();
                }

                //
                checkCancelled();
                da.CommitChanges();
            }
            boundaryLoader.Load();
            updateProgress();
            Logger.Instance.LogInfoEvent(_primProcessResult.ToString());
        }

        private void loadSecondarySites() {
            updateMessage("Loading Secondary Site records ...");
            _distProcessResult.Reset();
            _primaryDict.Clear();
            exportDataAsJson<SecondarySiteRecord>(
                            "/api/explore/v2.1/catalog/datasets/ukpn-secondary-sites/exports/json?lang=en&timezone=Europe%2FLondon",
                            // Note - its quicker to process using 100 instead of 1000
                            100,(loaded,total,records)=>{
                                updateMessage($"Processing Secondary Site records [{loaded}] of [{total}]...",false);
                                processSecondarySites(records);
            });
            //
            updateProgress();
            //
            var missingPrimaries = _primaryDict.Where( m=>m.Value==null).Select(m=>m.Key).Distinct();
            foreach( var pFeeder in missingPrimaries) {
                Logger.Instance.LogWarningEvent($"Could not find primary [{pFeeder}]");
            }
            //
            Logger.Instance.LogInfoEvent(_distProcessResult.ToString());
        }

        private void processSecondarySites(IEnumerable<SecondarySiteRecord> records) {
            using( var da = new DataAccess() ) {
                var toAdd = new List<DistributionSubstation>();
                foreach( var record in records) {
                    bool added = false;
                    if ( record.llsoaname==null || record.geopoint==null ) {
                        _distProcessResult.NumBlank++;
                        continue;
                    }
                    var dss = da.Substations.GetDistributionSubstationByExternalId(record.functional_location);
                    if ( dss==null) {
                        var pss = getPrimarySubstation(da,record.primary_feeder);
                        if ( pss!=null ) {
                            dss = new DistributionSubstation(ImportSource.UKPowerNetworksOpenData,record.functional_location,null,pss);
                            //??Logger.Instance.LogInfoEvent($"Added new Distribution substation=[{record.llsoaname}]");
                            _distProcessResult.NumAdded++;
                            added=true;
                            toAdd.Add(dss);
                        } else {
                            //??Logger.Instance.LogWarningEvent($"Could not find Primary substation [{record.primary_feeder}], ignoring secondary site [{record.functional_location}]");
                            _distProcessResult.NumIgnored++;
                            continue;
                        }
                    }
                    //
                    if ( record.update(dss) && !added ) {
                        _distProcessResult.NumModified++;
                    }
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
                da.CommitChanges();
                //
                _distProcessResult.NumProcessed+=records.Count();
            }
        }

        private PrimarySubstation getPrimarySubstation(DataAccess da, string primaryFeeder) {
            PrimarySubstation pss=null;
            if ( primaryFeeder==null) {
                return null;
            }
            if ( _primaryDict.TryGetValue(primaryFeeder, out pss)) {
                return pss;
            }
            var match = _primaryFeederRegEx.Match(primaryFeeder);
            if ( match.Success ) {
                var pFeeder = match.Groups[1].Value;
                pss = da.Substations.GetPrimarySubstationLike(MatchMode.Start,ImportSource.UKPowerNetworksOpenData,pFeeder);
            } else {
                pss = da.Substations.GetPrimarySubstation(ImportSource.UKPowerNetworksOpenData,primaryFeeder);
            }
            _primaryDict.Add(primaryFeeder,pss);
            return pss;
        }

        private List<T> loadData<T>(string methodUrl,int rows=20, Action<int,int,IEnumerable<T>>? progress=null) where T : class {
            int start=0;
            Container<T> container;
            List<T> records=new List<T>();
            string methodStr;
            do {
                var separator = methodUrl.Contains('?') ? "&" : "?";
                methodStr = methodUrl + $"{separator}limit={rows}&offset={start}";
                container = get<Container<T>>(methodStr);
                checkCancelled();
                if ( container.results.Length>0) {
                    foreach( var record in container.results) {
                        records.Add(record);
                    }
                    progress?.Invoke(records.Count,container.total_count,container.results.Select(m=>m));
                }
                start += container.results.Length;
            } while( container.results.Length>0 );
            //
            return records;
        }

        private void exportDataAsJson<T>(string methodUrl,int rows=100, Action<int,int,IEnumerable<T>>? progress=null) where T : class {
            IEnumerable<T> records=null;
            var allRecords = get<List<T>>(methodUrl);;
            int start=0;
            do {
                checkCancelled();
                records = allRecords.Skip(start).Take(rows);
                progress?.Invoke(start,allRecords.Count,records);
                start += rows;
            } while( start<allRecords.Count );
            //
        }

        private class ProcessResult {
            private string _name;
            public ProcessResult(string name) {
                _name = name;
            }
            public override string ToString()
            {
                return $"{NumProcessed} {_name} processed, {NumAdded} added, {NumModified} modified, {NumIgnored} ignored, {NumBlank} blank";
            }
            public int NumAdded {get; set;}
            public int NumModified {get; set;}
            public int NumProcessed {get; set;}
            public int NumIgnored {get; set;}
            public int NumBlank {get; set;}
            public string Name {
                get {
                    return _name;
                }
            }
            public void Reset() {
                NumAdded= 0;
                NumModified = 0;
                NumIgnored=0;
                NumBlank=0;
            }
        }

        private class SecondarySiteRecord {
            public string llsoaname {get; set;}

            public GeoPoint geopoint {get; set;}
                        
            public string postcode {get; set;}

            [JsonPropertyName("primaryfeeder")]
            public string primary_feeder {get; set;}
            
            [JsonPropertyName("substationdesign")]
            public string substation_design {get; set;}

            public int customer_count {get; set;}

            [JsonPropertyName("functionallocation")]
            public string functional_location {get; set;}

            public bool update(DistributionSubstation dss) {
                bool updated = false;
                if ( dss.Name!=llsoaname ) {
                    dss.Name = llsoaname;
                    updated = true;
                }
                if ( dss.GISData.Latitude!=geopoint.lat) {
                    dss.GISData.Latitude = geopoint.lat;
                }
                if ( dss.GISData.Longitude!=geopoint.lon) {
                    dss.GISData.Longitude = geopoint.lon;
                }
                var distData = dss.SubstationData;
                if ( distData==null) {
                    distData=new DistributionSubstationData(dss);
                }
                // Type (pole/ground)
                if ( substation_design == "PMT") {
                    distData.Type = DistributionSubstationType.Pole;
                } else {
                    distData.Type = DistributionSubstationType.Ground;
                }
                //
                distData.NumCustomers = customer_count;
                return updated;
            }
        }

        private class GeoPoint {
            public double lat {get; set;}
            public double lon {get ;set;}
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
            public GeoPoint geo_point_2d {get; set;}
            public GeoShapeGeometry geo_shape {get; set;}

            public bool update(PrimarySubstation pss) {
                bool updated = false;
                if ( pss.Name != primary_substation_name) {
                    pss.Name = primary_substation_name;
                    updated = true;
                }
                // Position of GSP
                if ( pss.GISData.Latitude!=geo_point_2d.lat) {
                    pss.GISData.Latitude = geo_point_2d.lat;
                    updated = true;
                }
                if ( pss.GISData.Longitude!=geo_point_2d.lon) {
                    pss.GISData.Longitude = geo_point_2d.lon;
                    updated = true;
                }
                //
                return updated;
            }
        }

        private class GSPRecord {
            public string gsp {get; set;}
            public  GeoPoint geo_point_2d {get; set;}
            public GeoShapeGeometry geo_shape {get; set;}

            public bool update(GridSupplyPoint gsp) {
                bool updated = false;
                if ( gsp.GISData.Latitude!=geo_point_2d.lat ) {
                    gsp.GISData.Latitude = geo_point_2d.lat;
                    updated = true;
                }
                if ( gsp.GISData.Longitude!=geo_point_2d.lon ) {
                    gsp.GISData.Longitude = geo_point_2d.lon;
                    updated = true;
                }
                return updated;
            }

        }

        public class GeoShapeGeometry {
            public GeoShape geometry {get; set;}
        }

        public class GeoShape {
            public string type {get; set;}
            public JsonElement coordinates{ get; set; }
            public double[][][][] multiPolygonCoords {get; set;}
            public double[][][] polygonCoords {get; set;}
        }

        private class Fields<T> {
            public T fields {get; set;}
        }

        private class Container<T> {
            public int total_count{get; set;}
            public T[] results {get; set;}
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
                    _httpClient = new HttpClient() {
                        BaseAddress = new Uri(_baseUrl)
                    };
                    _httpClient.DefaultRequestHeaders.Add("Authorization",$"Apikey {_apiKey}");
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