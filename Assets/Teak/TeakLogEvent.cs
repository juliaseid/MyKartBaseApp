using System;
using System.Collections.Generic;

public class TeakLogEvent {

    public enum Level {
        VERBOSE,
        INFO,
        WARN,
        ERROR
    }

    public string RunId { get; private set; }
    public long   EventId { get; private set; }
    public long   TimeStamp { get; private set; }
    public string EventType { get; private set; }
    public Level  LogLevel { get; private set; }

    public Dictionary<string, object> EventData { get; private set; }

    public TeakLogEvent(Dictionary<string, object> logData) {
        this.RunId = logData["run_id"] as string;
        this.EventId = (long) logData["event_id"];
        this.TimeStamp = (long) logData["timestamp"];
        this.EventType = logData["event_type"] as string;
        this.LogLevel = (Level) Enum.Parse(typeof(Level), logData["log_level"] as string, true);
        this.EventData = logData.ContainsKey("event_data") ? logData["event_data"] as Dictionary<string, object> : null;
    }

    public override string ToString() {
        string formatString = "{{ RunId = '{0}', EventId = '{1}', TimeStamp = '{2}', EventType = '{3}', LogLevel = '{4}'{5} }}";
        string eventDataString = "";
        return string.Format(formatString,
                             this.RunId,
                             this.EventId,
                             this.TimeStamp,
                             this.EventType,
                             this.LogLevel,
                             eventDataString
                            );
    }
}
