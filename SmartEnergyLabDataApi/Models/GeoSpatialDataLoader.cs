using System.Diagnostics;
using System.Text;
using System.Text.Json;
using HaloSoft.EventLogger;
using SmartEnergyLabDataApi.Data;
using SmartEnergyLabDataApi.Model;

namespace SmartEnergyLabDataApi.Models
{
    public class GeoSpatialDataLoader {
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

        public GeoSpatialDataLoader(TaskRunner? taskRunner) {
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

            }
            //
            return message;
        }

        private void updateMessage(string msg) {
            _taskRunner?.Update(msg);
        }

        private string processGpkg() {

            updateMessage($"Downloading [{_spd.name}] geopackage ...");
            var client = getHttpClientGpkg();

            var gPkgFile = AppFolders.Instance.GetTempFile(".gpkg");
            var geoJsonFile = gPkgFile.Replace(".gpkg",".geojson");
            try {
                //
                using (HttpRequestMessage message = getRequestMessage(HttpMethod.Get, _spd.url)) {
                    //
                    var response = client.SendAsync(message).Result;
                    //
                    if ( response.IsSuccessStatusCode) {
                        var stream = response.Content.ReadAsStream();
                        if ( stream!=null) {
                            saveToFile(stream, gPkgFile);
                            convertToGeoJson(gPkgFile,geoJsonFile);
                        }
                    }
                }
                geoJsonFile  = Path.Combine(AppFolders.Instance.Temp,"South West Distribution.geojson");
                //
                return processJson(geoJsonFile);

            } finally {
                if ( File.Exists(gPkgFile)) {
                    File.Delete(gPkgFile);
                }
                if ( File.Exists(geoJsonFile)) {
                    File.Delete(geoJsonFile);
                }
            }
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
            using( var da = new DataAccess()) {

                var dnoArea = GetDNOArea(_spd);
                var ga = da.Organisations.GetGeographicalArea(dnoArea);
                if ( ga==null) {
                    throw new Exception($"Could not find Geographical area for area code=[{dnoArea}]");
                }

                var addedGSPs = new List<GridSupplyPoint>();

                using( var fs = new FileStream(geoJsonFile,FileMode.Open)) {
                    var geoJson = JsonSerializer.Deserialize<GeoJson>(fs);
                    var geoName = geoJson.name;
                    int numNew = 0;
                    int numModified = 0;
                    int numIgnored = 0;
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
                        var gsp = da.SupplyPoints.GetGridSupplyPointByNrIdOrName(nrId,name);
                        if ( gsp==null ) {
                            gsp = new GridSupplyPoint(name,"", nrId,ga,ga.DistributionNetworkOperator);
                            Logger.Instance.LogInfoEvent($"Added new GSP [{name}] nrId=[{nrId}]");
                            addedGSPs.Add(gsp);
                            numNew++;
                        } else {
                            if ( updateGSP(gsp,feature.properties)) {
                                Logger.Instance.LogInfoEvent($"Modifiing existing GSP [{gsp.Name}] nrId=[{gsp.NRId}] nr=[{gsp.NR}]");
                                numModified++;
                            }
                        }
                        var elements = feature.geometry.coordinates.Deserialize<double[][][][]>();
                        int maxIndex=0;
                        int maxLength = 0;
                        for(int i=0;i<elements.Length;i++) {
                            if ( elements[i][0].Length>maxLength) {
                                maxIndex=i;
                                maxLength = elements[i][0].Length;
                            }
                        }
                        var length = elements[maxIndex][0].Length;
                        if ( gsp.GISData==null) {
                            gsp.GISData = new GISData();
                        }
                        gsp.GISData.BoundaryLatitudes = new double[length];
                        gsp.GISData.BoundaryLongitudes = new double[length];
                        for(int index=0; index<length; index++) {
                            var eastings = elements[maxIndex][0][index][0];
                            var northings = elements[maxIndex][0][index][1];
                            var latLong=LatLonConversions.ConvertOSToLatLon(eastings,northings);
                            gsp.GISData.BoundaryLatitudes[index] = latLong.Latitude;
                            gsp.GISData.BoundaryLongitudes[index] = latLong.Longitude;
                        }
                        //
                        if ( gsp.GISData.BoundaryLatitudes.Length!=0 ) {
                            gsp.GISData.Latitude = gsp.GISData.BoundaryLatitudes.Sum()/gsp.GISData.BoundaryLatitudes.Length;
                        }
                        if ( gsp.GISData.BoundaryLongitudes.Length!=0 ) {
                            gsp.GISData.Longitude = gsp.GISData.BoundaryLongitudes.Sum()/gsp.GISData.BoundaryLongitudes.Length;
                        }
                        //
                    }                
                    msg = $"{ga.Name} area, [{numNew}] GSPs added, [{numModified}] modified, [{numIgnored}] ignored";
                }

                // add new ones found
                foreach( var gsp in addedGSPs) {
                    da.SupplyPoints.Add(gsp);
                }
                //
                da.CommitChanges();
            }
            return msg;
        }

        private bool updateGSP(GridSupplyPoint gsp, Props props) {
            bool updated = false;
            if ( gsp.Name!=props.GSP_NRID_NAME) {
                gsp.Name = props.GSP_NRID_NAME;
                updated=true;
            } 
            if ( gsp.NRId!=props.GSP_NRID.ToString()) {
                gsp.NRId = props.GSP_NRID.ToString();
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
            //
            using( var da = new DataAccess() ) {
                var ga = da.Organisations.GetGeographicalArea(GetDNOArea(_spd));
                var dno = ga.DistributionNetworkOperator;
                var primaryCache = new PrimaryCache(da, dno);

                //
                using (var stream = new FileStream(geoJsonFile,FileMode.Open)) {
                    var geoJson = JsonSerializer.Deserialize<GeoJson>(stream);
                    int nPrimaries = geoJson.features.Length;
                    foreach( var feature in geoJson.features) {
                        var pss = primaryCache.Get(feature.properties.PRIM_NRID.ToString(),feature.properties.PRIM_NRID_NAME);
                        var gsp =  da.SupplyPoints.GetGridSupplyPointByNrIdOrName(feature.properties.GSP_NRID.ToString(),feature.properties.GSP_NRID_NAME);
                        if ( gsp==null ) {
                            Logger.Instance.LogErrorEvent($"Could not find GSP with GSP_NRID=[{feature.properties.GSP_NRID}] [{feature.properties.GSP_NRID_NAME}]");
                            numIgnored++;
                            continue;
                        }
                        if ( pss==null ) {                        
                            pss = new PrimarySubstation(null,feature.properties.PRIM_NRID.ToString(), gsp);
                            da.Substations.Add(pss);
                            numNew++;
                        } else {
                            pss.GridSupplyPoint = gsp;
                            numModified++;
                        }
                        //
                        pss.Name = feature.properties.PRIM_NRID_NAME;
                        if ( string.IsNullOrEmpty(pss.NRId) && feature.properties.PRIM_NRID!=0) {
                            pss.NRId = feature.properties.PRIM_NRID.ToString();
                        }
                        if ( pss.GISData==null) {
                            pss.GISData = new GISData(pss);
                        }
                        //
                        var elements = feature.geometry.coordinates.Deserialize<double[][][][]>();
                        int maxIndex=0;
                        int maxLength = 0;
                        for(int i=0;i<elements.Length;i++) {
                            if ( elements[i][0].Length>maxLength) {
                                maxIndex=i;
                                maxLength = elements[i][0].Length;
                            }
                        }
                        var length = elements[maxIndex][0].Length;
                        pss.GISData.BoundaryLatitudes = new double[length];
                        pss.GISData.BoundaryLongitudes = new double[length];
                        for(int index=0; index<length; index++) {
                            var eastings = elements[maxIndex][0][index][0];
                            var northings = elements[maxIndex][0][index][1];
                            var latLong=LatLonConversions.ConvertOSToLatLon(eastings,northings);
                            pss.GISData.BoundaryLatitudes[index] = latLong.Latitude;
                            pss.GISData.BoundaryLongitudes[index] = latLong.Longitude;
                        }
                        if ( pss.GISData.BoundaryLatitudes.Length!=0 ) {
                            pss.GISData.Latitude = (pss.GISData.BoundaryLatitudes.Max()+pss.GISData.BoundaryLatitudes.Min())/2;
                        }
                        if ( pss.GISData.BoundaryLongitudes.Length!=0 ) {
                            pss.GISData.Longitude = (pss.GISData.BoundaryLongitudes.Max()+pss.GISData.BoundaryLongitudes.Min())/2;
                        }
                    }
                    //
                    da.CommitChanges();
                }

                var msg=$"{ga.Name} area, [{numNew}] primary substations added, [{numModified}] modified, [{numIgnored}] ignored";
                return msg;
            }
        }

        private class PrimaryCache {
            private Dictionary<string,PrimarySubstation> _byNrId;
            private Dictionary<string,PrimarySubstation> _byName;

            public PrimaryCache(DataAccess da, DistributionNetworkOperator dno) {
                _byNrId = new Dictionary<string, PrimarySubstation>();
                _byName = new Dictionary<string, PrimarySubstation>();
                var psss = da.Substations.GetPrimarySubstations(dno);
                foreach( var pss in psss) {
                    if ( !string.IsNullOrEmpty(pss.NRId)) {
                        if ( _byNrId.ContainsKey(pss.NRId)) {
                            Logger.Instance.LogErrorEvent($"More than one Primary substation has NRId [{pss.NRId}]");
                        } else {
                            _byNrId.Add(pss.NRId,pss);
                        }
                    }                    
                    if ( !string.IsNullOrEmpty(pss.Name)) {
                        if ( _byName.ContainsKey(pss.Name)) {
                            Logger.Instance.LogErrorEvent($"More than one Primary substation has name  [{pss.Name}]");
                        } else {
                            _byName.Add(pss.Name,pss);
                        }
                    }
                }
            }
            public PrimarySubstation Get(string nrId, string name ) {
                if ( !string.IsNullOrEmpty(nrId) ) {
                    if ( _byNrId.TryGetValue(nrId,out PrimarySubstation pss)) {
                        return pss;
                    }
                }
                if (!string.IsNullOrEmpty(name)) {
                    if ( _byName.TryGetValue(name, out PrimarySubstation pss)) {
                        return pss;
                    }
                }
                return null;
            }
        }

        private string loadDistributions(string geoJsonFile) {
            var loader = new DistributionLoader(this,_spd,geoJsonFile);
            return loader.Load();
        }

        private class DistributionLoader {
            private GeoSpatialDataLoader _loader;
            private string _geoJsonFile;
            private CKANDataLoader.CKANDataset _spd;
            private Feature[] _features;
            private int _numNew = 0;
            private int _numModified = 0;
            private int _numIgnored=0;
            private Dictionary<int,bool> _processedDict = new Dictionary<int,bool>();
            public DistributionLoader(GeoSpatialDataLoader loader, CKANDataLoader.CKANDataset  spd, string geoJsonFile) {
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
                int bufferSize=1000;
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
                    PartialLoad( featureSpan);
                    Logger.Instance.LogInfoEvent($"{area} area, [{_numNew}] distribution substations added, [{_numModified}] modified, [{_numIgnored}] ignored");
                    processed += length;
                    percent = processed*100/_features.Length;
                    if ( percent!=prevPercent) {
                        _loader.updateMessage($"Processing [{_spd.name}] {percent}%");
                        prevPercent = percent;
                    }
                }
                var msg=$"{area} area, [{_numNew}] distribution substations added, [{_numModified}] modified, [{_numIgnored}] ignored";
                return msg;
            }

            private void PartialLoad( ReadOnlySpan<Feature> features) {  
                int prevNrId=0;
                using( var da = new DataAccess() ) {
                    var ga = da.Organisations.GetGeographicalArea(_loader.GetDNOArea(_spd));
                    var dno = ga.DistributionNetworkOperator;
                    var toAdd = new List<DistributionSubstation>();

                    foreach( var feature in features) {
                        // Various entries are repeated so only process it the NRId changes
                        if ( prevNrId==feature.properties.NRID) {
                            continue;
                        }
                        prevNrId = feature.properties.NRID;
                        //
                        var nr = feature.properties.NR.ToString();
                        var nrId = feature.properties.NRID.ToString();
                        var name = feature.properties.NAME;

                        var dss = da.Substations.GetDistributionSubstationByNrOrName(nr,name);
                        var pss = da.Substations.GetPrimarySubstationByNrIdOrName(feature.properties.PRIM_NRID.ToString(), feature.properties.PRIM_NRID_NAME);
                        if ( pss==null ) {
                            Logger.Instance.LogErrorEvent($"Could not find Primary substation with PRIM_NRID=[{feature.properties.PRIM_NRID}] PRIM_NRID_NAME=[{feature.properties.PRIM_NRID_NAME}]");
                            _numIgnored++;
                            continue;
                        }
                        if ( dss==null ) {                        
                            dss = new DistributionSubstation(nr,pss);
                            dss.NRId = nrId;
                            toAdd.Add(dss);
                            _numNew++;
                        } else {
                            dss.PrimarySubstation = pss;
                            _numModified++;
                        }
                        //
                        if ( string.IsNullOrEmpty(dss.NR) && !string.IsNullOrEmpty(nr)) {
                            dss.NR = nr;
                        }
                        if ( string.IsNullOrEmpty(dss.NRId) && !string.IsNullOrEmpty(nrId)) {
                            dss.NRId = nrId;
                        }
                        dss.Name = feature.properties.NAME;
                        // location
                        /*var eastings = feature.properties.dp2_x;                    
                        var northings = feature.properties.dp2_y;
                        var latLong=LatLonConversions.ConvertOSToLatLon(eastings,northings);
                        dss.GISData.Latitude = latLong.Latitude;
                        dss.GISData.Longitude = latLong.Longitude;*/
                        // boundary
                        var elements = feature.geometry.coordinates.Deserialize<double[][][][]>();
                        int maxIndex=0;
                        int maxLength = 0;
                        for(int i=0;i<elements.Length;i++) {
                            if ( elements[i][0].Length>maxLength) {
                                maxIndex=i;
                                maxLength = elements[i][0].Length;
                            }
                        }
                        var length = elements[maxIndex][0].Length;
                        dss.GISData.BoundaryLatitudes = new double[length];
                        dss.GISData.BoundaryLongitudes = new double[length];
                        LatLon latLong;
                        for(int index=0; index<length; index++) {                            
                            latLong=LatLonConversions.ConvertOSToLatLon(elements[maxIndex][0][index][0],elements[maxIndex][0][index][1]);
                            dss.GISData.BoundaryLongitudes[index] = latLong.Longitude;
                            dss.GISData.BoundaryLatitudes[index] = latLong.Latitude;
                        }
                        if ( dss.GISData.BoundaryLatitudes.Length!=0 ) {
                            dss.GISData.Latitude = (dss.GISData.BoundaryLatitudes.Max()+dss.GISData.BoundaryLatitudes.Min())/2;
                        }
                        if ( dss.GISData.BoundaryLongitudes.Length!=0 ) {
                            dss.GISData.Longitude = (dss.GISData.BoundaryLongitudes.Max()+dss.GISData.BoundaryLongitudes.Min())/2;
                        }

                    }

                    // Add new ones to db
                    foreach( var dss in toAdd) {
                        da.Substations.Add(dss);
                    }

                    da.CommitChanges();

                }

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
            processStartInfo.FileName = "ogr2ogr";

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