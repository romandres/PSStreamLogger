using System;
using System.Collections;
using System.Management.Automation;
using Serilog.Events;
using Serilog.Sinks.EventLog;

namespace PSStreamLoggerModule
{
    public class EventIdScriptBlockProvider : IEventIdProvider
    {
        private readonly ScriptBlock eventIdScriptBlock;

        public EventIdScriptBlockProvider(ScriptBlock eventIdScriptBlock)
        {
            this.eventIdScriptBlock = eventIdScriptBlock;
        }

        public ushort ComputeEventId(LogEvent logEvent)
        {
            try
            {
                var properties = new Hashtable((IDictionary)logEvent.Properties);
                var output = eventIdScriptBlock.Invoke(properties);

                return Convert.ToUInt16(output[0].BaseObject);
            }
            catch
            {
                return ushort.MinValue;
            }
        }
    }
}
