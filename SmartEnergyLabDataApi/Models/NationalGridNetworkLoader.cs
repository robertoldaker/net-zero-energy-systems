using System;
using System.Diagnostics;
using System.Drawing;
using System.IO.Compression;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Web;
using CommonInterfaces.Models;
using HaloSoft.EventLogger;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc.TagHelpers;
using Microsoft.AspNetCore.WebUtilities;
using NHibernate.Util;
using Org.BouncyCastle.Crypto.Signers;
using SmartEnergyLabDataApi.BoundCalc;
using SmartEnergyLabDataApi.Data;
using SmartEnergyLabDataApi.Data.BoundCalc;
//using SmartEnergyLabDataApi.Data.Loadflow;

namespace SmartEnergyLabDataApi.Models
{
    public class NationalGridNetworkLoader {

        public enum NationalGridNetworkSource {All, NGET, SSE, SPT}

        // NGET urls
        private const string ALL_DATA_URL = "https://www.nationalgrid.com/electricity-transmission/document/81201/download";
        // SSE urls
        private readonly UriBuilder _sseSuperGridBuilder = new UriBuilder("https://ssentransmission.opendatasoft.com/api/explore/v2.1/catalog/datasets/ssen-transmission-substation-site-supergrid/exports/json?lang=en&timezone=GMT");
        //ssen-transmission-substation-site-grid
        private readonly UriBuilder _sseGridBuilder = new UriBuilder("https://ssentransmission.opendatasoft.com/api/explore/v2.1/catalog/datasets/ssen-transmission-substation-site-grid/exports/json?lang=en&timezone=GMT");
        private const string SSE_KEY = "d7abb9519f874dbf5094f63d6daa5e50b2e43977f4ee2a250fed0147";
        // SP networks url
        private readonly UriBuilder _SPShapefileInfoUrl = new UriBuilder("https://spenergynetworks.opendatasoft.com/api/explore/v2.1/catalog/datasets/gis-shapefiles-excluding-lv/records");
        private const string SP_KEY = "f35d7b18b9e425f34c37fa5d08d25c8ba7ea99692084815feef0a54c";

        private readonly Dictionary<string,string> _sseSiteAliases = new Dictionary<string, string>() {
            {"BRIDGE OF DUNN","BRIDGE OF DUN"},
            {"MILLENNIUM","MILLENNIUM EAST"},
            {"NANT","NANT (LOCH NANT)"},
            {"ARDKINGLASS","ARDKINGLAS"},
            {"SPITTAL SUPER","SPITTAL"},
            {"STRATHBRORA","STRATH BRORA"}
        };

        private HttpClient _httpClient;
        private object _httpClientLock = new object();

        private static Regex _nameRegex = new Regex(@"^([A-Z\s]+)(\s\d{1,3}KV|\s)",RegexOptions.IgnoreCase);

        private class InterConnector {
            public LatLng LatLng {get; private set;}
            public string Name {get; private set;}
            public InterConnector(string name, LatLng latLng) {
                Name = name;
                LatLng = latLng;
            }
        }

        private class LatLng {
            public double Lat {get; set;}
            public double Lng {get; set;}
        }

        // These are on the end of the inter-connectors so show them in their respective countries
        private static Dictionary<string,InterConnector> _nodeInterConnectors = new Dictionary<string, InterConnector>() {
            { "SELLX", new InterConnector("IFA1", new LatLng()                  { Lat=50.755175, Lng= 1.63329}) },
            { "CHILX", new InterConnector("IFA2", new LatLng()                  { Lat=49.84289,  Lng= 0.84227}) },
            { "RICHX", new InterConnector("NEMO", new LatLng()                  { Lat=51.142778, Lng= 2.86376}) },
            { "GRAIX", new InterConnector("BritNed", new LatLng()               { Lat=51.527152, Lng= 3.56688}) }, 
            { "GRAIX2",new InterConnector("NeuConnect",new LatLng()             { Lat=53.578771, Lng= 8.139178})},
            { "GRAIX3",new InterConnector("SouthernLink",new LatLng()           { Lat=52.02969,  Lng= 4.17204})},
            { "CONQX", new InterConnector("EWLink", new LatLng()                { Lat=53.714219, Lng=-6.21094}) },  
            { "BLYTX", new InterConnector("NorNed", new LatLng()                { Lat=58.35708,  Lng= 6.92481}) },
            { "BLYTX2", new InterConnector("ContinentalLink", new LatLng()      { Lat=58.005015, Lng= 7.534516}) },
            { "AUCHX", new InterConnector("Moyle", new LatLng()                 { Lat=55.11766,  Lng=-6.06103})},
            { "BICFX", new InterConnector("VikingLink",new LatLng()             { Lat=55.500674, Lng= 8.400294})},
            { "PEMBX", new InterConnector("GreenLink",new LatLng()              { Lat=52.290343, Lng=-7.000129})},
            { "LOVEX", new InterConnector("AQUIND",new LatLng()                 { Lat=50.05922,  Lng= 1.35011})},
            { "KEMSX", new InterConnector("CRONOS",new LatLng()                 { Lat=51.270249, Lng= 3.042515})},
            { "EXETX", new InterConnector("FabLink",new LatLng()                { Lat=49.650342, Lng=-1.556387})},
            { "KINOX", new InterConnector("GrindLink",new LatLng()              { Lat=51.022983, Lng= 2.160388})},
            { "CANTX", new InterConnector("Kulizumboo",new LatLng()             { Lat=50.967541, Lng= 1.925483})},
            { "LEISX", new InterConnector("LionLink",new LatLng()               { Lat=52.160005, Lng= 4.352833})},
            { "KILSX", new InterConnector("LIRIC",new LatLng()                  { Lat=55.22094,  Lng=-6.16118})},
            { "BODEX", new InterConnector("MARES",new LatLng()                  { Lat=53.485366, Lng=-6.098011})},
            { "PEHEX", new InterConnector("NorthConnect",new LatLng()           { Lat=60.509681, Lng= 7.185079})},
            { "CREBX", new InterConnector("AtlanticSuperConnector",new LatLng() { Lat=64.150369, Lng=-15.716492})},
        };

        public void Delete(GridSubstationLocationSource source) {
            using ( var da = new DataAccess() ) {
                da.NationalGrid.DeleteLocations(source);
                var subSource = GridSubstation.getSource(source);
                if ( subSource!=null ) {
                    da.NationalGrid.DeleteSubstations((GridSubstationSource) subSource);
                }
                //
                da.CommitChanges();
            }
        }


        public void Load(NationalGridNetworkSource source) {
            //
            updateInterConnectors();
            // Note loading SP networks first as it uses KIRK and BLYT which are used by NGET and so get overwritten
            // SP Networks
            if ( source == NationalGridNetworkSource.All || source == NationalGridNetworkSource.SPT ) {
                loadSPNetworks();
            }
            // NGET
            if ( source == NationalGridNetworkSource.All || source == NationalGridNetworkSource.NGET ) {
                loadNGET();
            }
            // SSE
            if ( source == NationalGridNetworkSource.All || source == NationalGridNetworkSource.SSE ) {
                loadSSE();
            }
        }

        private void updateInterConnectors() {
            using( var da = new DataAccess() ) {
                // these are locations for the interconnectors
                foreach( var code in _nodeInterConnectors.Keys) {
                    var ic = _nodeInterConnectors[code];
                    var loc = da.NationalGrid.GetGridSubstationLocation(code);
                    if ( loc==null ) {
                        loc = GridSubstationLocation.Create(code, GridSubstationLocationSource.Estimated, null); 
                        da.NationalGrid.Add(loc);  
                        Logger.Instance.LogInfoEvent($"Added new inter-connector end point [{code}]");
                    }
                    loc.Name = ic.Name;
                    loc.GISData.Latitude = ic.LatLng.Lat;
                    loc.GISData.Longitude = ic.LatLng.Lng;
                }                
                // add new ones found
                da.CommitChanges();
            }
        }

        private class SPShapefileInfo {
            public class Info {
                public class Download {
                    public string url {get; set;}
                }
                public string asset_type {get; set;}
                public string voltage {get; set;}
                public Download shapefiles_click_to_download {get; set;}
            }
            public Info[] results {get; set;}
        }

        private void loadSPNetworks() {
            //
            var uriBuilder = _SPShapefileInfoUrl;
            var geoJsonFile = loadSPShapefile(uriBuilder);
            //
            processSPGeojsonFile(geoJsonFile);

        }

        private string loadSPShapefile(UriBuilder uriBuilder) {

            var client = getHttpClient();
            //
            var query = HttpUtility.ParseQueryString(string.Empty);
            query["apikey"] = SP_KEY;
            uriBuilder.Query = query.ToString();
            var url = uriBuilder.ToString();
            //
            // Download info about shapefiles available
            using (HttpRequestMessage message = getRequestMessage(HttpMethod.Get, url)) {
                var response = client.SendAsync(message).Result;
                //
                if ( response.IsSuccessStatusCode) {
                    var str = response.Content.ReadAsStringAsync().Result;
                    var info = JsonSerializer.Deserialize<SPShapefileInfo>(str);
                    foreach( var result in info.results) {
                        if ( result.asset_type == "Ground Mounted Substations") {
                            return loadSPShapefile(result.shapefiles_click_to_download.url);
                        }
                    }
                    throw new Exception($"Could not find asset of type \"Ground Mounted Substations\" in list of shape files");
                } else {
                    throw new Exception($"Problem obtaining SP shapefile info [{response.StatusCode}] [{response.ReasonPhrase}]");
                }
            }
            //

        }

        private string loadSPShapefile(string uri) {

            var uriBuilder = new UriBuilder(uri);
            var query = HttpUtility.ParseQueryString(string.Empty);
            query["apikey"] = SP_KEY;
            uriBuilder.Query = query.ToString();
            var url = uriBuilder.ToString();
            //
            // Download json
            var client = getHttpClient();
            using (HttpRequestMessage message = getRequestMessage(HttpMethod.Get, url)) {
                var response = client.SendAsync(message).Result;
                //
                if ( response.IsSuccessStatusCode) {
                    var stream = response.Content.ReadAsStream();
                    var cd = response.Content.Headers.ContentDisposition;                    
                    if ( stream!=null && cd!=null && cd.FileName!=null) {
                        string fn = Path.Combine(AppFolders.Instance.Temp,cd.FileName);
                        fn=fn.Replace("\"","");
                        saveToFile(stream, fn);
                        string outFolder = Path.GetFileNameWithoutExtension(fn);                        
                        extractZip(AppFolders.Instance.Temp,cd.FileName, outFolder);
                        //

                        var zipContentsFolder = Path.Combine(AppFolders.Instance.Temp,outFolder);
                        //
                        var files = Directory.GetFiles(zipContentsFolder,"*.shp");
                        if ( files.Length>0 ) {
                            //
                            var geoJsonFile = files[0].Replace(".shp",".geojson");
                            convertToGeoJson(files[0],geoJsonFile);
                            return geoJsonFile;
                        } else {
                            throw new Exception($"No shape files found in folder [{zipContentsFolder}]");
                        }
                    } else {
                        throw new Exception("Unexpected response downloading SP shapefile info");
                    }
                } else {
                    throw new Exception($"Problem obtaining SP shapefile info [{response.StatusCode}] [{response.ReasonPhrase}]");
                }
            }

        }

        private void processSPGeojsonFile(string geoJsonFile) {
            using( var da = new DataAccess()) {

                GeoJson<SPSubstationProps> geoJson;
                using( var fs = new FileStream(geoJsonFile,FileMode.Open)) {
                    geoJson = JsonSerializer.Deserialize<GeoJson<SPSubstationProps>>(fs);
                    Logger.Instance.LogInfoEvent($"Found [{geoJson.features.Length}] features");                    
                }
                Regex nameRegEx1 = new Regex(@"^TS-([A-Z\-]{4})\d\s?([\w\-\s]*?)\s?([\d\s\/kKvV]+|SealingEnd|)$");
                Regex nameRegEx2 = new Regex(@"^([A-Z]{4})[\d]+$");
                var locDict = new Dictionary<string,List<GridSubstationLocation>>();
                int nAdded=0;
                int nFailed=0;
                foreach( var feature in geoJson.features) {
                    if (int.TryParse(feature.properties.PRIM_VOLT,out int voltage)) {
                        if ( voltage>=132 ) {
                            var m=nameRegEx1.Match(feature.properties.SPNAME);
                            string code = "";
                            string name = "";
                            if ( m.Success ) {
                                code = m.Groups[1].Value;
                                name = m.Groups[2].Value;
                            } else {
                                m=nameRegEx2.Match(feature.properties.SPNAME);
                                if ( m.Success) {
                                    code = m.Groups[1].Value;
                                } else {
                                    Logger.Instance.LogInfoEvent($"Cannot find match for name = [{feature.properties.SPNAME}]");
                                    nFailed++;
                                }
                            }
                            if ( !string.IsNullOrEmpty(code)) {
                                var loc = da.NationalGrid.GetGridSubstationLocation(code);
                                if ( loc==null ) {
                                    loc = GridSubstationLocation.Create(code, GridSubstationLocationSource.SPT, null); 
                                    da.NationalGrid.Add(loc);
                                    nAdded++;
                                }
                                if ( !string.IsNullOrEmpty(name) ) {
                                    loc.Name = name;
                                }
                                var coords = feature.geometry.coordinates.Deserialize<double[]>();
                                loc.GISData.Latitude = coords[1];
                                loc.GISData.Longitude = coords[0];
                                //
                            }
                        }
                    }
                }
                da.CommitChanges();
                Logger.Instance.LogInfoEvent($"Finished loading SP Netwrork substation, num added=[{nAdded}], failed=[{nFailed}]");
            }

            //

        }

        private void loadSSE() {

            // get list of substation codes from ETYS 
            var etysLoader = new BoundCalcETYSLoader(BoundCalcETYSLoader.BoundCalcLoadOptions.OnlyHighVoltageCircuits);
            var substationCodes = etysLoader.LoadSubstationCodes();
            //
            var gridItems = getSSEGridItems(_sseGridBuilder);
            var superGridItems = getSSEGridItems(_sseSuperGridBuilder);
            gridItems.AddRange(superGridItems);

            int nGridAdded=0;
            int nLocAdded=0;
            using( var da = new DataAccess() ) {
                foreach ( var gridItem in gridItems) {                    
                    // look for name in substationCodes that get read from appendix B of ETYS
                    if ( gridItem.name == null ) {
                        continue;
                    }
                    var name = getGridItemName(gridItem);
                    var sc = substationCodes.Values.Where( m=>isNameMatch(m,name) && m.Owner == BoundCalcETYSLoader.SubstationOwner.SHET).SingleOrDefault();
                    if ( sc==null) {
                        Logger.Instance.LogInfoEvent($"Cannot find SSE grid item [{name}]");
                        continue;
                    }

                    // Create grid substation if not there already
                    var gs=da.NationalGrid.GetGridSubstation(sc.Code);
                    if ( gs==null) {
                        gs = GridSubstation.Create(sc.Code, GridSubstationSource.SHET);
                        da.NationalGrid.Add(gs);
                        nGridAdded++;
                    }
                    gs.Name = gridItem.name;
                    gs.Voltage = getGridItemVoltage(gridItem);
                    gs.GISData.Latitude = gridItem.geo_point_2d.lat;
                    gs.GISData.Longitude = gridItem.geo_point_2d.lon;

                    // Grid substation location
                    var loc = da.NationalGrid.GetGridSubstationLocation(sc.Code);
                    if ( loc==null ) {
                        loc = GridSubstationLocation.Create(sc.Code, GridSubstationLocationSource.SHET, null); 
                        da.NationalGrid.Add(loc);                       
                        nLocAdded++;
                    }
                    loc.Name = sc.Name;
                    loc.GISData.Latitude = gridItem.geo_point_2d.lat;
                    loc.GISData.Longitude = gridItem.geo_point_2d.lon;
                }

                da.CommitChanges();
                Logger.Instance.LogInfoEvent($"SSE - [{nGridAdded}] grid substations and [{nLocAdded}] grid locations added");
            }
        }

        private string getGridItemName(SSEGridItem gridItem) {
            var name = gridItem.name.
                Replace(" SUPERGRID","").
                Replace(" GRID","").
                Replace(" POWER STATION","").
                Replace(" PS","").
                Replace(" WIND","").
                Replace(" 400kV","").
                Replace(" HVDC","");
            if ( _sseSiteAliases.ContainsKey(name)) {
                name = _sseSiteAliases[name];
            }
            return name;
        }

        private string getGridItemVoltage(SSEGridItem gridItem) {
            var voltage = $"{gridItem.operatingv/1000}kV";
            return voltage;
        }


        private bool isNameMatch(BoundCalcETYSLoader.SubstationCode code, string name) {
            var etysName = code.Name.
                    Replace(" GRID","").
                    Replace(" HYDRO","").
                    Replace(" WIND FARM","").
                    Replace(" WINDFARM","").
                    Replace(" WIND","").
                    Replace(" GSP","").
                    Replace(" SUBSTATION","").
                    Replace(" 132/33KV","").
                    Replace(" (SSE)","");
            return string.Compare(etysName,name)==0;
        }

        private List<SSEGridItem> getSSEGridItems(UriBuilder uriBuilder) {
            var client = getHttpClient();
            //
            var query = HttpUtility.ParseQueryString(string.Empty);
            query["apikey"] = SSE_KEY;
            uriBuilder.Query = query.ToString();
            var url = uriBuilder.ToString();
            //
            List<SSEGridItem> items;
            // Download json
            using (HttpRequestMessage message = getRequestMessage(HttpMethod.Get, url)) {
                var response = client.SendAsync(message).Result;
                //
                if ( response.IsSuccessStatusCode) {
                    var str = response.Content.ReadAsStringAsync().Result;
                    items = JsonSerializer.Deserialize<List<SSEGridItem>>(str);
                } else {
                    throw new Exception($"Problem obtaining SSE grid items [{response.StatusCode}] [{response.ReasonPhrase}]");
                }
            }
            //
            return items;
        }


        private class SSEGridItem {
            public GeoPoint geo_point_2d {get; set;}
            public string name {get; set;}
            public int operatingv {get; set;}
        }

        private class GeoPoint {
            public double lat {get; set;}
            public double lon {get; set;}
        }


        private void loadNGET() {
            var client = getHttpClient();
            string substationsGeoJsonFile=null;
            string ohlGeoJsonFile = null;

            // Download json
            using (HttpRequestMessage message = getRequestMessage(HttpMethod.Get, ALL_DATA_URL)) {
                //
                var response = client.SendAsync(message).Result;
                //
                if ( response.IsSuccessStatusCode) {
                    var stream = response.Content.ReadAsStream();
                    var cd = response.Content.Headers.ContentDisposition;                    
                    if ( stream!=null && cd!=null && cd.FileName!=null) {
                        string fn = Path.Combine(AppFolders.Instance.Temp,cd.FileName);
                        saveToFile(stream, fn);
                        string outFolder = Path.GetFileNameWithoutExtension(fn);                        
                        extractZip(AppFolders.Instance.Temp,cd.FileName, outFolder);
                        //
                        var shapeFile = Path.Combine(AppFolders.Instance.Temp,outFolder,"Substations.shp");
                        substationsGeoJsonFile = shapeFile.Replace(".shp",".geojson");
                        convertToGeoJson(shapeFile,substationsGeoJsonFile);
                        //
                        shapeFile = Path.Combine(AppFolders.Instance.Temp,outFolder,"OHL.shp");                        
                        ohlGeoJsonFile = shapeFile.Replace(".shp",".geojson");
                        convertToGeoJson(shapeFile,ohlGeoJsonFile);
                    }
                }
            }
            if ( substationsGeoJsonFile!=null) {
                processNGETSubstationsGeoJson(substationsGeoJsonFile);
            }
            if ( ohlGeoJsonFile!=null) {
                processNGETOhlGeoJson(ohlGeoJsonFile);
            }
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

        private void convertToGeoJson(string shapeFile, string geoJsonFile) {
            var processStartInfo = new ProcessStartInfo();
            processStartInfo.CreateNoWindow = true;
            processStartInfo.UseShellExecute = false;
            processStartInfo.Arguments = $"-f GeoJSON -t_srs EPSG:4326 \"{geoJsonFile}\" \"{shapeFile}\"";
            // Can't get the service to pick up ogr2ogr so have to mention it explicitly
            if ( AppEnvironment.Instance.Context == Context.Production) {
                processStartInfo.FileName = "/home/roberto/anaconda3/bin/ogr2ogr";
            } else if ( Environment.OSVersion.Platform == PlatformID.Win32NT ) {
                // to install on windows see "https://www.osgeo.org/projects/osgeo4w/"
                processStartInfo.FileName = "C:\\OSGeo4W\\bin\\ogr2ogr";
            } else {
                processStartInfo.FileName = "ogr2ogr";
            }

            // enable raising events because Process does not raise events by default
            Logger.Instance.LogInfoEvent($"Running ogr2ogr");
            Logger.Instance.LogInfoEvent($"Filename=[{processStartInfo.FileName}]");
            Logger.Instance.LogInfoEvent($"Arguments=[{processStartInfo.Arguments}]");
            processStartInfo.UseShellExecute = false;
            processStartInfo.RedirectStandardError = true;
            var process = new Process();
            process.StartInfo = processStartInfo;

            process.Start();

            process.WaitForExit();

            if ( process.ExitCode!=0) {
                var error = process.StandardError.ReadToEnd();
                throw new Exception($"Could not run ogr2gr error [{error}]");
            }
        }

        private HttpClient getHttpClient()
        {
            if (_httpClient == null) {
                lock (_httpClientLock) {
                    _httpClient = new HttpClient();
                }
            }
            //
            return _httpClient;
        }

        private void saveToFile(Stream stream, string filename) {

            var buffer = new byte[32768];
            using( var fs = new FileStream(filename,FileMode.Create)) {
                int nRead;
                while( (nRead = stream.Read(buffer, 0, buffer.Length)) !=0) {
                    fs.Write(buffer,0,nRead);
                };
            }
        }

        private void _extractZip(string folder, string fn, string outFolder) {
             var processStartInfo = new ProcessStartInfo();
            processStartInfo.CreateNoWindow = true;
            processStartInfo.UseShellExecute = false;
            processStartInfo.WorkingDirectory = folder;

            processStartInfo.Arguments = $"-o \"{fn}\" -d \"{outFolder}\"";
            // 
            processStartInfo.FileName = "unzip";

            // enable raising events because Process does not raise events by default
            processStartInfo.UseShellExecute = false;
            var process = new Process();
            process.StartInfo = processStartInfo;

            process.Start();

            process.WaitForExit();
           
        }

        private void extractZip(string folder, string fn, string outFolder) {
            fn = fn.Trim('"');
            string zipPath = Path.Combine(folder,fn);

            outFolder = Path.Combine(folder,outFolder);

            ZipFile.ExtractToDirectory(zipPath, outFolder, true);

        }


        private void processNGETSubstationsGeoJson(string geoJsonFile) {
            using( var da = new DataAccess()) {

                GeoJson<SubstationProps> geoJson;
                using( var fs = new FileStream(geoJsonFile,FileMode.Open)) {
                    geoJson = JsonSerializer.Deserialize<GeoJson<SubstationProps>>(fs);
                    Logger.Instance.LogInfoEvent($"Found [{geoJson.features.Length}] features");                    
                }
                foreach( var feature in geoJson.features) {
                    if ( feature.properties.SUBSTATION == null ) {
                        Logger.Instance.LogInfoEvent("SUBSTATION is null");
                        continue;
                    }
                    var gs=da.NationalGrid.GetGridSubstation(feature.properties.SUBSTATION);
                    if ( gs==null) {
                        gs = GridSubstation.Create(feature.properties.SUBSTATION,GridSubstationSource.NGET);
                        da.NationalGrid.Add(gs);
                    }
                    //
                    gs.Name = feature.properties.Substation;
                    gs.Voltage = feature.properties.OPERATING_;
                    //
                    var locCode = feature.properties.SUBSTATION.Substring(0,4);
                    var loc = da.NationalGrid.GetGridSubstationLocation(locCode);
                    if ( loc==null ) {
                        loc = GridSubstationLocation.Create(locCode, GridSubstationLocationSource.NGET, null); 
                        da.NationalGrid.Add(loc);                       
                    }
                    //
                    var m = _nameRegex.Match(feature.properties.Substation);
                    if ( m.Success) {
                        var locName = m.Groups[1].Value;
                        loc.Name = locName;
                    } else {
                        Logger.Instance.LogWarningEvent($"Could not find match for name [{feature.properties.Substation}]");
                    }
                    //
                    try {
                        getLocation(feature.geometry,out double lat, out double lng);
                        loc.GISData.Latitude = lat;
                        loc.GISData.Longitude = lng;
                        gs.GISData.Latitude = lat;
                        gs.GISData.Longitude = lng;
                    } catch( Exception e) {
                        Logger.Instance.LogWarningEvent($"Exception raised for feature [{feature.properties.Substation}] [{e.Message}]");
                    }
                }

                // these are locations for the interconnectors
                foreach( var code in _nodeInterConnectors.Keys) {
                    var ic = _nodeInterConnectors[code];
                    var loc = da.NationalGrid.GetGridSubstationLocation(code);
                    if ( loc==null ) {
                        loc = GridSubstationLocation.Create(code, GridSubstationLocationSource.NGET, null); 
                        da.NationalGrid.Add(loc);                       
                    }
                    loc.Name = ic.Name;
                    loc.GISData.Latitude = ic.LatLng.Lat;
                    loc.GISData.Longitude = ic.LatLng.Lng;
                }
                
                // add new ones found
                da.CommitChanges();
            }

            //
        }

        private void getLocation(Geometry geometry, out double lat, out double lng) {
            if ( geometry.type == "Polygon") {
                var elements = geometry.coordinates.Deserialize<double[][][]>();
                lng = elements[0].Select(m=>m[0]).Average();
                lat = elements[0].Select(m=>m[1]).Average();
            } else if ( geometry.type == "MultiPolygon") {
                var elements = geometry.coordinates.Deserialize<double[][][][]>();
                lng = elements[0][0].Select(m=>m[0]).Average();
                lat = elements[0][0].Select(m=>m[1]).Average();
            } else {
                throw new Exception($"Unexpected geometry type found [{geometry.type}]");
            }
        }

        private void processNGETOhlGeoJson(string geoJsonFile) {

            var lineLoader = new GISLineLoader();

            using( var da = new DataAccess()) {

                GeoJson<OHLProps> geoJson;
                using( var fs = new FileStream(geoJsonFile,FileMode.Open)) {
                    geoJson = JsonSerializer.Deserialize<GeoJson<OHLProps>>(fs);
                    Logger.Instance.LogInfoEvent($"Found [{geoJson.features.Length}] features");                    
                }
                // 
                var features = geoJson.features.Where( m=>m.properties.CIRCUIT1!=null && m.properties.CIRCUIT2!=null);

                foreach( var feature in features) {
                    var ohl=da.NationalGrid.GetGridOverheadline(feature.properties.GDO_GID.ToString());
                    if ( ohl==null) {
                        ohl = GridOverheadLine.Create(feature.properties.GDO_GID.ToString());
                        da.NationalGrid.Add(ohl);
                    }
                    //
                    ohl.Circuit1 = feature.properties.CIRCUIT1;
                    ohl.Circuit2 = feature.properties.CIRCUIT2;
                    //
                    lineLoader.AddGISData(ohl.GISData,feature.geometry);
                }
                
                // add new ones found
                da.CommitChanges();
            }

            //
            lineLoader.Load();
            
        }

        private class GISLineLoader {
            private Dictionary<int,Geometry> _geoShapeDict = new Dictionary<int, Geometry>();

            public GISLineLoader() {
            }
            public void AddGISData( GISData data, Geometry geoShape) {
                if ( _geoShapeDict.ContainsKey(data.Id)) {
                    Logger.Instance.LogWarningEvent($"Duplicate key found in feature dict for gidDataId=[{data.Id}]");
                } else {
                    _geoShapeDict.Add(data.Id,geoShape);
                }
            }

            public void Load() {
                using( var da = new DataAccess() ) {
                    var lineDict = da.GIS.GetLineDict(_geoShapeDict.Keys.ToArray());

                    var linesToAdd = new List<GISLine>();
                    var linesToDelete = new List<GISLine>();

                    foreach( var k in _geoShapeDict.Keys) {
                        var gisData = da.GIS.Get<GISData>(k);
                        if ( gisData!=null ) {
                            var geoShape = _geoShapeDict[k];
                            IList<GISLine> lines;
                            if ( lineDict.ContainsKey(k)) {
                                lines = lineDict[k];
                            } else {
                                lines = new List<GISLine>();
                            }
                            if ( lines!=null) {
                                updateLinePoints(gisData,geoShape,lines, linesToAdd, linesToDelete);
                            }
                        }
                    }
                    // add boundaries
                    foreach( var line in linesToAdd) {
                       da.GIS.Add(line);
                    }
                    // remove boundaries
                    foreach( var line in linesToDelete) {
                       da.GIS.Delete(line);
                    }
                    //
                    da.CommitChanges();
                }
                //
                //??sw.Stop();
                //
                //??Logger.Instance.LogInfoEvent($"Boundary load for [{_featureDict.Keys.Count}] features done in {sw.Elapsed}s");
            }

            private void updateLinePoints(GISData gisData, Geometry geometry, 
                        IList<GISLine> lines, 
                        IList<GISLine> linesToAdd,
                        IList<GISLine> linesToDelete) {                    
                if ( geometry.type=="MultiLineString" ) {
                    var lineCoords = geometry.coordinates.Deserialize<double[][][]>();
                    int numLines = lineCoords.Length;
                    gisData.AdjustLineLists(numLines,lines,linesToAdd,linesToDelete);
                    for( int i=0;i<numLines;i++) {
                        int length = lineCoords[i].Length;
                        lines[i].Latitudes = new double[length];
                        lines[i].Longitudes = new double[length];
                        int index=0;
                        foreach( var coord in lineCoords[i] ) {
                            lines[i].Longitudes[index] = coord[0];
                            lines[i].Latitudes[index] = coord[1];
                            index++;
                        }
                    }
                } else if ( geometry.type=="LineString") {
                    var lineCoords = geometry.coordinates.Deserialize<double[][]>();
                    int length = lineCoords.Length;
                    gisData.AdjustLineLists(1,lines,linesToAdd,linesToDelete);
                    var line = lines[0];
                    line.Latitudes = new double[length];
                    line.Longitudes = new double[length];
                    int index=0;
                    foreach( var coord in lineCoords ) {
                        line.Longitudes[index] = coord[0];
                        line.Latitudes[index] = coord[1];
                        index++;
                    }
                } else {
                    throw new Exception($"Unexpected geometry type=[{geometry.type}]");
                }
            }
        }

        private Feature<SubstationProps> getFeature(Node node, Feature<SubstationProps>[] features) {
            foreach( var feature in features) {
                if ( node.Code.StartsWith(feature.properties.SUBSTATION)) {
                    return feature;
                }
            }
            return null;
        }


        public class GeoJson<T> {
            public string type {get; set;}
            public string name {get; set;}
            public Feature<T>[] features {get; set;}
        }

        public class Feature<T> {
            public int id {get; set;}
            public T properties {get; set;}

            public Geometry geometry { get; set; }
        }

        public class SubstationProps {
            public string SUBSTATION {get; set;}
            public string OPERATING_ {get; set;}
            public string ACTION_DTT {get; set;}
            public string STATUS {get; set;}
            public string Substation {get; set;}
            public string OWNER_FLAG {get; set;}
        }

        public class SPSubstationProps {
            public string PRIM_VOLT {get; set;}
            public string SPNAME {get; set;}
            public string STATUS {get; set;}
        }

        public class OHLProps {
             public double GDO_GID {get; set;}
             public string ROUTE_ASSE {get; set;}
             public string Towers_In {get; set;}
             public string ACTION_DTT {get; set;} 
             public string status {get; set;}
             public string OPERATING {get; set;}
             public string CIRCUIT1 {get; set;}
             public string CIRCUIT2 {get; set;} 

        }

        public class Geometry {
            public string type {get; set;}
            public JsonElement coordinates {get; set;}
        }

    }
}