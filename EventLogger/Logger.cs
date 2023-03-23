using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NLog;
using NLog.Config;
using NLog.Targets;
using System.IO;

namespace HaloSoft.EventLogger
{
    public class Logger : IEventLogger
    {
        public event EventLogged EventLoggedEvent;

        private static Logger m_instance=null;
        private static object m_instanceLock = new object();
        private NLog.Logger m_NLogLogger = NLog.LogManager.GetLogger("Test");
        private static Configuration m_config;
        private string m_logFile;
        private string m_archivesFolder;

        private Logger(string baseDir)
        {
            ConfigureNLog(baseDir);
        }

        private void ConfigureNLog(string baseDir)
        {
            if ( m_config ==null )
            {
                m_config = new Configuration();
            }
            // Step 1. Create configuration object 
            LoggingConfiguration config = new LoggingConfiguration();

            FileTarget fileTarget = new FileTarget();
            config.AddTarget("file", fileTarget);

            // Step 3. Set target properties
            string baseLogsDir = Path.Combine(baseDir,"Logs");
            if ( !Directory.Exists(baseLogsDir) ) {
                Directory.CreateDirectory(baseLogsDir);
            }
            m_logFile = Path.Combine(baseLogsDir, "AB.log");
            fileTarget.FileName = m_logFile;
            fileTarget.Layout = "${longdate} ${level:uppercase=true} ${message}";
            m_archivesFolder = Path.Combine(baseLogsDir, "Archives");
            fileTarget.ArchiveFileName= Path.Combine(m_archivesFolder, "AB{#}.log");
            fileTarget.ArchiveEvery = FileArchivePeriod.Day;
            fileTarget.ArchiveNumbering = ArchiveNumberingMode.Rolling;
            fileTarget.MaxArchiveFiles=7;
            fileTarget.ConcurrentWrites=true;
            fileTarget.KeepFileOpen=false;
            fileTarget.Encoding = Encoding.UTF8;

            // Step 4. Define rules
            LoggingRule rule2 = new LoggingRule("*", LogLevel.Debug, fileTarget);
            config.LoggingRules.Add(rule2);

            // Step 5. Activate the configuration
            LogManager.Configuration = config;

        }

        public static void SetConfiguration( Logger.Configuration config)
        {
            m_config = config;
        }

        public string LogFile
        {
            get
            {
                return m_logFile;
            }
        }

        public string ArchivesFolder
        {
            get
            {
                return m_archivesFolder;
            }
        }

        public static Logger Instance
        {
            get
            {
                lock (m_instanceLock)
                {
                    if (m_instance == null)
                    {
                        throw new Exception("Please call Initialise to configure Logger");
                    }
                    return m_instance;
                }
            }
        }

        public static void Initialise(string baseDir)
        {
            m_instance = new Logger(baseDir);
        }

        public void LogEvent(Event eventToLog)
        {
            try {
                LogLevel logLevel;
                if (eventToLog.EventType == EventType.INFO)
                {
                    logLevel = LogLevel.Info;
                }
                else if (eventToLog.EventType == EventType.WARNING)
                {
                    logLevel = LogLevel.Warn;
                }
                else if (eventToLog.EventType == EventType.ERROR || eventToLog.EventType == EventType.EXCEPTION)
                {
                    logLevel = LogLevel.Error;
                }
                else if (eventToLog.EventType == EventType.FATAL_ERROR)
                {
                    logLevel = LogLevel.Fatal;
                }
                else
                {
                    logLevel = LogLevel.Info;
                }
                //
                m_NLogLogger.Log(logLevel, eventToLog.Message);
                //
                RaiseEventLoggedEvent(eventToLog);
            } catch {
                
            }
        }

        public void LogInfoEvent(string message)
        {
            LogEvent( new Event(EventType.INFO, message) );
        }

        public void LogInfoEvent(string message, params object[] p)
        {
            LogInfoEvent(string.Format(message, p));
        }

        public void LogWarningEvent(string message)
        {
            LogEvent(new Event(EventType.WARNING, message));
        }

        public void LogWarningEvent(string message, params object[] p)
        {
            LogWarningEvent(string.Format(message, p));
        }

        public void LogErrorEvent(string message)
        {
            LogEvent(new Event(EventType.ERROR, message));
        }

        public void LogErrorEvent(string message, params object[] p)
        {
            LogErrorEvent(string.Format(message, p));
        }

        public void LogFatalErrorEvent(string message)
        {
            LogEvent(new Event(EventType.FATAL_ERROR, message));
        }

        public void LogFatalErrorEvent(string message, params object[] p)
        {
            LogFatalErrorEvent(string.Format(message, p));
        }

        public void LogException(Exception e, string prompt="")
        {
            string innerEMess = "";
            Exception ie = e.InnerException;
            while(ie!=null)
            {
                innerEMess += "|" + ie.Message;
                ie = ie.InnerException;
            }
            string eMess = tidyErrorMessage(e.Message);
            innerEMess = tidyErrorMessage(innerEMess);
            string mess = string.Format($"{prompt}\r\n{e.GetType().Name} raised\r\nMessage:\r\n{eMess}\r\nInner exception:\r\n{innerEMess}\r\nStackTrace:\r\n{e.StackTrace}");
            LogEvent( new ExceptionEvent( mess ));
        }

        private string tidyErrorMessage( string mess)
        {
            // This stops json in the message from being interepreted as a c# interpolated string
            if (!string.IsNullOrEmpty(mess) && mess.Contains("{"))
            {
                mess = mess.Replace('{', '[');
                mess = mess.Replace('}', ']');
            }
            return mess;
        }

        private void RaiseEventLoggedEvent(Event eventToLog)
        {
            if ( EventLoggedEvent!=null) {
                EventLoggedEvent( this, eventToLog );
            }
        }

        public class Configuration
        {
            public Configuration()
            {
                BaseDir = "${basedir}";
            }
            public string BaseDir { get; set; }
        }
    }

}
