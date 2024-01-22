using System.Diagnostics;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using CommonInterfaces.Models;
using HaloSoft.EventLogger;
using SmartEnergyLabDataApi.Data;

namespace SmartEnergyLabDataApi.Models
{
    public class NationalGridNetworkLoader {

        private const string ALL_DATA_URL = "https://www.nationalgrid.com/electricity-transmission/document/81201/download";

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
            { "SELLX", new InterConnector("IFA1", new LatLng() {Lat=50.755175, Lng= 1.63329}) },
            { "CHILX", new InterConnector("IFA2", new LatLng() {Lat=49.84289, Lng= 0.84227}) },
            { "RICHX", new InterConnector("NEMO", new LatLng() {Lat=51.142778, Lng= 2.86376}) },
            { "GRAIX", new InterConnector("BritNed", new LatLng() {Lat=51.527152, Lng= 3.56688}) }, 
            { "CONQX", new InterConnector("EWLink", new LatLng() {Lat=53.714219, Lng=-6.21094}) },  
            { "BLYTX", new InterConnector("NorNed", new LatLng() {Lat=58.35708, Lng=6.92481}) },  
        };


        public void Load() {

            var client = getHttpClient();
            string substationsGeoJsonFile=null;
            string ohlGeoJsonFile = null;

            // Download json file unless we are developing
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
                processSubstationsGeoJson(substationsGeoJsonFile);
            }
            if ( ohlGeoJsonFile!=null) {
                processOhlGeoJson(ohlGeoJsonFile);
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

        private void extractZip(string folder, string fn, string outFolder) {
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

        private void processSubstationsGeoJson(string geoJsonFile) {
            using( var da = new DataAccess()) {

                GeoJson<SubstationProps> geoJson;
                using( var fs = new FileStream(geoJsonFile,FileMode.Open)) {
                    geoJson = JsonSerializer.Deserialize<GeoJson<SubstationProps>>(fs);
                    Logger.Instance.LogInfoEvent($"Found [{geoJson.features.Length}] features");                    
                }
                foreach( var feature in geoJson.features) {
                    var gs=da.NationalGrid.GetGridSubstation(feature.properties.SUBSTATION);
                    if ( gs==null) {
                        gs = GridSubstation.Create(feature.properties.SUBSTATION);
                        da.NationalGrid.Add(gs);
                    }
                    //
                    gs.Name = feature.properties.Substation;
                    gs.Voltage = feature.properties.OPERATING_;
                    //
                    var locCode = feature.properties.SUBSTATION.Substring(0,4);
                    var loc = da.NationalGrid.GetGridSubstationLocation(locCode);
                    if ( loc==null ) {
                        loc = GridSubstationLocation.Create(locCode); 
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
                        loc = GridSubstationLocation.Create(code); 
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

        private void processOhlGeoJson(string geoJsonFile) {

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