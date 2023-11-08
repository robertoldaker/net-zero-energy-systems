using System.ComponentModel;
using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization;
using HaloSoft.EventLogger;
using Microsoft.AspNetCore.SignalR;
using MySqlX.XDevAPI.Common;
using NHibernate.Criterion;
using NLog.Targets;
using Npgsql.Replication;
using Org.BouncyCastle.Crypto.Signers;
using SmartEnergyLabDataApi.Data;

namespace SmartEnergyLabDataApi.Models
{    
    public class EVDemandRunner {
        private static EVDemandRunner? _instance=null;
        private static IHubContext<NotificationHub> _hubContext;
        public static void Initialise(string contentRootPath,IHubContext<NotificationHub> hubContext) {            
            _instance = new EVDemandRunner(contentRootPath);
            _hubContext = hubContext;
            _instance.start();
        }

        public static EVDemandRunner Instance {
            get{
                if ( _instance==null) {
                    throw new Exception("Please run EVDemandRunner.Initialise() before accessing instance member");
                }
                return _instance;
            }
        }

        private Process? _proc;
        private Task _startTask;
        private bool _ready;
        private object _readyLock = new object();
        private bool _started;
        private string _contentRootPath;
        private const string PYTHON_SCRIPT = "EVDemandRunner.py";

        private EVDemandRunner(string contentRootPath) {
            _contentRootPath = contentRootPath;
            _ready=false;
            _started=false;
        }

        private void start() {
            _startTask = new Task(()=>{
                try {
                    var ready = startEvDemandPredictor();
                    if ( ready ) {
                        Logger.Instance.LogInfoEvent($"{PYTHON_SCRIPT} is ready");
                    } else {
                        Logger.Instance.LogErrorEvent("Unexpected output from EVDemand predictor");
                    }
                } catch(Exception e) {
                    Logger.Instance.LogErrorEvent("Problem starting EVDemand predictor");
                    Logger.Instance.LogException(e);                    
                }
            });
            _startTask.Start();
        }

        public void Restart() {
            if ( IsRunning ) {
                _proc.Kill(true);
            }
            start();
        }

        public bool IsReady {
            get {
                lock(_readyLock) {
                    return _ready;
                }
            }
        }

        private void setReady(bool value) {
            lock(_readyLock) {
                _ready = value;
            }
            sendStatusUpdate();
        }

        public bool IsRunning {
            get {
                return _started && !_proc.HasExited;
            }
        }

        private void setStarted(bool value) {
            _started = value;                
            sendStatusUpdate();
        }

        private void sendStatusUpdate() {
            _hubContext.Clients.All.SendAsync("EVDemandStatus",GetStatus());
        }

        private bool startEvDemandPredictor() {
            var workingDir = $"{_contentRootPath}LowVoltage/EVDemand";
			ProcessStartInfo oInfo = new ProcessStartInfo("python",PYTHON_SCRIPT);
			oInfo.UseShellExecute = false;
			oInfo.CreateNoWindow = true;
            oInfo.WorkingDirectory = workingDir;
			oInfo.RedirectStandardOutput = true;
			oInfo.RedirectStandardError = true;
            oInfo.RedirectStandardInput = true;

			StreamReader srOutput = null;

            setReady(false);
			_proc = Process.Start(oInfo);
            _proc.EnableRaisingEvents = true;
            setStarted(true);
            _proc.Exited+=processExited;
            if ( _proc==null) {
                throw new Exception("Problem starting EVDemand process");
            }
			srOutput = _proc.StandardOutput;
            Logger.Instance.LogInfoEvent($"Started {PYTHON_SCRIPT}, waiting for OK");
            // wait for the script to output OK
            bool cont=true;
            string line;
            while( cont ) {
			    line = srOutput.ReadLine().TrimEnd();
                cont = processLine(line,null);
            }
            setReady(true);
            return true;
        }

        private bool processLine(string line, TaskRunner? taskRunner) {
            bool cont = true;
            if ( line.StartsWith("OK:")) {
                cont=false;
            } else if (line.StartsWith("LOG:")) {
                line=line.Substring(4);
                Logger.Instance.LogInfoEvent(line);
            } else if (line.StartsWith("ERROR:")) {
                line=line.Substring(6);
                Logger.Instance.LogErrorEvent(line);
            } else if (line.StartsWith("PROGRESS:")) {
                var percentStr=line.Substring(9);
                if ( int.TryParse(percentStr, out int percent)) {
                    if ( taskRunner!=null) {
                        taskRunner.Update(percent);
                    }
                } else {
                    Logger.Instance.LogErrorEvent($"Problem parsing PROGRESS: message [{line}]");
                }
            } else if (line.StartsWith("PROGRESS_TEXT:")) {
                var textStr=line.Substring(14);
                if ( taskRunner!=null) {
                    taskRunner.Update(textStr,false);
                }
            } else if (line.StartsWith("RESULT:")) {
                line=line.Substring(7);
                //?? Need to parse output
                Logger.Instance.LogInfoEvent(line);
                cont=false;
            }
            return cont;
        }

        private void processExited(object? sender, EventArgs args) {
            setReady(false);            
            Logger.Instance.LogInfoEvent("Premature exit of EVDemand tool:-");
            var error = _proc.StandardError.ReadToEnd();
            Logger.Instance.LogInfoEvent(error);
        }

        public void RunDistributionSubstation(int id, TaskRunner? taskRunner) {
            var input = EVDemandInput.CreateFromDistributionId(id);
            runEvDemandPreditor(input, taskRunner);
        }

        public void RunPrimarySubstation(int id, TaskRunner? taskRunner) {
            var input = EVDemandInput.CreateFromPrimaryId(id);
            runEvDemandPreditor(input, taskRunner);
        }

        public void RunGridSupplyPoint(int id, TaskRunner? taskRunner) {
            var input = EVDemandInput.CreateFromGridSupplyPointId(id);
            runEvDemandPreditor(input, taskRunner);
        }

        private void runEvDemandPreditor(EVDemandInput input, TaskRunner? taskRunner) {
            setReady(false);
            var inputStr=JsonSerializer.Serialize(input);
            Logger.Instance.LogInfoEvent("Writing EVDemandInput json to stdin ..");
            _proc.StandardInput.WriteLine(inputStr);
            Logger.Instance.LogInfoEvent("Reading output from EVDemand tool ...");
            bool cont=true;
            string line;
            while( cont ) {
                line = _proc.StandardOutput.ReadLine().TrimEnd();
                cont = processLine(line, taskRunner);
            }
            setReady(true);
        }

        public class Status {
            public Status(bool isRunning, bool isReady) {
                IsRunning = isRunning;
                IsReady = isReady;
            }
            public bool IsRunning {get; set;}
            public bool IsReady {get; set;}
        }

        public Status GetStatus() {
            return new Status(IsRunning, IsReady);
        }


    }

    public class EVDemandInput {
        private void initialise() {
            predictorParams = new PredictorParams() { vehicleUsage = VehicleUsage.Medium};
            regionData = new List<RegionData>();
        }
        public EVDemandInput() {
            initialise();
        }

        public static EVDemandInput CreateFromDistributionId(int id) {
            var evDi = new EVDemandInput();
            using ( var da = new DataAccess()) {
                var dss = da.Substations.GetDistributionSubstation(id);
                if ( dss==null) {
                    throw new Exception($"Could not find substation with id=[{id}]");
                }
                var boundaries = da.GIS.GetBoundaries(dss.GISData.Id);
                //
                if ( boundaries.Count>0 ) {
                    var boundary=boundaries[0];
                    var rD = new RegionData(id,RegionType.Dist);
                    rD.latitudes = boundary.Latitudes;
                    rD.longitudes = boundary.Longitudes;
                    if ( dss.SubstationData!=null ) {
                        rD.numCustomers = dss.SubstationData.NumCustomers;
                    } else {
                        throw new Exception($"No substation data defined for dss=[{dss.Name}]");
                    }
                    evDi.regionData.Add(rD);
                } else {
                    throw new Exception($"No boundaries defined for dss=[{dss.Name}]");
                }
            }
            return evDi;
        }

        public static EVDemandInput CreateFromPrimaryId(int id) {
            var evDi = new EVDemandInput();
            using ( var da = new DataAccess()) {
                var pss = da.Substations.GetPrimarySubstation(id);
                if ( pss==null) {
                    throw new Exception($"Could not find substation with id=[{id}]");
                }
                var boundaries = da.GIS.GetBoundaries(pss.GISData.Id);
                //
                if ( boundaries.Count>0 ) {
                    var boundary=boundaries[0];
                    var rD = new RegionData(id,RegionType.Primary);
                    rD.latitudes = boundary.Latitudes;
                    rD.longitudes = boundary.Longitudes;
                    var numCustomers = da.Substations.GetCustomersForPrimarySubstation(id);
                    if ( numCustomers>0 ) {
                        rD.numCustomers = numCustomers;
                    } else {
                        throw new Exception($"Num customers is 0 for pss=[{pss.Name}]");
                    }
                    evDi.regionData.Add(rD);
                } else {
                    throw new Exception($"No boundaries defined for pss=[{pss.Name}]");
                }
            }
            return evDi;
        }

        public static EVDemandInput CreateFromGridSupplyPointId(int id) {
            var evDi = new EVDemandInput();
            using ( var da = new DataAccess()) {
                var gsp = da.SupplyPoints.GetGridSupplyPoint(id);
                if ( gsp==null) {
                    throw new Exception($"Could not find grid supply point with id=[{id}]");
                }
                var boundaries = da.GIS.GetBoundaries(gsp.GISData.Id);
                //
                if ( boundaries.Count>0 ) {
                    var psss=da.Substations.GetPrimarySubstationsByGridSupplyPointId(id);
                    foreach( var boundary in boundaries) {
                        var numCustomers=0;
                        foreach( var pss in psss ) {
                            var lat = pss.GISData.Latitude;
                            var lng = pss.GISData.Longitude;
                            if( GISUtilities.IsPointInPolygon(lat,lng, boundary.Latitudes,boundary.Longitudes )) {
                                numCustomers += da.Substations.GetCustomersForPrimarySubstation(pss.Id);
                            }
                        }
                        if ( numCustomers>0 ) {
                            var rD = new RegionData(id,RegionType.GSP);
                            rD.latitudes = boundary.Latitudes;
                            rD.longitudes = boundary.Longitudes;
                            rD.numCustomers = numCustomers;
                            evDi.regionData.Add(rD);
                        } 
                    }
                } else {
                    throw new Exception($"No boundaries defined for gsp=[{gsp.Name}]");
                }
            }
            return evDi;
        }

        /// <summary>
        /// Defines the region over which the prediction will take place
        /// </summary> <summary>
        /// 
        /// </summary>
        public enum RegionType { Dist, Primary, GSP}
        public class RegionData {
            public RegionData(int _id, RegionType _type) {
                id = _id;
                type=_type;
            }
            public string className {
                get {
                    return "EVDemandInput.RegionData";
                }
            }
            public int id {get; set;}
            public RegionType type{ get; set;}
            public double[] latitudes {get; set;}
            public double[] longitudes {get; set;}
            public int numCustomers {get; set;}
        } 
        /// <summary>
        /// Params associated with prediction
        /// </summary>
        public enum VehicleUsage { Low, Medium, High}
        public class PredictorParams {            
            public string className {
                get {
                    return "EVDemandInput.PredictorParams";
                }
            }
            public VehicleUsage vehicleUsage {get; set;}

            //?? Not used at present - but could be??
            public List<int> years {get; set;}
        } 

        public string className {
            get {
                return "EVDemandInput";
            }
        }
        public List<RegionData> regionData {get; set;}
        public PredictorParams predictorParams {get;set;}
    }

    
}
