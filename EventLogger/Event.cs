using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace HaloSoft.EventLogger
{
    public enum EventType { INFO, WARNING, ERROR, FATAL_ERROR, EXCEPTION };

    public class Event
    {
        private DateTime m_timeStamp;
        private string m_message;
        private EventType m_eventType;
        //
        public Event(EventType eventType, string message)
        {
            m_timeStamp = DateTime.Now;
            m_eventType = eventType;
            m_message = message;
        }

        //
        public string Message
        {
            get
            {
                return m_message;
            }
        }
        //
        public DateTime TimeStamp
        {
            get
            {
                return m_timeStamp;
            }
        }

        //
        public EventType EventType
        {
            get
            {
                return m_eventType;
            }
        }
    }

    //
    public class InfoEvent : Event
    {
        public InfoEvent(string message)
            : base(EventType.INFO, message)
        {
        }
    }

    //
    public class WarningEvent : Event
    {
        public WarningEvent(string message)
            : base(EventType.WARNING, message)
        {
        }
    }

    //
    public class ErrorEvent : Event
    {
        public ErrorEvent(string message)
            : base(EventType.ERROR, message)
        {
        }
    }

    //
    public class FatalErrorEvent : Event
    {
        public FatalErrorEvent(string message)
            : base(EventType.FATAL_ERROR, message)
        {
        }
    }

    public class ExceptionEvent : Event
    {
        public ExceptionEvent(string message)
            : base(EventType.EXCEPTION, message)
        {
        }
    }

}
