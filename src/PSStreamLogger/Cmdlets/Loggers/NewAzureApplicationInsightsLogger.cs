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
    /// <summary>
    /// <para type="synopsis">Creates a new logger that writes log events to an Azure Application Insights instance.</para>
    /// <para type="description">A logger based on the Serilog.Sinks.ApplicationInsights that writes log events to an Azure Application Insights instance.</para>
    /// <para type="type">Cmdlet</para>
    /// </summary>
    [Cmdlet(VerbsCommon.New, Name)]
    public class NewAzureApplicationInsightsLogger : NewLoggerCmdlet
    {
        private const string Name = "AzureApplicationInsightsLogger";
        
        /// <summary>
        /// <para type="description">The connection string for the target Azure Application Insights instance.</para>
        /// </summary>
        [Parameter(Mandatory = true)]
        public string? ConnectionString { get; set; }

        /// <summary>
        /// <para type="description">An optional hashtable containing additional properties to add to each log event (key = log property name, value = log property value).</para>
        /// </summary>
        [Parameter()]
        public Hashtable? Properties { get; set; }

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

            WriteObject(new Logger(MinimumLogLevel, loggerConfiguration.CreateLogger(), Name));
        }
    }
}
