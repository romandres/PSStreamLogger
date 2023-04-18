using System;
using System.Globalization;
using System.Management.Automation;
using Serilog;
using Serilog.Templates;

namespace PSStreamLoggerModule
{
    /// <summary>
    /// <para type="synopsis">Creates a new event log logger that writes log events to a Windows EventLog.</para>
    /// <para type="description">A logger based on the Serilog.Sinks.EventLog that writes log events to a Windows EventLog.</para>
    /// <para type="type">Cmdlet</para>
    /// </summary>
    [Cmdlet(VerbsCommon.New, "EventLogLogger")]
    public class NewEventLogLogger : NewTextLoggerCmldet
    {
        public NewEventLogLogger()
        {
            ExpressionTemplate = $"{{@m:lj}}{Environment.NewLine}{{{DataRecordLogger.PSErrorDetailsKey}}}";
        }
        
        /// <summary>
        /// <para type="description">The event source that is typically name of the application/program writing log events to the event log.</para>
        /// <para type="description">The logger will not create the event source. The event source has to exist for the logger to work (can be created with elevated privileges using New-EventLog).</para>
        /// </summary>
        [Parameter(Mandatory = true)]
        public string? Source { get; set; }

        /// <summary>
        /// <para type="description">The name of the target event log to which log events are written.</para>
        /// <para type="description">The logger will not create the event log. The event log has to exist for the logger to work (can be created with elevated privileges using New-EventLog).</para>
        /// </summary>
        [Parameter()]
        public string? LogName { get; set; }
        
        /// <summary>
        /// <para type="description">A script block that provides the event ID for each log event based on the log event's properties.</para>
        /// </summary>
        [Parameter()]
        public ScriptBlock? EventIdProvider { get; set; }
        
        protected override void EndProcessing()
        {
            var loggerConfiguration = new Serilog.LoggerConfiguration()
                .MinimumLevel.Is(MinimumLogLevel)
                .WriteTo.EventLog(
                    source: Source,
                    logName: LogName,
                    formatter: new ExpressionTemplate(template: ExpressionTemplate, formatProvider: CultureInfo.CurrentCulture),
                    restrictedToMinimumLevel: MinimumLogLevel,
                    eventIdProvider: EventIdProvider is object
                        ? new EventIdScriptBlockProvider(EventIdProvider)
                        : null)
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
