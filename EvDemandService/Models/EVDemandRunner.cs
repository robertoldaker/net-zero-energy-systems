using System.ComponentModel;
using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization;
using CommonInterfaces.Models;
using HaloSoft.EventLogger;
using Microsoft.AspNetCore.SignalR;
using NLog.LayoutRenderers.Wrappers;
using NLog.Targets;

namespace EvDemandService.Models
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
        private const string PYTHON_SCRIPT = "main.py";

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
            var workingDir = $"{_contentRootPath}../EvDemandModel";
            string fileName;
            if ( AppEnvironment.Instance.Context == Context.Production) {
                fileName = "/home/roberto/anaconda3/bin/python";
            } else if (AppEnvironment.Instance.Context == Context.Staging) {
                fileName = "/home/rob/anaconda3/bin/python";
            } else {
                fileName = "python";
            }

			ProcessStartInfo oInfo = new ProcessStartInfo(fileName,PYTHON_SCRIPT);
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
                string result = "";
                cont = processLine(line,ref result);
            }
            setReady(true);
            return true;
        }

        private bool processLine(string line, ref string result, TaskRunner? taskRunner=null) {
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
                result=line.Substring(7);
                //?? Need to parse output

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

        public string RunPredictor(EVDemandInput input, TaskRunner? taskRunner=null) {
            if ( !IsReady ) {
                throw new Exception("EvModel predicton is not ready");
            }
            setReady(false);
            try {
                var result = "";
                var inputStr=JsonSerializer.Serialize(input);
                Logger.Instance.LogInfoEvent("Writing EVDemandInput json to stdin ..");
                _proc.StandardInput.WriteLine(inputStr);
                Logger.Instance.LogInfoEvent("Reading output from EVDemand tool ...");
                bool cont=true;
                string line;
                while( cont ) {
                    line = _proc.StandardOutput.ReadLine().TrimEnd();
                    cont = processLine(line, ref result, taskRunner);
                }
                return result;
            } finally {
                setReady(true);
            }
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
}
