using System.Diagnostics;
using System.Text;
using System.Text.Json;
using CommonInterfaces.Models;
using ExcelDataReader;
using Google.Apis.Sheets.v4.Data;
using Google.Apis.Util;
using Google.Protobuf.WellKnownTypes;
using HaloSoft.EventLogger;
using LumenWorks.Framework.IO.Csv;
using NHibernate.Dialect.Schema;
using NHibernate.Linq.Functions;
using Npgsql.Replication.PgOutput.Messages;
using Org.BouncyCastle.Asn1.X509.Qualified;
using Org.BouncyCastle.Crypto.Signers;
using SmartEnergyLabDataApi.Data;
using SmartEnergyLabDataApi.Model;

namespace SmartEnergyLabDataApi.Models;

public class ScottishAndSouthernLoader {

    private HttpClient _httpClient;
    private object _httpClientLock = new object();
    private string _baseUrl = "https://data-api.ssen.co.uk"; 

    private TaskRunner? _taskRunner;


    public ScottishAndSouthernLoader(TaskRunner? taskRunner) {
        _taskRunner = taskRunner;
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

    public void Load() {
        // Gets names/locations of GSPs and primary substations
        processGenerationAndAvailabilityNetworkMap();
        // adds boundaries
        processGSPAndBSPShapeFile();
        processPrimaryShapeFiles();
        // lastely add distribution substations - needs primary boundaries in-place since it uses this
        // to assign upstream parent primary.
        processSSENSubstationData();
    }

    private void processSSENSubstationData() {
        var dataFile = downloadSSENSubstationData();
        var loader = new DistributionDataLoader(this,dataFile);
        loader.Load();
    }

    private string downloadSSENSubstationData() {
        var url = "/api/3/action/package_show?id=ssen-substation-data";
        var datasets = get<CKANPackageData>(url);
        var name = 	"SSEN Substation Data";
        var resource = getCKANResourse(datasets,name);
        var dataFile = downloadFile(resource.url,resource.name);
        return dataFile;
    }

    public class DistributionDataLoader {

        private ScottishAndSouthernLoader _loader;
        private string _dataFile;
        private int _numCreated;
        private int _numIgnored;
        private int _numProcessed;
        private IList<GISBoundary> _boundaries;

        public DistributionDataLoader(ScottishAndSouthernLoader loader, string dataFile) {
            _loader = loader;
            _dataFile = dataFile;
        }

        public class SSENRecord {
            public string OwnerName {get; set;}
            public string Type {get; set;}
            public string Name {get; set;}
            public double LocationXm {get; set;}
            public double LocationYm {get; set;}

            public bool IsDistributionSubstation() {
                return ( OwnerName == "SEPD" || OwnerName == "SHEPD" || OwnerName == "SHET" ) &&
                        Type.Contains("Distribution");
            }

            public bool HasValidLocation() {
                return LocationXm!=0 && LocationYm!=0;
            }
        }

        public void Load() {
            _numCreated = 0;
            _numIgnored = 0;
            _numProcessed = 0;
            _loader.checkCancelled();
            _loader.updateMessage("Getting primary boundaries ...");
            using( var da = new DataAccess() ) {
                _boundaries = da.GIS.GetPrimaryBoundaries(ImportSource.ScottishAndSouthernOpenData);
            }
            _loader.checkCancelled();
            var records = loadRecords();
            var numRecords = records.Count();
            var numProcessed = 0;
            var numPartial = 1000;
            _loader.updateMessage("Processing distribution substations ...");
            _loader.updateProgress(0);
            while ( numProcessed<numRecords ) {
                _loader.checkCancelled();
                loadPartial(records.Skip(numProcessed).Take(numPartial));
                numProcessed+=numPartial;
                _loader.updateMessage($"Processed [{numProcessed}] of [{numRecords}] ...",false);
                _loader.updateProgress( (numProcessed*100)/numRecords);
            }
            Logger.Instance.LogInfoEvent($"Finished loading substations, records={numRecords}, created={_numCreated}, ignored={_numIgnored}");
        }

        private List<SSENRecord> loadRecords() {
            var records = new List<SSENRecord>();
            using( var sr = new StreamReader(_dataFile) ) {
                using (CsvReader reader = new CsvReader(sr, true))
                {
                    // Read header
                    reader.ReadNextRecord();
                    while (reader.ReadNextRecord())
                    {
                        //
                        var record = new SSENRecord() {
                            OwnerName = reader[1],
                            Type = reader[2],
                            Name = reader[4],
                            LocationXm = double.Parse(reader[12]),
                            LocationYm = double.Parse(reader[14])
                        };
                        double value;
                        if ( double.TryParse(reader[12], out value) ) {
                            record.LocationXm = value;
                        } else {
                            Logger.Instance.LogInfoEvent($"Could not parse LocationXm value [{reader[12]}]");
                        }
                        if ( double.TryParse(reader[14], out value) ) {
                            record.LocationYm = value;
                        } else {
                            Logger.Instance.LogInfoEvent($"Could not parse LocationYm value [{reader[14]}]");
                        }
                        // add it to the list if its a distribution substation
                        if ( record.IsDistributionSubstation() ) {
                            records.Add(record);
                        }
                    }
                }
            }
            return records;
        }

        private void loadPartial(IEnumerable<SSENRecord> records) {            
            using( var da = new DataAccess() ) {

                var toAdd = new List<DistributionSubstation>();
                foreach( var record in records) {
                    var name = record.Name;
                    var dss = da.Substations.GetDistributionSubstation(ImportSource.ScottishAndSouthernOpenData,null,null,name);
                    // Only way I can figure out to find its parent primary substation is to see which boundary the dss is in
                    var latLon = LatLonConversions.ConvertOSToLatLon((double) record.LocationXm,(double) record.LocationYm);
                    // create one if not exist
                    if ( dss==null) {
                        // get a GISData id
                        var gisId = getGISId(latLon);
                        if ( gisId!=0) {
                            // find primary substation from GIS id
                            var pss = da.GIS.GetPrimarySubstation(gisId);
                            dss=create(pss, record, latLon);
                            toAdd.Add(dss);
                            _numCreated++;
                        } else {
                            _numIgnored++;
                        }                       
                    } 
                    //
                    _numProcessed++;
                    //
                    if ( dss!=null) {
                        dss.SubstationData.Type = record.Type.StartsWith("Ground") ? DistributionSubstationType.Ground : DistributionSubstationType.Pole;
                        dss.GISData.Latitude = latLon.Latitude;
                        dss.GISData.Longitude = latLon.Longitude;
                    }
                    _loader.checkCancelled();
                }
                // Add ones we have created
                // (done here since it can effect the speed to search for existing ones)
                foreach( var dss in toAdd) {
                    da.Substations.Add(dss);
                }
                //
                da.CommitChanges();
            }
        }

        private DistributionSubstation create(PrimarySubstation pss, SSENRecord record, LatLon latLon) {
            var dss = new DistributionSubstation(ImportSource.ScottishAndSouthernOpenData,null,null,pss);
            dss.Name = record.Name;
            return dss;
        }

        private int getGISId(LatLon latLon) {
            foreach( var b in _boundaries) {
                if ( GISUtilities.IsPointInPolygon(latLon.Latitude,latLon.Longitude,b.Latitudes,b.Longitudes)) {
                    return b.GISData.Id;
                }
            }
            return 0;
        }

    }

    private void processGenerationAndAvailabilityNetworkMap() {
        var url = "/api/3/action/package_show?id=generation-availability-and-network-capacity";
        var datasets = get<CKANPackageData>(url);
        var sepdData = getDemandHeatMapData(datasets,"SEPD Generation Heat Map Update");
        var scotlandData = getDemandHeatMapData(datasets,"Scotland Demand Heat Map");
        var shetlandData = getDemandHeatMapData(datasets,"Shetland Demand Heat Map");
        // GSPs
        updateMessage($"Processing Southern England GSP records ...");
        processGsps(sepdData.GSPs);
        updateMessage($"Processing Scotland GSP records ...");
        processGsps(scotlandData.GSPs);
        updateMessage($"Processing Shetland GSP records ...");
        processGsps(shetlandData.GSPs);
        // Primaries
        updateMessage($"Processing Southern England Primary records ...");
        processPrimaries(sepdData.PSs);
        updateMessage($"Processing Scotland Primary records ...");
        processPrimaries(scotlandData.PSs);
        updateMessage($"Processing Shetland Primary records ...");
        processPrimaries(shetlandData.PSs);
    }

    private void processGsps(List<DemandHeatMapData.GSPData> gspData) {
        int numIgnored=0;
        int numNew=0;
        string area="";
        using ( var da = new DataAccess() ) {
            var addedGSPs = new List<GridSupplyPoint>();
            foreach( var data in gspData) {
                area = data.Area;
                var ga = getGeographicalArea(da,data.Area);
                string name = data.Name.Trim();
                // Ignore if name is empty
                if (string.IsNullOrEmpty(name)) {
                    numIgnored++;
                    continue;
                }
                var gsp = da.SupplyPoints.GetGridSupplyPoint(ImportSource.ScottishAndSouthernOpenData,null,null,name);
                if ( gsp==null ) {
                    gsp = new GridSupplyPoint(ImportSource.ScottishAndSouthernOpenData,name,null, null,ga,ga.DistributionNetworkOperator);
                    Logger.Instance.LogInfoEvent($"Added new GSP [{name}]");
                    addedGSPs.Add(gsp);
                    numNew++;
                }
                //
                gsp.GISData.Latitude = data.Latitude;
                gsp.GISData.Longitude = data.Longitude;
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
        Logger.Instance.LogInfoEvent($"Area [{area}], [{gspData.Count}] GSP records processed, [{numNew}] added, [{numIgnored}] ignored");
    }
    private void processPrimaries(List<DemandHeatMapData.PSData> primData) {
        int numIgnored=0;
        int numNew=0;
        string area = "";
        using ( var da = new DataAccess() ) {
            var addedPrimaries = new List<PrimarySubstation>();
            var missingGsps = new Dictionary<string,int>();
            foreach( var data in primData) {
                area = data.Area;
                var ga = getGeographicalArea(da,data.Area);
                string name = data.Name.Trim();
                // Ignore if name is empty
                if (string.IsNullOrEmpty(name)) {
                    numIgnored++;
                    continue;
                }
                var gsp = da.SupplyPoints.GetGridSupplyPoint(ImportSource.ScottishAndSouthernOpenData,null,null,data.GspParent);
                if ( gsp!=null ) {
                    var pss = da.Substations.GetPrimarySubstation(ImportSource.ScottishAndSouthernOpenData,null,null,name);
                    if ( pss==null ) {
                        pss = new PrimarySubstation(ImportSource.ScottishAndSouthernOpenData,null,null,gsp);
                        pss.Name = name;
                        addedPrimaries.Add(pss);
                        numNew++;
                    }
                    //
                    pss.GISData.Latitude = data.Latitude;
                    pss.GISData.Longitude = data.Longitude;
                } else {
                    // Record name of missing GSP parent in dictionary with number of references
                    if ( !missingGsps.ContainsKey(data.GspParent)) {
                        missingGsps.Add(data.GspParent,1);
                    } else {
                        missingGsps[data.GspParent] += 1;
                    }
                }
                //
                checkCancelled();
            }

            // add new ones found
            foreach( var pss in addedPrimaries) {
                da.Substations.Add(pss);
                checkCancelled();
            }
            //
            da.CommitChanges();
            //
            foreach( var missingGsp in missingGsps) {
                Logger.Instance.LogWarningEvent($"Could not find GSP parent with name [{missingGsp.Key}], [{missingGsp.Value}] primary records ignored");
            }
        }
        //
        Logger.Instance.LogInfoEvent($"Area [{area}], [{primData.Count}] Primary records processed, [{numNew}] added, [{numIgnored}] ignored");
    }

    private GeographicalArea getGeographicalArea(DataAccess da, string area) {
        DNOAreas dnoArea;
        if ( area=="ENGLAND") {
            dnoArea = DNOAreas.SouthernEngland;
        } else if ( area == "Scotland") {
            dnoArea = DNOAreas.NorthScotland;
        } else if ( area == "Shetland") {
            dnoArea = DNOAreas.NorthScotland;
        } else {
            throw new Exception($"Unexpected license area found [{area}]");
        }
        var ga = da.Organisations.GetGeographicalArea(dnoArea);
        if ( ga==null) {
            throw new Exception($"Could not find Geographical area for area code=[{dnoArea}]");
        }
        return ga;
    }

    private DemandHeatMapData getDemandHeatMapData(CKANPackageData datasets, string name) {
        // Download the .xlsx files
        var resource = getCKANResourse(datasets,name);
        var sepdGenFile = downloadFile(resource.url,resource.name);
        // get data from them
        var heatMapData = new DemandHeatMapData();
        heatMapData.Load(sepdGenFile);
        return heatMapData;
    }

    private class DemandHeatMapData {
        public List<GSPData> GSPs {get; set;}
        public List<BSPData> BSPs {get; set;}
        public List <PSData> PSs {get; set;}
        public string Path {get; set;}

        public void Load(string spreadsheetPath) {
            Path = spreadsheetPath;
            BSPs = new List<BSPData>();
            PSs = new List<PSData>();            
            using (var stream = new FileStream(spreadsheetPath,FileMode.Open)) {
                using (var reader = ExcelReaderFactory.CreateReader(stream)) {
                    do {
                        var name = reader.Name;
                        //
                        if ( name=="GSP") {
                            loadGSPResults(reader);
                        }
                        if ( name=="BSP") {
                            loadBSPResults(reader);
                        }
                        if ( name=="PS") {
                            loadPSResults(reader);
                        }
                    } while (reader.NextResult());
                }
            }
        }

        private void loadGSPResults(IExcelDataReader reader) {
            GSPs = new List<GSPData>();
            // consume the header
            reader.Read();
            var areaIndex = 0;
            var nameIndex = 1;
            var latIndex = 3;
            var lonIndex = 4;
            int rowNum=1;
            //
            while ( reader.Read() ) {
                var data  = new GSPData();
                rowNum++;
                try {
                    if (reader.GetValue(0)==null) {
                        break;
                    }
                    data.Area = reader.GetString(areaIndex);
                    data.Name = reader.GetString(nameIndex);
                    data.Latitude = getDouble(reader,latIndex);
                    data.Longitude = getDouble(reader,lonIndex);
                    GSPs.Add(data);                    
                } catch( Exception e) {
                    Logger.Instance.LogErrorEvent($"Problem reading GSP data row=[{rowNum}], path=[{Path}]");
                    Logger.Instance.LogException(e);
                }
            }
        }

        private double getDouble(IExcelDataReader reader, int index) {
            var value = reader.GetValue(index);
            if ( value is double) {
                return (double) value;
            } else if ( value is string) {
                return double.Parse((string) value);
            } else {
                throw new Exception($"Unexpected column type [{value.GetType().Name}]");
            }
        }

        private void loadBSPResults(IExcelDataReader reader) {
            BSPs = new List<BSPData>();
            int rowNum = 1;
            // consume the header
            reader.Read();
            var areaIndex = 0;
            var nameIndex = 1;
            var gspIndex = 2;
            var latIndex = 4;
            var lonIndex = 5;
            //
            while ( reader.Read() ) {
                rowNum++;
                try {
                    if ( reader.GetValue(0)==null ) {
                        break;
                    }
                    var data  = new BSPData();                
                    data.Area = reader.GetString(areaIndex);
                    data.Name = reader.GetString(nameIndex);
                    data.GspParent = reader.GetString(gspIndex);
                    data.Latitude = getDouble(reader,latIndex);
                    data.Longitude = getDouble(reader,lonIndex);
                    BSPs.Add(data);
                } catch( Exception e) {
                    Logger.Instance.LogErrorEvent($"Problem reading BSP data row=[{rowNum}], path=[{Path}]");
                    Logger.Instance.LogException(e);
                }
            }
        }

        private void loadPSResults(IExcelDataReader reader) {
            PSs = new List<PSData>();
            int rowNum=1;
            // consume the header
            reader.Read();
            var areaIndex = 0;
            var nameIndex = 1;
            var gspIndex = 2;
            var bspIndex = 3;
            var latIndex = 5;
            var lonIndex = 6;
            //
            while ( reader.Read() ) {
                rowNum++;
                try {
                    if ( reader.GetValue(0)==null ) {
                        break;
                    }
                    var data  = new PSData();
                    data.Area = reader.GetString(areaIndex);
                    data.Name = reader.GetString(nameIndex);
                    data.GspParent = reader.GetString(gspIndex);
                    data.BspParent = reader.GetString(bspIndex);
                    data.Latitude = getDouble(reader,latIndex);
                    data.Longitude = getDouble(reader,lonIndex);
                    PSs.Add(data);
                } catch( Exception e) {
                    Logger.Instance.LogErrorEvent($"Problem reading PS data row=[{rowNum}], path=[{Path}]");
                    Logger.Instance.LogException(e);
                }
            }
        }
        public class GSPData {
            public string Name {get; set;}
            public string Area {get; set;}
            public double Latitude {get; set;}
            public double Longitude {get; set;}
        }
        public class BSPData {
            public string Name {get; set;}
            public string GspParent {get; set;}
            public string Area {get; set;}
            public double Latitude {get; set;}
            public double Longitude {get; set;}
        }
        public class PSData {
            public string Name {get; set;}
            public string GspParent {get; set;}
            public string BspParent {get; set;}
            public string Area {get; set;}
            public double Latitude {get; set;}
            public double Longitude {get; set;}
        }
    }

    private void processDemandHeatMapSpreadsheet(string path) {
    }

    private CKANResource getCKANResourse(CKANPackageData datasets, string name) {
        var resource = datasets.result.resources.Where(m=>m.name.StartsWith(name)).OrderByDescending(m=>m.last_modified).Take(1).SingleOrDefault();
        if ( resource==null) {
            throw new Exception($"Cannot find dataset entry for \"{name}\"");
        }
        return resource;
    }

    private string downloadFile(string url,string name) {
        updateMessage($"Downloading [{name}] ...");
        var client = getHttpClient();
        string path = "";

        // Download json file unless we are developing
        using (HttpRequestMessage message = getRequestMessage(HttpMethod.Get, url)) {
            //
            var response = client.SendAsync(message).Result;
            //
            if ( response.IsSuccessStatusCode) {
                var str = response.Headers.ToString();
                var stream = response.Content.ReadAsStream();
                if ( stream!=null) {
                    var fileName = response.Content.Headers.ContentDisposition.FileName;
                    if ( fileName!=null) {
                        path = Path.Combine(AppFolders.Instance.Temp,fileName);
                        checkCancelled();
                        saveToFile(stream, path);
                        checkCancelled();
                    } else {
                        throw new Exception($"No filename specified in url download [{name}]");
                    }
                }
            } else {
                throw new Exception($"Problem reading file [{name}], [{response.StatusCode},{response.ReasonPhrase}]");
            }
        }
        checkCancelled();
        return path;
    }

    private void processGSPAndBSPShapeFile() {
        string url = "/dataset/915ba157-79b5-4d80-a0f0-c900959f51a6/resource/93d7b968-7861-444e-ae4e-1d6df38076de/download/dataportalfiles.zip";
        downloadAndExtractZip(url,"dataPortalFiles.zip");
        var geoFiles = convertShapeToGeojson("DataPortalFiles");
        var gspFiles = geoFiles.Where( m=>m.Contains("GSP"));
        foreach( var gspFile in gspFiles) {
            loadGSPBoundaries(gspFile);
        }
    }

    private void processPrimaryShapeFiles() {
        string shepdUrl = "/dataset/811cada9-c800-42f9-b333-66c8375f86c7/resource/6d9a2ce2-7c82-433a-8cee-a1a82320d9f8/download/shepd-primary-sub-boundaries.zip";
        string sepdUrl = "/dataset/811cada9-c800-42f9-b333-66c8375f86c7/resource/2646de46-bd43-4155-a94b-afd53f2054c6/download/sepd-primary-sub-boundaries.zip";
        downloadAndExtractZip(shepdUrl,"shepd-primary-sub-boundaries.zip");
        downloadAndExtractZip(sepdUrl,"sepd-primary-sub-boundaries.zip");
        var geoFiles = convertShapeToGeojson("SEPD Primary Sub Boundaries");        
        var shepdGeoFiles = convertShapeToGeojson("SHEPD Primary Sub Boundaries");
        geoFiles.AddRange(shepdGeoFiles);
        foreach( var primFile in geoFiles) {
            loadPrimaryBoundaries(primFile);
        }
    }

    private void downloadAndExtractZip(string url,string filename) {
        updateMessage($"Downloading [{filename}] ...");
        var client = getHttpClient();

        //
        var zipFile = Path.Combine(AppFolders.Instance.Temp,filename);

        // Download json file unless we are developing
        using (HttpRequestMessage message = getRequestMessage(HttpMethod.Get, url)) {
            //
            var response = client.SendAsync(message).Result;
            //
            if ( response.IsSuccessStatusCode) {
                var str = response.Headers.ToString();
                var stream = response.Content.ReadAsStream();
                if ( stream!=null) {
                    saveToFile(stream, zipFile);
                    checkCancelled();
                    var exe = new Execute();
                    var resp = exe.Run("unzip",$"-o \"{filename}\"",AppFolders.Instance.Temp);
                    if ( resp<0) {
                        throw new Exception($"Problem unzipping [{filename}], error=[{exe.StandardError}]");
                    }                    
                    checkCancelled();
                }
            } else {
                throw new Exception($"Problem reading zip file [{filename}], [{response.StatusCode},{response.ReasonPhrase}]");
            }
        }
        checkCancelled();
        return;
    }

    private List<string> convertShapeToGeojson(string subFolder) {
        var folder = Path.Combine(AppFolders.Instance.Temp,subFolder);
        var shapeFiles = new List<string>();
        getFiles(shapeFiles, folder,"*.shp");
        var geoJsonFiles = new List<string>();
        foreach( var shpFile in shapeFiles) {
            var fileName = Path.GetFileName(shpFile);
            updateMessage($"Converting shape file [{fileName}] to geojson...");
            var geojsonFile = Path.ChangeExtension(shpFile,"geojson");
            GISUtilities.ConvertToGeoJson(shpFile,geojsonFile);
            geoJsonFiles.Add(geojsonFile);
        }
        //
        return geoJsonFiles;
    }

    private string loadGSPBoundaries(string geoJsonFile) {

        string msg = "";
        var boundaryLoader = new GISBoundaryLoader(this);
        int numIgnored = 0;
        int numMissing = 0;
        GeoJson geoJson;
        string gaName = "";
        //
        var nameAliases = new Dictionary<string,string>(StringComparer.InvariantCultureIgnoreCase) {
            {"Thurso South","Thurso"},
            {"BRAMLEY (FLEE)","FLEET BRAMLEY"}
        };

        updateMessage($"Processing GSP boundaries ...");

        using( var da = new DataAccess()) {

            using( var fs = new FileStream(geoJsonFile,FileMode.Open)) {
                geoJson = JsonSerializer.Deserialize<GeoJson>(fs);
                var ga = getGeographicalArea(da,geoJson);
                gaName = ga.Name;
                foreach( var feature in geoJson.features) {                    
                    string name = feature.properties.GSPName;
                    // Ignore if name is empty (West midlands has this)
                    if (string.IsNullOrEmpty(name)) {
                        numIgnored++;
                        continue;
                    }
                    var gsp = getGSP(da,nameAliases,name);
                    if ( gsp==null ) {
                        Logger.Instance.LogWarningEvent($"Cannot find GSP [{name}]");
                        numMissing++;
                    } else {
                        //
                        boundaryLoader.AddGISData(da,gsp.GISData,feature);
                    }
                }                
                //
                checkCancelled();
            }

        }

        // Use boundary loader to load boundaries into GISData
        updateMessage($"Loading GSP boundaries ...");
        boundaryLoader.Load();
        //
        msg = $"{gaName} area, GSP boundaries, [{geoJson.features.Count()}] records processed, [{numIgnored}] ignored, [{numMissing}] missing";
        Logger.Instance.LogInfoEvent(msg);
        return msg;
    }

    private GridSupplyPoint? getGSP(DataAccess da, Dictionary<string,string> aliases, string name) {
        var gsp = da.SupplyPoints.GetGridSupplyPoint(ImportSource.ScottishAndSouthernOpenData,null,null,name);
        if ( gsp==null ) {
            if ( aliases.ContainsKey(name) ) {
                name = aliases[name];
                gsp = da.SupplyPoints.GetGridSupplyPoint(ImportSource.ScottishAndSouthernOpenData,null,null,name);
            } else if ( name.EndsWith(" GSP")) {
                name = name.Replace(" GSP","");
                gsp = da.SupplyPoints.GetGridSupplyPoint(ImportSource.ScottishAndSouthernOpenData,null,null,name);
            }
        }
        return gsp;
    }

    private GeographicalArea getGeographicalArea(DataAccess da, GeoJson geojson) {
        DNOAreas dnoArea;
        if ( geojson.name.Contains("SEPD")) {
            dnoArea = DNOAreas.SouthernEngland;
        } else if ( geojson.name.Contains("SHEPD") ) {
            dnoArea = DNOAreas.NorthScotland;
        } else {
            throw new Exception($"Unexpected license area found [{geojson.name}]");
        }
        var ga = da.Organisations.GetGeographicalArea(dnoArea);
        if ( ga==null) {
            throw new Exception($"Could not find Geographical area for area code=[{dnoArea}]");
        }
        return ga;
    }

    private string loadPrimaryBoundaries(string geoJsonFile) {

        string msg = "";
        var boundaryLoader = new GISBoundaryLoader(this);
        int numIgnored = 0;
        int numMissing = 0;
        string gaName = "";
        GeoJson geoJson;
        //
        var nameAliases = new Dictionary<string,string>(StringComparer.InvariantCultureIgnoreCase) {
            {"Bleddington","Bledington"},
            {"Chandlersford","Chandlers Ford"},
            {"Chilton Cantello","Chilton Cantelo"},
            {"Cowes PS","Cowes Power Station"},
            {"crookham (church)","crookham"},
            {"dorchester town","dorchester"},
            {"hinchelsea","hincheslea"},
            {"laburnam road","laburnum road"},
            {"mill lane, ringwood","mill lane"},
            {"plessey swindon","plessey"},
            {"plessy titchfield","plessey titchfield"},
            {"poole","pool (hill street)"},
            {"pressed steel swindon se","pressed steel swindon"},
            {"south berstead","south bersted"},
            {"winterbourne kingston","winterborne kingston"},
            {"yatton keynall","yatton keynell"},
        };

        updateMessage("Processing Primary boundaries ...");

        using( var da = new DataAccess()) {

            using( var fs = new FileStream(geoJsonFile,FileMode.Open)) {
                geoJson = JsonSerializer.Deserialize<GeoJson>(fs);
                var ga = getGeographicalArea(da,geoJson);
                gaName = ga.Name;
                foreach( var feature in geoJson.features) {                    
                    string name = feature.properties.Primary;
                    // Ignore if name is empty (West midlands has this)
                    if (string.IsNullOrEmpty(name)) {
                        numIgnored++;
                        continue;
                    }
                    var pss = getPrimary(da,nameAliases,name);
                    if ( pss==null ) {
                        Logger.Instance.LogWarningEvent($"Cannot find Primary substation [{name}]");
                        numMissing++;
                    } else {
                        //
                        boundaryLoader.AddGISData(da,pss.GISData,feature);
                    }
                }                
                //
                checkCancelled();
            }

        }

        // Use boundary loader to load boundaries into GISData
        updateMessage("Loading Primary boundaries ...");
        boundaryLoader.Load();
        //
        msg = $"{gaName} area, Primary boundaries, [{geoJson.features.Count()}] records processed, [{numIgnored}] ignored, [{numMissing}] missing";
        Logger.Instance.LogInfoEvent(msg);
        return msg;
    }

    private PrimarySubstation? getPrimary(DataAccess da, Dictionary<string,string> aliases, string name) {
        var pss = da.Substations.GetPrimarySubstation(ImportSource.ScottishAndSouthernOpenData,null,null,name);
        if ( pss==null ) {
            if ( aliases.ContainsKey(name) ) {
                name = aliases[name];
                pss = da.Substations.GetPrimarySubstation(ImportSource.ScottishAndSouthernOpenData,null,null,name);
            } 
        }
        return pss;
    }

    private void getFiles(List<string> files, string startFolder, string ext) {
        var newFiles = Directory.GetFiles(startFolder,ext);
        files.AddRange(newFiles);
        string[] folders = Directory.GetDirectories(startFolder);
        foreach (string folder in folders) {
            getFiles(files,folder, ext);
        }
        return;
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

    private HttpClient getHttpClient()
    {
        if (_httpClient == null) {
            lock (_httpClientLock) {
                _httpClient = new HttpClient() {
                    BaseAddress = new Uri(_baseUrl)
                };
            }
        }
        //
        return _httpClient;
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

    private class CKANPackageData {
        public bool success {get; set;}
        public CKANResult result {get; set;}
    }

    private class CKANResult {
        public CKANResource[] resources {get; set;}
    }

    private class CKANResource {
        public string name  {get; set;}
        public string package_id {get; set;}
        public string id {get; set;}
        public string url {get; set;}
        public DateTime? last_modified {get; set;}
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
        public string Licence_Ar {get; set;}
        public string GSP_name {get; set;}
        public string GSP_Name {get; set;} 
        public string GSP_Alias {get; set;}
        public string BSP_Name {get; set;}
        public string BSP_Alias {get; set;}
        public string BSP_kV {get; set;}
        public string Primary {get; set;}

        public string GSPName {
            get {
                return GSP_Name!=null ? GSP_Name : GSP_name;
            }
        }
    }

    public class Geometry {
        public string type {get; set;}
        public JsonElement coordinates {get; set;}
    }

    private class GISBoundaryLoader {
        private Dictionary<int,Feature> _featureDict = new Dictionary<int, Feature>();

        private ScottishAndSouthernLoader _parentLoader;

        public GISBoundaryLoader(ScottishAndSouthernLoader parentLoader) {
            _parentLoader = parentLoader;
        }
        public void AddGISData( DataAccess da, GISData data, Feature feature) {
            if ( _featureDict.ContainsKey(data.Id)) {
                Logger.Instance.LogWarningEvent($"Duplicate key found in feature dict for gidDataId=[{data.Id}]");
            } else {
                _featureDict.Add(data.Id,feature);
            }
        }

        public void Load() {
            var sw = new Stopwatch();
            sw.Start();
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
                        if ( feature.geometry.type=="MultiPolygon" ) {
                            var elements = feature.geometry.coordinates.Deserialize<double[][][][]>();
                            if ( elements!=null && boundaries!=null) {
                                gisData.UpdateBoundaryPoints(elements,boundaries, boundariesToAdd, boundariesToDelete, false);
                            }
                        } else if ( feature.geometry.type=="Polygon") {
                            var elements = feature.geometry.coordinates.Deserialize<double[][][]>();
                            if ( elements!=null && boundaries!=null) {
                                gisData.UpdateBoundaryPoints(elements,boundaries, boundariesToAdd, boundariesToDelete, false);
                            }
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
            Logger.Instance.LogInfoEvent($"Boundary load for [{_featureDict.Keys.Count}] features done in {sw.Elapsed}s");
        }
    }
}