using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace HaloSoft.EventLogger
{
    public delegate void EventLogged( IEventLogger eventLogger, Event eventLogged);

    public interface IEventLogger
    {
        void LogEvent(Event eventToLog);
        void LogInfoEvent(string message);
        event EventLogged EventLoggedEvent;
    }
}
