using System;
using System.Collections.Generic;
using Microsoft.ApplicationInsights.Channel;
using Serilog.Events;
using Serilog.Sinks.ApplicationInsights.TelemetryConverters;

namespace PSStreamLoggerModule
{
    internal class CustomConverter : TraceTelemetryConverter
    {
        private readonly string? scriptName;

        public CustomConverter(string? scriptName)
        {
            this.scriptName = scriptName;
        }

        public override IEnumerable<ITelemetry> Convert(LogEvent logEvent, IFormatProvider formatProvider)
        {
            foreach (ITelemetry telemetry in base.Convert(logEvent, formatProvider))
            {
                if (!string.IsNullOrWhiteSpace(scriptName))
                {
                    telemetry.Context.GlobalProperties.Add("ScriptName", scriptName);
                }

                yield return telemetry;
            }
        }
    }
}
