using System;
using System.Collections;
using System.Globalization;
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
            if (logEvent is null)
            {
                throw new ArgumentNullException(nameof(logEvent));
            }

            ushort eventId = ushort.MinValue;

            try
            {
                var properties = new Hashtable((IDictionary)logEvent.Properties);
                var output = eventIdScriptBlock.Invoke(properties);

                eventId = Convert.ToUInt16(output[0].BaseObject, CultureInfo.InvariantCulture);
            }
            catch (FormatException)
            {
                // TODO: Log FormatException
            }
            catch (InvalidCastException)
            {
                // TODO: Log InvalidCastException
            }
            catch (OverflowException)
            {
                // TODO: Log OverflowException
            }

            return eventId;
        }
    }
}
