using HaloSoft.EventLogger;

namespace CommonInterfaces.Models;

public class LogData
{
    public LogData() {
        Log = "";
    }

    public LogData(Logger logger) {
        string logFile = logger.LogFile;                 
        Log = File.ReadAllText(logFile);
    }
    public string Log {get; set;}

}

