using System;
using HaloSoft.EventLogger;

namespace EnergySystemLabDataApi
{

    public class AdminModel
    {

        public AdminModel() : base()
        {
        }

        public string LoadLogFile()
        {
            string logFile = Logger.Instance.LogFile;
            return File.ReadAllText(logFile);
        }

        public string LoadGeoSpatialData() {
            return "";
        }
        
    }


}