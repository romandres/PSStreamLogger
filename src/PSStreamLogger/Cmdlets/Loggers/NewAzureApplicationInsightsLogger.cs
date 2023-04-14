using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;
using Microsoft.ApplicationInsights.Channel;
using Microsoft.ApplicationInsights.DataContracts;
using Serilog;
using Serilog.Events;
using Serilog.Sinks.ApplicationInsights.TelemetryConverters;
using Serilog.Templates;

namespace PSStreamLoggerModule
{
    [Cmdlet(VerbsCommon.New, "AzureApplicationInsightsLogger")]
    public class NewAzureApplicationInsightsLogger : PSCmdlet
    {
        [Parameter(Mandatory = true)]
        public string? ConnectionString { get; set; }

        [Parameter()]
        public Hashtable? Properties { get; set; }

        [Parameter()]
        public string? FilterIncludeOnlyExpression { get; set; }

        [Parameter()]
        public string? FilterExcludeExpression { get; set; }

        [Parameter()]
        public Serilog.Events.LogEventLevel MinimumLogLevel { get; set; } = Serilog.Events.LogEventLevel.Information;

        protected override void EndProcessing()
        {
            var loggerConfiguration = new Serilog.LoggerConfiguration()
            .MinimumLevel.Is(MinimumLogLevel)
                .WriteTo.ApplicationInsights(
                     connectionString: ConnectionString,
                     telemetryConverter: (new AzureApplicationInsightsTraceTelemetryConverter(Properties?.Cast<DictionaryEntry>().ToDictionary(x => x.Key.ToString(), x => x.Value.ToString()))),
                     restrictedToMinimumLevel: MinimumLogLevel)
                .Enrich.FromLogContext();

            if (FilterIncludeOnlyExpression is object)
            {
                loggerConfiguration = loggerConfiguration
                    .Filter.ByIncludingOnly(FilterIncludeOnlyExpression);
            }

            if (FilterExcludeExpression is object)
            {
                loggerConfiguration = loggerConfiguration
                    .Filter.ByExcluding(FilterExcludeExpression);
            }

            WriteObject(new Logger(MinimumLogLevel, loggerConfiguration.CreateLogger()));
        }
    }
}
