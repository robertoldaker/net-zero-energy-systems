using System;
using System.ComponentModel;
using HaloSoft.EventLogger;
using Microsoft.AspNetCore.Server.IIS.Core;
using Microsoft.AspNetCore.SignalR;

namespace EvDemandService.Models
{

    public class AdminModel
    {
        private static AdminModel? _instance=null;
        private static object _instanceLock = new object();

        public static void Initialise(IHubContext<NotificationHub> hubContext) {
            lock( _instanceLock) {
                if ( _instance==null ) {
                    _instance = new AdminModel(hubContext);
                }
            }
        }

        public static AdminModel Instance {
            get {
                lock( _instanceLock) {
                    if ( _instance==null ) {
                        throw new Exception("Please call AdminModel.Initialise before accessing instance");
                    }
                    return _instance;
                }
            }
        }

        private bool _maintenanceMode = false;
        private object _maintenanceModeLock = new object();
        private IHubContext<NotificationHub> _hubContext;
        private AdminModel(IHubContext<NotificationHub> hubContext)
        {       
            _hubContext = hubContext;
        }

        public bool MaintenanceMode {
            get {
                lock( _maintenanceModeLock) {
                    return _maintenanceMode;
                }
            }
            set {
                lock( _maintenanceModeLock ) {                    
                    _maintenanceMode = value;
                    _hubContext.Clients.All.SendAsync("MaintenanceMode",value);
                }
            }
        }

        public LogData LoadLogFile()
        {
            return new LogData();
        }
        
    }

    public class SystemInfo {

        public int ProcessorCount { 
            get {
                return Environment.ProcessorCount;
            }
        }

        public bool MaintenanceMode {
            get {
                return AdminModel.Instance.MaintenanceMode;
            }
        }
    }

    public class LogData {
        public LogData() {
            string logFile = Logger.Instance.LogFile;                 
            Log = File.ReadAllText(logFile);
        }
        public string Log {get; set;}
    }


}