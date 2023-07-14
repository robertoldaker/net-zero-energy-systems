using System.Diagnostics;
using System.Text;
using System.Text.Json;
using HaloSoft.EventLogger;
using SmartEnergyLabDataApi.Data;
using SmartEnergyLabDataApi.Model;

namespace SmartEnergyLabDataApi.Models
{
    public class NationalGridDistributionLoader {
        private string BASE_ADDRESS = "https://connecteddata.nationalgrid.co.uk";
        private const string PACKAGE_NAME = "spatial-datasets";
        private readonly string[] DATASET_NAMES = {            
            "East Midlands GSP",
            "West Midlands GSP",            
            "South Wales GSP",
            "South West GSP",
            
            /*
            "East Midlands BSP",
            "South Wales BSP",
            "South West BSP",
            "West Midlands BSP",
            */
            
            "East Midlands Primary",                       
            "West Midlands Primary",
            "South Wales Primary",
            "South West Primary", 
                
            "East Midlands Distribution", 
            "West Midlands Distribution",
            "South Wales Distribution",
            "South West Distribution",         
            
            
            };
        private HttpClient _httpClientGpkg;
        private static object _httpClientGpkgLock = new object();
        private TaskRunner? _taskRunner;
        private CKANDataLoader.CKANDataset _spd;

        public NationalGridDistributionLoader(TaskRunner? taskRunner) {
            _taskRunner = taskRunner;
        }
        public string Load() {
            var message = "";
            var ckanLoader = new CKANDataLoader(BASE_ADDRESS,PACKAGE_NAME);
            //
            var cLoader = new ConditionalDataLoader();
            int done=0;
            foreach ( var name in DATASET_NAMES) {
                done++;
                var percent = (100*done)/DATASET_NAMES.Length;
                _spd = ckanLoader.GetDatasetInfo(name);
                if ( _spd!=null ) {
                    message += cLoader.Load(_spd, ()=> {
                        var msg = processGpkg();
                        _taskRunner?.Update(percent);
                        Logger.Instance.LogInfoEvent(msg);
                        return msg;
                    });
                    message+="\n";
                }
                checkCancelled();
            }
            //
            return message;
        }

        private void updateMessage(string msg, bool addToLog=true) {
            _taskRunner?.Update(msg,addToLog);
        }

        private void checkCancelled() {
            _taskRunner?.CheckCancelled();
        }

        private string processGpkg() {

            updateMessage($"Downloading [{_spd.name}] geopackage ...");
            var client = getHttpClientGpkg();

            //??var gPkgFile = AppFolders.Instance.GetTempFile(".gpkg");
            var gPkgFile = Path.Combine(AppFolders.Instance.Temp,getPackageFile());
            var geoJsonFile = gPkgFile.Replace(".gpkg",".geojson");

            // Download json file unless we are developing
            if ( AppEnvironment.Instance.Context != Context.Development || !File.Exists(geoJsonFile) ) {
                using (HttpRequestMessage message = getRequestMessage(HttpMethod.Get, _spd.url)) {
                    //
                    var response = client.SendAsync(message).Result;
                    //
                    if ( response.IsSuccessStatusCode) {
                        var stream = response.Content.ReadAsStream();
                        if ( stream!=null) {
                            saveToFile(stream, gPkgFile);
                            checkCancelled();
                            convertToGeoJson(gPkgFile,geoJsonFile);
                        }
                    }
                }

            }
            checkCancelled();
            return processJson(geoJsonFile);

        }

        private string getPackageFile() {
            return _spd.name + ".gpkg";
        }

        private string processJson(string geoJsonFile) {
            updateMessage($"Processing [{_spd.name}] geoJson ...");
            if ( _spd.name.EndsWith("GSP") ) {
                return loadGSPs(geoJsonFile);
            } else if ( _spd.name.EndsWith("BSP")) {
                return loadBSPs(geoJsonFile);
            } else if ( _spd.name.EndsWith("Primary")) {
                return loadPrimaries(geoJsonFile);
            } else if ( _spd.name.EndsWith("Distribution")) {
                return loadDistributions(geoJsonFile);
            } else {
                throw new Exception($"Unexpected SpatialDataset name found [{_spd.name}]");
            }
        }

        private DNOAreas GetDNOArea(CKANDataLoader.CKANDataset ds) {
            if ( ds.name.StartsWith("East Midlands")) {
                return DNOAreas.EastMidlands;
            } else if ( ds.name.StartsWith("West Midlands")) {
                return DNOAreas.WestMidlands;
            } else if ( ds.name.StartsWith("South Wales")) {
                return DNOAreas.SouthWales;
            } else if ( ds.name.StartsWith("South West")) {
                return DNOAreas.SouthWestEngland;
            } else {
                throw new Exception($"Unexpected SpatialDataset name [{ds.name}]");
            }
        }


        private string loadGSPs(string geoJsonFile) {

            string msg = "";
            var boundaryLoader = new GISBoundaryLoader(this);
            int numNew = 0;
            int numModified = 0;
            int numIgnored = 0;
            string gaName = "";

            using( var da = new DataAccess()) {

                var dnoArea = GetDNOArea(_spd);
                var ga = da.Organisations.GetGeographicalArea(dnoArea);
                if ( ga==null) {
                    throw new Exception($"Could not find Geographical area for area code=[{dnoArea}]");
                }
                gaName = ga.Name;

                var addedGSPs = new List<GridSupplyPoint>();
                bool isNew = false;

                using( var fs = new FileStream(geoJsonFile,FileMode.Open)) {
                    var geoJson = JsonSerializer.Deserialize<GeoJson>(fs);
                    var geoName = geoJson.name;
                    if ( geoName==null || !geoName.Contains("GSP")) {
                        throw new Exception("Name of geojson file needs to contain \"GSP\"");
                    }
                    int nSupplyPoints = geoJson.features.Length;
                    foreach( var feature in geoJson.features) {                    
                        string nrId = feature.properties.GSP_NRID.ToString();
                        string name = feature.properties.GSP_NRID_NAME;
                        // Ignore if name is empty (West midlands has this)
                        if (string.IsNullOrEmpty(name)) {
                            numIgnored++;
                            continue;
                        }
                        var gsp = da.SupplyPoints.GetGridSupplyPoint(ImportSource.NationalGridDistributionOpenData,null,nrId,name);
                        if ( gsp==null ) {
                            gsp = new GridSupplyPoint(ImportSource.NationalGridDistributionOpenData,name,null, nrId,ga,ga.DistributionNetworkOperator);
                            Logger.Instance.LogInfoEvent($"Added new GSP [{name}] nrId=[{nrId}]");
                            addedGSPs.Add(gsp);
                            isNew = true;
                            numNew++;
                        } else {
                            if ( updateGSP(gsp,feature.properties)) {
                                Logger.Instance.LogInfoEvent($"Modifiing existing GSP [{gsp.Name}] nrId=[{gsp.NRId}] nr=[{gsp.NR}]");
                                numModified++;
                            }
                        }
                        //
                        if ( gsp.GISData==null) {
                            gsp.GISData = new GISData();
                        }
                        //
                        //
                        var elements = feature.geometry.coordinates.Deserialize<double[][][][]>();
                        // If currently apears in a different geographical area then only update points
                        if ( !isNew && gsp.GeographicalArea.Id!=ga.Id ) {
                            // work out polygon that is longest
                            int maxLength = 0;
                            for(int i=0;i<elements.Length;i++) {
                                if ( elements[i][0].Length>maxLength) {
                                    maxLength = elements[i][0].Length;
                                }
                            }
                            int boundaryLength = gsp.GetMaxBoundaryLength(da);
                            // Border GSPs are often mentioned in both geographical areas so choose the one with most points
                            // - a bit esoteric but not sure of a better way of doing just now
                            if (  maxLength > boundaryLength ) {
                                Logger.Instance.LogInfoEvent($"{gsp.Name}, moving from [{gsp.GeographicalArea.Name}] to [{ga.Name}], lengths=[{maxLength}/{boundaryLength}] ");
                                gsp.GeographicalArea = ga;
                                gsp.DistributionNetworkOperator = ga.DistributionNetworkOperator;
                                boundaryLoader.AddGISData(gsp.GISData,feature);
                            }
                        } else {
                            boundaryLoader.AddGISData(gsp.GISData,feature);
                        }

                    }                
                    //
                    checkCancelled();
                }

                // add new ones found
                foreach( var gsp in addedGSPs) {
                    da.SupplyPoints.Add(gsp);
                    checkCancelled();
                }
                //
                da.CommitChanges();
            }

            //
            boundaryLoader.Load();
            //
            msg = $"{gaName} area, [{numNew}] GSPs added, [{numModified}] modified, [{numIgnored}] ignored";
            return msg;
        }

        private bool updateGSP(GridSupplyPoint gsp, Props props) {
            bool updated = false;
            if ( gsp.Name!=props.GSP_NRID_NAME) {
                gsp.Name = props.GSP_NRID_NAME;
                updated=true;
            } 
            return updated;
        }

        private string loadBSPs(string geoJsonFile) {
            return "BSPs not implemented";
        }
        private string loadPrimaries(string geoJsonFile) {
            int numNew = 0;
            int numModified = 0;
            int numIgnored=0;
            string name;
            //
            var boundaryLoader = new GISBoundaryLoader(this);
            //
            using( var da = new DataAccess() ) {
                var ga = da.Organisations.GetGeographicalArea(GetDNOArea(_spd));
                name = ga.Name;
                var dno = ga.DistributionNetworkOperator;

                //
                using (var stream = new FileStream(geoJsonFile,FileMode.Open)) {
                    var geoJson = JsonSerializer.Deserialize<GeoJson>(stream);
                    int nPrimaries = geoJson.features.Length;
                    foreach( var feature in geoJson.features) {
                        var pss = da.Substations.GetPrimarySubstation(ImportSource.NationalGridDistributionOpenData,null,
                                        feature.properties.PRIM_NRID.ToString(),
                                        feature.properties.PRIM_NRID_NAME);
                        var gsp =  da.SupplyPoints.GetGridSupplyPoint(ImportSource.NationalGridDistributionOpenData,null,feature.properties.GSP_NRID.ToString());
                        if ( gsp==null ) {
                            Logger.Instance.LogErrorEvent($"Could not find GSP with GSP_NRID=[{feature.properties.GSP_NRID}] [{feature.properties.GSP_NRID_NAME}]");
                            numIgnored++;
                            continue;
                        }
                        if ( pss==null ) {

                            pss = new PrimarySubstation(ImportSource.NationalGridDistributionOpenData,null,feature.properties.PRIM_NRID.ToString(), gsp);
                            da.Substations.Add(pss);
                            numNew++;
                        } else {
                            pss.GridSupplyPoint = gsp;
                            numModified++;
                        }
                        //
                        pss.Name = feature.properties.PRIM_NRID_NAME;
                        if ( string.IsNullOrEmpty(pss.ExternalId2) && feature.properties.PRIM_NRID!=0) {
                            pss.ExternalId2 = feature.properties.PRIM_NRID.ToString();
                        }
                        if ( pss.GISData==null) {
                            pss.GISData = new GISData(pss);
                        }
                        //
                        boundaryLoader.AddGISData(pss.GISData,feature);
                        //
                        checkCancelled();
                    }
                    //
                    checkCancelled();
                    da.CommitChanges();
                }
            }
            //
            boundaryLoader.Load();
            var msg=$"{name} area, [{numNew}] primary substations added, [{numModified}] modified, [{numIgnored}] ignored";
            return msg;
        }

        private string loadDistributions(string geoJsonFile) {
            var loader = new DistributionLoader(this,_spd,geoJsonFile);
            return loader.Load();
        }

        private class GISBoundaryLoader {
            private Dictionary<int,Feature> _featureDict = new Dictionary<int, Feature>();

            private NationalGridDistributionLoader _parentLoader;

            public GISBoundaryLoader(NationalGridDistributionLoader parentLoader) {
                _parentLoader = parentLoader;
            }
            public void AddGISData( GISData data, Feature feature) {
                if ( _featureDict.ContainsKey(data.Id)) {
                    Logger.Instance.LogWarningEvent($"Duplicate key found in feature dict for gidDataId=[{data.Id}]");
                } else {
                    _featureDict.Add(data.Id,feature);
                }
            }

            public void Load() {
                //??var sw = new Stopwatch();
                //??sw.Start();
                using( var da = new DataAccess() ) {
                    _parentLoader.checkCancelled();
                    var boundaryDict = da.GIS.GetBoundaryDict(_featureDict.Keys.ToArray());
                    _parentLoader.checkCancelled();

                    var boundariesToAdd = new List<GISBoundary>();
                    var boundariesToDelete = new List<GISBoundary>();
                    foreach( var k in _featureDict.Keys) {
                        var gisData = da.GIS.Get<GISData>(k);
                        if ( gisData!=null ) {
                            var feature = _featureDict[k];
                            IList<GISBoundary> boundaries;
                            if ( boundaryDict.ContainsKey(k)) {
                                boundaries = boundaryDict[k];
                            } else {
                                boundaries = new List<GISBoundary>();
                            }
                            var elements = feature.geometry.coordinates.Deserialize<double[][][][]>();
                            if ( elements!=null && boundaries!=null) {
                                gisData.UpdateBoundaryPoints(elements,boundaries, boundariesToAdd, boundariesToDelete);
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

        private class DistributionLoader {
            private NationalGridDistributionLoader _loader;
            private string _geoJsonFile;
            private CKANDataLoader.CKANDataset _spd;
            private Feature[] _features;
            private int _numNew = 0;
            private int _numModified = 0;
            private int _numIgnored=0;
            private Dictionary<int,bool> _processedDict = new Dictionary<int,bool>();
            public DistributionLoader(NationalGridDistributionLoader loader, CKANDataLoader.CKANDataset  spd, string geoJsonFile) {
                _loader = loader;
                _spd = spd;
                _geoJsonFile = geoJsonFile;
            }

            public string Load() {
                
                using (var stream = new FileStream(_geoJsonFile, FileMode.Open)) {
                    var geoJson = JsonSerializer.Deserialize<GeoJson>(stream);
                    _features = geoJson.features;
                }
                //
                var area = _loader.GetDNOArea(_spd);
                int bufferSize=100;
                int processed = 0;
                int percent = 0;
                int prevPercent=0;
                while ( processed<_features.Length) {
                    int start = processed;
                    int length = bufferSize;
                    if ( length+start>_features.Length) {
                        length = _features.Length-start;
                    }
                    var featureSpan = new ReadOnlySpan<Feature>(_features,start,length);
                    partialLoad(featureSpan);
                    processed += length;
                    percent = processed*100/_features.Length;
                    if ( percent!=prevPercent) {
                        _loader.updateMessage($"Processing [{_spd.name}] {percent}%",false);
                        prevPercent = percent;
                    }
                }
                var msg=$"{area} area, [{_numNew}] distribution substations added, [{_numModified}] modified, [{_numIgnored}] ignored";
                return msg;
            }


            private void partialLoad( ReadOnlySpan<Feature> featureSpan) {  
                int prevNrId=0;

                var boundaryLoader = new GISBoundaryLoader(_loader);
                // Needed to speed up searching for existing distribution substations
                // without this speed taken to call GetDistributionSubstations get slower and slower as we progress through the data
                using( var daRead = new DataAccess() ) {


                    using( var da = new DataAccess() ) {

                        var ga = da.Organisations.GetGeographicalArea(_loader.GetDNOArea(_spd));
                        var dno = ga.DistributionNetworkOperator;
                        var toAdd = new List<DistributionSubstation>();
                        var primCache = new Dictionary<int,PrimarySubstation>();
                        prevNrId=0;


                        foreach( var feature in featureSpan) {
                            // Various entries are repeated so only process it if the NRId changes
                            if ( prevNrId==feature.properties.NRID) {
                                continue;
                            }
                            prevNrId = feature.properties.NRID;
                            //
                            var nr = feature.properties.NR.ToString();
                            var nrId = feature.properties.NRID.ToString();
                            var name = feature.properties.NAME;

                            DistributionSubstation dss=null;
                            var dssRead = daRead.Substations.GetDistributionSubstation(ImportSource.NationalGridDistributionOpenData,nr,nrId,name);
                            if ( dssRead!=null) {
                                dss = da.Substations.GetDistributionSubstation(dssRead.Id);
                            }

                            PrimarySubstation pss = null;
                            // look in cache first
                            if ( !primCache.TryGetValue(feature.properties.PRIM_NRID, out pss)) {
                                pss = da.Substations.GetPrimarySubstation(ImportSource.NationalGridDistributionOpenData,
                                    null,
                                    feature.properties.PRIM_NRID.ToString(), 
                                    feature.properties.PRIM_NRID_NAME);
                                if ( pss!=null ) {
                                    primCache.Add(feature.properties.PRIM_NRID,pss);
                                }
                            }
                            //
                            if ( pss==null ) {
                                Logger.Instance.LogErrorEvent($"Could not find Primary substation with PRIM_NRID=[{feature.properties.PRIM_NRID}] PRIM_NRID_NAME=[{feature.properties.PRIM_NRID_NAME}]");
                                _numIgnored++;
                                continue;
                            }
                            if ( dss==null ) {                        
                                dss = new DistributionSubstation(ImportSource.NationalGridDistributionOpenData,nr,nrId,pss);
                                toAdd.Add(dss);
                                _numNew++;
                            } else {
                                dss.PrimarySubstation = pss;
                                _numModified++;
                            }
                            //
                            if ( string.IsNullOrEmpty(dss.ExternalId) && !string.IsNullOrEmpty(nr)) {
                                dss.ExternalId = nr;
                            }
                            if ( string.IsNullOrEmpty(dss.ExternalId2) && !string.IsNullOrEmpty(nrId)) {
                                dss.ExternalId2 = nrId;
                            }
                            dss.Name = feature.properties.NAME;
                            // boundary
                            boundaryLoader.AddGISData(dss.GISData,feature);
                            _loader.checkCancelled();

                        }

                        // Add new ones to db
                        foreach( var dss in toAdd) {
                            da.Substations.Add(dss);
                            _loader.checkCancelled();
                        }

                        da.CommitChanges();
                    }

                }

                // load boundaries
                boundaryLoader.Load();

            }
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

        private void convertToGeoJson(string gPkgFile, string geoJsonFile) {
            updateMessage($"Converting [{_spd.name}] to geoJson ...");

            var processStartInfo = new ProcessStartInfo();
            processStartInfo.CreateNoWindow = true;
            processStartInfo.UseShellExecute = false;
            processStartInfo.Arguments = $"-f GeoJSON \"{geoJsonFile}\" \"{gPkgFile}\"";
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


        private HttpClient getHttpClientGpkg()
        {
            if (_httpClientGpkg == null) {
                lock (_httpClientGpkgLock) {
                    _httpClientGpkg = new HttpClient();
                }
            }
            //
            return _httpClientGpkg;
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


        private enum DataType { GSP, BSP, Primary, Distribution }


        public class GeoJson {
            public string type {get; set;}
            public string name {get; set;}
            public Feature[] features {get; set;}
        }

        public class Feature {
            public int id {get; set;}
            public Props properties {get; set;}

            public Geometry geometry { get; set; }
        }

        public class Props {
            public int NRID {get; set;}
            public string NR {get; set;}
            public int NR_TYPE_ID {get; set;}
            public string NAME {get; set;}
            public int PRIM_NRID {get; set;}
            public string PRIM_NRID_NAME {get; set;}
            public int BSP_NRID {get; set;}
            public string BSP_NRID_NAME {get; set;}
            public int GSP_NRID {get; set;}
            public string GSP_NRID_NAME {get; set;}
        }

        public class Geometry {
            public string type {get; set;}
            public JsonElement coordinates {get; set;}
        }
    }
}