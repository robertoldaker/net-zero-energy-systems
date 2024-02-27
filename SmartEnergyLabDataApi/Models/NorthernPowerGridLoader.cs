using System.Diagnostics;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using HaloSoft.EventLogger;
using MySql.Data.Types;
using NHibernate.Criterion;
using Org.BouncyCastle.Crypto.Signers;
using SmartEnergyLabDataApi.Data;

namespace SmartEnergyLabDataApi.Models;

public class NorthernPowerGridLoader {
    private HttpClient _httpClient;
    private object _httpClientLock = new object();
    private string _baseUrl = "https://northernpowergrid.opendatasoft.com"; 
    // key generated from the UK power networks opn data website
    // https://ukpowernetworks.opendatasoft.com/account/api-keys/
    private string _apiKey = "dd2fce73b6487e3d3943580c048231b8b0518d0169db407ae981e7bc";
    private TaskRunner? _taskRunner; 
    private Dictionary<string,string> _upstreamSiteAliases = new Dictionary<string, string>() {
       { "Linton", "Blyth 132Kv"},
       { "Blyth 132/66Kv", "Blyth 66Kv"},
       { "Spennymoor", "Spennymoor Gsp"},
       { "Bradford", "Bradford West"},
    };
    private Dictionary<string,string> _primarySiteAliases = new Dictionary<string, string>() {
       { "Sharlston New", "Sharlston"},
       { "Roecliffe Industrial", "Boroughbridge/Roecliffe Industrial"},
    };

    private List<NPGSiteUtilisationRecord> _allRecords;
    private List<NPGSiteUtilisationRecord> _gspRecords;
    private List<NPGSiteUtilisationRecord> _primRecords;
    private List<NPGSiteUtilisationRecord> _distRecords;
    private List<NPGCombinedServiceAreaRecord> _boundaryRecords;

    private Regex _distRegEx1 = new Regex(@"(\s\d+)$");
    private Regex _distRegEx2 = new Regex(@"(33\/11)$");

    public NorthernPowerGridLoader(TaskRunner? taskRunner) {
        _taskRunner = taskRunner;
    }


    public void Load() {
        
        var url = "/api/explore/v2.1/catalog/datasets/npg-site-utilisation/exports/json?lang=en&timezone=Europe%2FLondon";
        _allRecords = get<List<NPGSiteUtilisationRecord>>(url);
        Logger.Instance.LogInfoEvent($"Loaded [{_allRecords.Count}] site utilisation records");
        checkCancelled();

        // Process GSPs
        _gspRecords = _allRecords.Where(m=>m.substation_class=="Grid Supply Point").ToList();
        var extraRecords = _allRecords.
                        // From email with NPG it appears that primaries/BSPs with associated_upstream_site == site_name mean they are actually connected to grid
                        // so am adding them as extra GSPs
                        Where( m=>m.site_name==m.associated_upstream_site && m.substation_class!="Distribution").
                        // But sometime this site already exists so do not add a second one
                        Where( m=>_gspRecords.Where(n=>n.site_name==m.site_name).Count()==0).ToList();
        _gspRecords.AddRange(extraRecords);
        processGspRecords();
        checkCancelled();

        // Process primaries (treating BSPs as primaries)
        _primRecords = _allRecords.Where(m=>m.substation_class=="Primary" || m.substation_class=="Bulk Supply Point").ToList();
        processPrimaryRecords();
        checkCancelled();

        // Nudge some GSPs so they do not block underlying primaries
        checkGspsPostImport();
        checkCancelled();

        // Process distribution
        _distRecords = _allRecords.Where(m=>m.substation_class=="Distribution").ToList();
        processDistRecords();
        checkCancelled();
        
        // Now load boundaries
        var boundUrl = "/api/explore/v2.1/catalog/datasets/substation_combined_service_areas/exports/json?lang=en&timezone=Europe%2FLondon";
        _boundaryRecords = get<List<NPGCombinedServiceAreaRecord>>(boundUrl);
        
        processBoundaryRecords();
        
    }

    private void processBoundaryRecords() {
        var boundaryLoader = new GISBoundaryLoader(this);
        using ( var da = new DataAccess() ) {
            foreach( var record in _boundaryRecords) {
                GISData gisData=null;
                var name = record.primary;
                if ( record.substation_class=="Primary" || record.substation_class=="BSP" ) {
                    var pss = da.Substations.GetPrimarySubstation(ImportSource.NorthernPowerGridOpenData,null,null,name);
                    if ( pss!=null) {
                        gisData = pss.GISData;
                    } else {
                        Logger.Instance.LogWarningEvent($"Cannot find primary substation [{name}]");
                    }
                } else if ( record.substation_class == "GSP") {
                    var gsp = da.SupplyPoints.GetGridSupplyPointLike(name);
                    if ( gsp!=null) {
                        gisData = gsp.GISData;
                    } else {
                        Logger.Instance.LogWarningEvent($"Cannot find GSP [{name}]");
                    }
                } else {
                    Logger.Instance.LogWarningEvent($"Unexpected substation_class found [{record.substation_class}]");
                }
                //
                if ( gisData!=null) {
                    // This stores the geometry and GISData id for later loading
                    boundaryLoader.AddGISData(gisData,record.geo_shape.geometry);
                }
            }
        }
        // Add/remove/edit boundaries as required
        boundaryLoader.Load();
    }

    private class GISBoundaryLoader {
        private Dictionary<int,Geometry> _geometryDict = new Dictionary<int, Geometry>();

        private NorthernPowerGridLoader _parentLoader;

        public GISBoundaryLoader(NorthernPowerGridLoader parentLoader) {
            _parentLoader = parentLoader;
        }
        public void AddGISData( GISData data, Geometry geometry) {
            if ( _geometryDict.ContainsKey(data.Id)) {
                Logger.Instance.LogWarningEvent($"Duplicate key found in feature dict for gidDataId=[{data.Id}]");
            } else {
                _geometryDict.Add(data.Id,geometry);
            }
        }

        private void addNewBoundaries(DataAccess da, GISData gisData, Geometry geometry) {
            var boundaries = new List<GISBoundary>();
            var boundariesToAdd = new List<GISBoundary>();
            var boundariesToDelete = new List<GISBoundary>();
            var elements = geometry.coordinates.Deserialize<double[][][][]>();
            if ( elements!=null && boundaries!=null) {
                gisData.UpdateBoundaryPoints(elements,boundaries, boundariesToAdd, boundariesToDelete);
            }
            // add boundaries
            foreach( var boundary in boundariesToAdd) {
                da.GIS.Add(boundary);
                _parentLoader.checkCancelled();
            }
        }

        public void Load() {
            var sw = new Stopwatch();
            sw.Start();
            using( var da = new DataAccess() ) {
                _parentLoader.checkCancelled();
                var boundaryDict = da.GIS.GetBoundaryDict(_geometryDict.Keys.ToArray());
                _parentLoader.checkCancelled();

                var boundariesToAdd = new List<GISBoundary>();
                var boundariesToDelete = new List<GISBoundary>();
                foreach( var k in _geometryDict.Keys) {
                    var gisData = da.GIS.Get<GISData>(k);
                    if ( gisData!=null ) {
                        var geometry = _geometryDict[k];
                        IList<GISBoundary> boundaries;
                        if ( boundaryDict.ContainsKey(k)) {
                            boundaries = boundaryDict[k];
                        } else {
                            boundaries = new List<GISBoundary>();
                        }
                        var elements = geometry.coordinates.Deserialize<double[][][][]>();
                        if ( elements!=null && boundaries!=null) {
                            updateBoundaryPoints(gisData,elements,boundaries, boundariesToAdd, boundariesToDelete);
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
            sw.Stop();
            //
            Logger.Instance.LogInfoEvent($"Boundary load for [{_geometryDict.Keys.Count}] features done in {sw.Elapsed}s");
        }
        private void updateBoundaryPoints(GISData gisData, double[][][][] elements, 
                IList<GISBoundary> boundaries, 
                IList<GISBoundary> boundariesToAdd,
                IList<GISBoundary> boundariesToDelete) {
            var numBoundaries = elements.Length;
            if ( numBoundaries>1) {
                //??Logger.Instance.LogInfoEvent($"Num boundaries > 1 [{numBoundaries}]");
            }

            gisData.AdjustBoundaryLists(numBoundaries,boundaries,boundariesToAdd,boundariesToDelete);

            for(int i=0; i<numBoundaries;i++) {
                int length = elements[i][0].Length;
                boundaries[i].Latitudes = new double[length];
                boundaries[i].Longitudes = new double[length];
                for(int index=0; index<length; index++) {
                    boundaries[i].Latitudes[index] = elements[i][0][index][1];
                    boundaries[i].Longitudes[index] = elements[i][0][index][0];
                }
            }
        }

    }


    private void checkGspsPostImport() {
        using( var da = new DataAccess() ) {
            var dno = da.Organisations.GetDistributionNetworkOperator(DNOCode.NorthernPowerGrid);
            var gsps = da.SupplyPoints.GetGridSupplyPoints(dno);
            var toDelete = new List<GridSupplyPoint>();
            foreach( var gsp in gsps) {
                // Remove ones with no primaries
                if ( gsp.NumberOfPrimarySubstations==0 ) {
                    toDelete.Add(gsp);
                }
                //
                var pss = da.Substations.GetPrimarySubstationAtLocation(gsp.GISData.Latitude,gsp.GISData.Longitude);
                // nudge it if the primary is one of its children
                if ( pss!=null && pss.GridSupplyPoint.Id==gsp.Id) {
                    // This will tell the gui to nudge the GSP out of the way
                    if ( !gsp.NeedsNudge ) {
                        gsp.NeedsNudge = true;
                        Logger.Instance.LogInfoEvent($"Nudging GSP [{gsp.Name}] since located over Primary [{pss.Name}]");
                    }
                }                
            }
            foreach( var gsp in toDelete) {
                da.SupplyPoints.Delete(gsp);
                Logger.Instance.LogInfoEvent($"Removing GSP with no primary substations attached [{gsp.Name}]");
            }
            //
            da.CommitChanges();
        }
    }

    private void processGspRecords() {
        updateMessage("Processing GSP records ...");
        using ( var da = new DataAccess()) {
            int nAdded=0;
            var dno = da.Organisations.GetDistributionNetworkOperator(DNOCode.NorthernPowerGrid);
            foreach( var record in _gspRecords) {
                var gsp = da.SupplyPoints.GetGridSupplyPointByName(record.site_name);
                if ( gsp==null) {
                    var ga = getGeographicalArea(da, record);
                    gsp = new GridSupplyPoint(ImportSource.NorthernPowerGridOpenData,record.site_name,null,null,ga,dno);
                    da.SupplyPoints.Add(gsp);
                    nAdded++;
                }
                // update fields
                gsp.GISData.Latitude = record.geopoint.lat;
                gsp.GISData.Longitude = record.geopoint.lon;
                // Note these are dummy GSPs for primary/BSP that are connected to the grid - need to offset them to allow the underlying primary/BSP
                // to be shown
                if ( record.site_name==record.associated_upstream_site ) {
                    gsp.IsDummy = true;
                }
            }
            da.CommitChanges();
            Logger.Instance.LogInfoEvent($"[{nAdded}] GSPs added");
        }
    }


    private void processPrimaryRecords() {
        updateMessage("Processing Primary records ...");        
        using ( var da = new DataAccess()) {
            int nAdded=0;
            var dno = da.Organisations.GetDistributionNetworkOperator(DNOCode.NorthernPowerGrid);
            foreach( var primRecord in _primRecords) {
                var gsp = getGridSupplyPoint(da,primRecord);
                if ( gsp!=null) {
                    var pss = da.Substations.GetPrimarySubstation(ImportSource.NorthernPowerGridOpenData,null,null,primRecord.site_name);
                    if ( pss==null) {
                        pss = new PrimarySubstation(ImportSource.NorthernPowerGridOpenData,null,null,gsp);
                        pss.Name = primRecord.site_name;
                        da.Substations.Add(pss);
                        nAdded++;
                    }
                    // update fields
                    pss.GISData.Latitude = primRecord.geopoint.lat;
                    pss.GISData.Longitude = primRecord.geopoint.lon;
                } else {
                    Logger.Instance.LogWarningEvent($"Cannot find associated upstream site with name [{primRecord.associated_upstream_site}] for primary [{primRecord.site_name}]");
                }
            }
            da.CommitChanges();
            updateMessage($"[{nAdded}] primary substations added");
        }
    }

    private string getLocationKey(NPGSiteUtilisationRecord record) {
        return $"{record.geopoint.lat:F6}:{record.geopoint.lon:F6}";
    }

    private GridSupplyPoint getGridSupplyPoint(DataAccess da, NPGSiteUtilisationRecord primRecord) {
        // First see if we have a GSP in the db
        var gsp = da.SupplyPoints.GetGridSupplyPointByName(primRecord.associated_upstream_site);
        if ( gsp==null ) {
            // see if its connected to a BSP and use this??
            var bspRecord = _allRecords.Where( m=>m.site_name == primRecord.associated_upstream_site).FirstOrDefault();
            if ( bspRecord!=null) {
                gsp = da.SupplyPoints.GetGridSupplyPointByName(bspRecord.associated_upstream_site);
            }
        }
        if ( gsp==null) {
            // Some entries use the wrong name so see if the name is in the list of upstream aliases
            if ( _upstreamSiteAliases.ContainsKey(primRecord.associated_upstream_site) ) {
                var site_alias = _upstreamSiteAliases[primRecord.associated_upstream_site];
                gsp = da.SupplyPoints.GetGridSupplyPointByName(site_alias);
            }
        }
        return gsp;
    }

    private GeographicalArea getGeographicalArea(DataAccess da, NPGSiteUtilisationRecord record) {
        if ( record.licence_area == "Yorkshire" ) {
            return da.Organisations.GetGeographicalArea(DNOAreas.Yorkshire);
        } else if ( record.licence_area == "Northeast") {
            return da.Organisations.GetGeographicalArea(DNOAreas.NorthEastEngland);
        } else {
            throw new Exception($"Unexpected license area for NPG import [{record.licence_area}]");
        }
    }

    private void processDistRecords() {
        int nAdded = 0;
        int nIgnored = 0;
        Dictionary<string,bool> notFound=new Dictionary<string, bool>();
        processSegments<NPGSiteUtilisationRecord>(_distRecords,
                        // Note - its quicker to process using 100 instead of 1000
                        100,(loaded,total,records)=>{
                            updateMessage($"Processing NPG site utilisation records [{loaded}] of [{total}]...",false);
                            int percent = (int) (100*loaded)/total;
                            updateProgress(percent);
                            processRecords(records, ref nAdded, ref nIgnored, notFound);
        });
        Logger.Instance.LogInfoEvent($"[{nAdded}] distribution substations added, [{nIgnored}] ignored");
    }

    private void processSegments<T>(List<T> allRecords,int rows=100, Action<int,int,IEnumerable<T>>? progress=null) where T : class {
        IEnumerable<T> records=null;
        int start=0;
        do {
            checkCancelled();
            records = allRecords.Skip(start).Take(rows);
            progress?.Invoke(start,allRecords.Count,records);
            start += rows;
        } while( start<allRecords.Count );
        //
    }

    private void processRecords(IEnumerable<NPGSiteUtilisationRecord> records, ref int nAdded, ref int nIgnored, Dictionary<string,bool> notFound) {
        checkCancelled();
        using ( var da = new DataAccess() ) {
            foreach( var distRecord in records) {
                var pss = getPrimarySubstation(da,distRecord);
                if ( pss!=null) {
                    var dss = da.Substations.GetDistributionSubstation(ImportSource.NorthernPowerGridOpenData,null,null,distRecord.site_name);
                    if ( dss==null) {
                        dss = new DistributionSubstation(ImportSource.NorthernPowerGridOpenData,null,null,pss);
                        dss.Name = distRecord.site_name;
                        da.Substations.Add(dss);    
                        nAdded++;
                    }
                    // update fields
                    dss.GISData.Latitude = distRecord.geopoint.lat;
                    dss.GISData.Longitude = distRecord.geopoint.lon;
                    dss.SubstationData.NumCustomers = distRecord.connected_customers!=null ? (int) Math.Round((double) distRecord.connected_customers) : 0;
                    dss.SubstationData.DayMaxDemand = distRecord.max_demand_kw!=null ? (double) distRecord.max_demand_kw : 0;
                    dss.SubstationData.Rating = distRecord.firm_capacity_kw!=null ? (double) distRecord.firm_capacity_kw : 0;
                    if ( distRecord.substation_type=="HV GM Substation") {
                        dss.SubstationData.Type = DistributionSubstationType.Ground;
                    } else if ( distRecord.substation_type=="HV Pole Mounted S/S") {
                        dss.SubstationData.Type = DistributionSubstationType.Pole;
                    } else {
                        Logger.Instance.LogWarningEvent($"Unexpected substation type found [{distRecord.substation_type}] for dist substation [{distRecord.site_name}]");
                    }
                } else {
                    if ( !notFound.ContainsKey(distRecord.associated_upstream_site)) {
                        Logger.Instance.LogWarningEvent($"Cannot find upstream site with name [{distRecord.associated_upstream_site}]");
                        notFound.Add(distRecord.associated_upstream_site,true);
                    }
                    nIgnored++;
                }                
            }
            //
            da.CommitChanges();
        }
    }

    private PrimarySubstation getPrimarySubstation(DataAccess da, NPGSiteUtilisationRecord distRecord) {
        // First see if we have a Primary in the db
        var pss = da.Substations.GetPrimarySubstation(ImportSource.NorthernPowerGridOpenData,null,null,distRecord.associated_upstream_site);
        if ( pss==null) {
            // If not then need to make changes to the name and then lookup
            var name = distRecord.associated_upstream_site;
            var match1 = _distRegEx1.Match(name);
            var match2 = _distRegEx2.Match(name);
            // See if its an alias
            if ( _primarySiteAliases.ContainsKey(name)) {
                name = _primarySiteAliases[name];
            } else if ( match1.Success ) {
                // e.g. "Fish Dam Lane 22496" => "Fist Dam Lane"
                name = name.Replace(match1.Groups[1].Value,"");
            } else if ( match2.Success) {
                // e.g. "Bingley 33/11 => "Bingley 33/11Kv"
                name = name.Replace(match2.Groups[1].Value,$"{match2.Groups[1].Value}Kv");
            }
            //
            var list = da.Substations.GetPrimarySubstationsLike(ImportSource.NorthernPowerGridOpenData,name);
            if ( list.Count>0 ) {
                pss = list[0];
            }
        }
        return pss;
    }


    private void checkCancelled() {
        _taskRunner?.CheckCancelled();
    }

    private void updateMessage(string message,bool addToLog=true) {
        _taskRunner?.Update(message,addToLog);
    }

    private void updateProgress(int percent) {
        _taskRunner?.Update(percent);
    }

    private class NPGCombinedServiceAreaRecord {
        public GeoPoint geo_point_2d {get; set;}
        public GeoShape geo_shape {get; set;}
        public string primary {get; set;}
        public string substation_class {get; set;}
    }

    private class GeoShape {
        public string type {get; set;}
        public Geometry geometry {get; set;}
    }

    private class Geometry {
        public string type {get; set;}
        public JsonElement coordinates {get; set;}        
    }

    private class GeoPoint {
        public double lat {get; set;}
        public double lon {get; set;}
    }

    private class NPGSiteUtilisationRecord {
        public string site_name { get; set;}
        public string eam_site_asset_id {get; set;}
        public string substation_type {get; set;}
        public string licence_area {get; set;}
        public double? max_demand_kw {get; set;}
        public double? connected_customers {get; set;}
        public double? firm_capacity_kw {get; set;}
        public string substation_class {get; set;}
        public string associated_upstream_site {get; set;}
        public GeoPoint geopoint {get; set;}
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