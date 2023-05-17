using System;
using HaloSoft.EventLogger;

namespace EnergySystemLabDataApi
{

    public class AdminModel
    {

        public AdminModel() : base()
        {
        }

        public LogData LoadLogFile()
        {
            return new LogData();
        }

        public string LoadGeoSpatialData() {
            return "";
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