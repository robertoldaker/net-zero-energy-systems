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
          /*  "East Midlands BSP",
            "South Wales BSP",
            "South West BSP",
            "West Midlands BSP",
            "East Midlands Primary",
            "West Midlands Primary",
            "South Wales Primary",
            "South West Primary",
            "East Midlands Distribution",
            "West Midlands Distribution",
            "South Wales Distribution",
            "South West Distribution",*/
            };
        private HttpClient _httpClientGpkg;
        private static object _httpClientGpkgLock = new object();
        private TaskRunner? _taskRunner;

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
                var spd = ckanLoader.GetDatasetInfo(name);
                if ( spd!=null ) {
                    message += cLoader.Load(spd, ()=> {
                        using ( var da = new DataAccess()) {
                            var msg = processGpkg(da, spd)+"\n";
                            _taskRunner?.Update($"Processing [{spd.name}] ...");
                            _taskRunner?.Update(percent);
                            da.CommitChanges();
                            return msg;
                        }
                    });
                }

            }
            //
            return message;
        }

        private string processGpkg( DataAccess da, CKANDataLoader.CKANDataset spd) {

            var client = getHttpClientGpkg();

            var gPkgFile = AppFolders.Instance.GetTempFile(".gpkg");
            var geoJsonFile = gPkgFile.Replace(".gpkg",".geojson");
            try {
                //
                using (HttpRequestMessage message = getRequestMessage(HttpMethod.Get, spd.url)) {
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

                //
                return processJson(da, spd, geoJsonFile);

            } finally {
                if ( File.Exists(gPkgFile)) {
                    File.Delete(gPkgFile);
                }
                if ( File.Exists(geoJsonFile)) {
                    File.Delete(geoJsonFile);
                }
            }
        }

        private string processJson(DataAccess da, CKANDataLoader.CKANDataset spd, string geoJsonFile) {
            if ( spd.name.EndsWith("GSP") ) {
                return loadGSPs(da, spd, geoJsonFile);
            } else if ( spd.name.EndsWith("BSP")) {
                return loadBSPs(da, spd, geoJsonFile);
            } else if ( spd.name.EndsWith("Primary")) {
                return loadPrimaries(da, spd, geoJsonFile);
            } else if ( spd.name.EndsWith("Distribution")) {
                //??return loadDistributions(da, spd,geoJsonFile);
                return "not impleented";
            } else {
                throw new Exception($"Unexpected SpatialDataset name found [{spd.name}]");
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


        private string loadGSPs(DataAccess da, CKANDataLoader.CKANDataset spd, string geoJsonFile) {

            var dnoArea = GetDNOArea(spd);
            var ga = da.Organisations.GetGeographicalArea(dnoArea);
            if ( ga==null) {
                throw new Exception($"Could not find Geographical area for area code=[{dnoArea}]");
            }
            Logger.Instance.LogInfoEvent($"Started reading GSPs from GeoSpatial dataset [{spd.name}]");

            string msg = "";

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
                msg = $"{ga.Name} area, [{numNew}] GSPs added, [{numModified}] GSPs modified, [{numIgnored} GSPs ignored]";
            }

            // add new ones found
            foreach( var gsp in addedGSPs) {
                da.SupplyPoints.Add(gsp);
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

        private string loadBSPs(DataAccess da, CKANDataLoader.CKANDataset spd, string geoJsonFile) {
            return "BSPs not implemented";
        }
        private string loadPrimaries(DataAccess da, CKANDataLoader.CKANDataset spd, string geoJsonFile) {
            string msg="";
            int numNew = 0;
            int numModified = 0;
            int numIgnored=0;
            //
            var ga = da.Organisations.GetGeographicalArea(GetDNOArea(spd));
            var dno = ga.DistributionNetworkOperator;
            var primarySubstations = da.Substations.GetPrimarySubstations(dno);
            //
            using (var stream = new FileStream(geoJsonFile,FileMode.Open)) {
                var geoJson = JsonSerializer.Deserialize<GeoJson>(stream);
                int nPrimaries = geoJson.features.Length;
                foreach( var feature in geoJson.features) {
                    string nr = feature.properties.NR.ToString();
                    var pss = primarySubstations.Where( m=>m.NR == nr ).FirstOrDefault();
                    var gsp =  da.SupplyPoints.GetGridSupplyPointByNRId(feature.properties.GSP_NRID.ToString());
                    if ( gsp==null ) {
                        msg+=$"Could not find GSP with GSP_NRID=[{feature.properties.GSP_NRID}]\n";
                        numIgnored++;
                        continue;
                    }
                    if ( pss==null ) {                        
                        pss = new PrimarySubstation(nr,feature.properties.PRIM_NRID.ToString(), gsp);
                        da.Substations.Add(pss);
                        numNew++;
                    } else {
                        pss.GridSupplyPoint = gsp;
                        numModified++;
                    }
                    //
                    pss.Name = feature.properties.NAME;
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
            }
            msg+=$"{ga.Name} area, [{numNew}] primary substations added, ";
            msg+=$"[{numModified}] primary substations modified";
            return msg;
        }
        private string loadDistributions(DataAccess da, CKANDataLoader.CKANDataset  spd, string geoJsonFile) {
            string msg="";
            var ga = da.Organisations.GetGeographicalArea(GetDNOArea(spd));
            int numNew = 0;
            int numModified = 0;
            int numIgnored=0;
            var toAdd = new List<DistributionSubstation>();
            using (var stream = new FileStream(geoJsonFile, FileMode.Open)) {
                var geoJson = JsonSerializer.Deserialize<GeoJson>(stream);
                int nDss = geoJson.features.Length;
                int nDone=0;
                Console.WriteLine($"Number of substations=[{nDss}]");
                foreach( var feature in geoJson.features) {
                    string nr = feature.properties.NR.ToString();
                    Console.WriteLine($"Processing {nDone++} of {nDss}, [{feature.properties.NAME}] ");
                    var dss = da.Substations.GetDistributionSubstation(nr);
                    var pss = da.Substations.GetPrimarySubstation(feature.properties.PRIM_NRID.ToString());
                    if ( pss==null ) {
                        msg+=$"Could not find Primary substation with PRIM_NRID=[{feature.properties.PRIM_NRID}]\n";
                        numIgnored++;
                        continue;
                    }
                    if ( dss==null ) {                        
                        dss = new DistributionSubstation(nr,pss);
                        toAdd.Add(dss);
                        numNew++;
                    } else {
                        dss.PrimarySubstation = pss;
                        numModified++;
                    }
                    //
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
                }

                // Add new ones to db
                foreach( var dss in toAdd) {
                    da.Substations.Add(dss);
                }
            }
            msg+=$"{ga.Name} area, [{numNew}] distribution substations added, ";
            msg+=$"[{numModified}] distibutions substations modified";
            return msg;
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