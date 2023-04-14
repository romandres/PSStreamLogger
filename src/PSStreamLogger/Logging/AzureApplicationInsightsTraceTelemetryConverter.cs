using System;
using System.Collections.Generic;
using Microsoft.ApplicationInsights.Channel;
using Serilog.Events;
using Serilog.Sinks.ApplicationInsights.TelemetryConverters;

namespace PSStreamLoggerModule
{
    internal class AzureApplicationInsightsTraceTelemetryConverter : TraceTelemetryConverter
    {
        private readonly string? scriptName;

        public AzureApplicationInsightsTraceTelemetryConverter(string? scriptName)
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
