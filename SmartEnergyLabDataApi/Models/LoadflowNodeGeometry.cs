using System.Diagnostics;
using System.Text;
using System.Text.Json;
using HaloSoft.EventLogger;
using SmartEnergyLabDataApi.Data;

namespace SmartEnergyLabDataApi.Models
{
    public class LoadflowNodeGeometry {

        private const string ALL_DATA_URL = "https://www.nationalgrid.com/electricity-transmission/document/81201/download";

        private HttpClient _httpClient;
        private object _httpClientLock = new object();

        public void Load() {

            var client = getHttpClient();
            string geoJsonFile=null;

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
                        var shapeFile = Path.Combine(AppFolders.Instance.Temp,outFolder,"Substations.shp");
                        geoJsonFile = shapeFile.Replace(".shp",".geojson");
                        convertToGeoJson(shapeFile,geoJsonFile);
                    }
                }
            }
            if ( geoJsonFile!=null) {
                processJson(geoJsonFile);
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
            processStartInfo.Arguments = $"-f GeoJSON \"{geoJsonFile}\" \"{shapeFile}\"";
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

        private void processJson(string geoJsonFile) {
            using( var da = new DataAccess()) {

                GeoJson geoJson;
                using( var fs = new FileStream(geoJsonFile,FileMode.Open)) {
                    geoJson = JsonSerializer.Deserialize<GeoJson>(fs);
                    Logger.Instance.LogInfoEvent($"Found [{geoJson.features.Length}] features");
                }

                var branches = da.Loadflow.GetBranches();
                var nodes = da.Loadflow.GetNodes();
                var nodeDict = new Dictionary<Node,Feature?>();
                foreach( var b in branches) {
                    if ( !nodeDict.ContainsKey(b.Node1) ) {
                        var feature = getFeature(b.Node1, geoJson.features);
                        if ( feature!=null) {
                            Logger.Instance.LogInfoEvent($"Found feature for node [{b.Node1.Code}] [{feature.properties.SUBSTATION}]:[[{feature.properties.Substation}]");
                        } else {
                           Logger.Instance.LogInfoEvent($"Could not find feature for node [{b.Node1.Code}] [{b.Node1.Demand}:{b.Node1.Generation_A}:{b.Node1.Generation_B}]");
                        }
                        nodeDict.Add(b.Node1,feature);
                    }
                    if ( !nodeDict.ContainsKey(b.Node2) ) {
                        var feature = getFeature(b.Node2, geoJson.features);
                        if ( feature!=null) {
                            Logger.Instance.LogInfoEvent($"Found feature for node [{b.Node2.Code}] [{feature.properties.SUBSTATION}]:[[{feature.properties.Substation}]");
                        } else {
                           Logger.Instance.LogInfoEvent($"Could not find feature for node [{b.Node2.Code}]");
                        }
                        nodeDict.Add(b.Node2,feature);
                    }
                }

                Logger.Instance.LogInfoEvent($"NodeDict length=[{nodeDict.Keys.Count}]");
                Logger.Instance.LogInfoEvent($"Found=[{nodeDict.Values.Where(m=>m!=null).Count()}]");
                Logger.Instance.LogInfoEvent($"Not found=[{nodeDict.Values.Where(m=>m==null).Count()}]");


                // add new ones found
                //??da.CommitChanges();
            }

            //
        }

        private Feature getFeature(Node node, Feature[] features) {
            foreach( var feature in features) {
                if ( node.Code.StartsWith(feature.properties.SUBSTATION)) {
                    return feature;
                }
            }
            return null;
        }


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
            public string SUBSTATION {get; set;}
            public string OPERATING_ {get; set;}
            public string ACTION_DTT {get; set;}
            public string STATUS {get; set;}
            public string Substation {get; set;}
            public string OWNER_FLAG {get; set;}
        }

        public class Geometry {
            public string type {get; set;}
            public JsonElement coordinates {get; set;}
        }

    }
}