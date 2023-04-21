using System;
using System.Collections.Generic;
using Microsoft.ApplicationInsights.Channel;
using Serilog.Events;
using Serilog.Sinks.ApplicationInsights.TelemetryConverters;

namespace PSStreamLoggerModule
{
    internal class AzureApplicationInsightsTraceTelemetryConverter : TraceTelemetryConverter
    {
        private readonly IDictionary<string, string>? properties;

        public AzureApplicationInsightsTraceTelemetryConverter(IDictionary<string, string>? properties)
        {
            this.properties = properties;
        }

        public override IEnumerable<ITelemetry> Convert(LogEvent logEvent, IFormatProvider formatProvider)
        {
            foreach (ITelemetry telemetry in base.Convert(logEvent, formatProvider))
            {
                if (properties is object)
                {
                    foreach (var property in properties)
                    {
                        telemetry.Context.GlobalProperties.Add(property.Key, property.Value);
                    }
                }

                yield return telemetry;
            }
        }
    }
}
