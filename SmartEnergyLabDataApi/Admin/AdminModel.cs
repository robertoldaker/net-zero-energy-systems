using System;
using HaloSoft.EventLogger;

namespace EnergySystemLabDataApi
{

    public class AdminModel
    {

        public AdminModel() : base()
        {
        }

        public String LoadLogFile()
        {
            string logFile = Logger.Instance.LogFile;
            return File.ReadAllText(logFile);
        }
    }


}