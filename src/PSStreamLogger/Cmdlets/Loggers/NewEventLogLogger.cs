using System;
using System.Globalization;
using System.Management.Automation;
using Serilog;
using Serilog.Templates;

namespace PSStreamLoggerModule
{
    [Cmdlet(VerbsCommon.New, "EventLogLogger")]
    public class NewEventLogLogger : NewTextLoggerCmldet
    {
        public NewEventLogLogger()
        {
            ExpressionTemplate = $"{{@m:lj}}{Environment.NewLine}{{{DataRecordLogger.PSErrorDetailsKey}}}";
        }
        
        [Parameter(Mandatory = true)]
        public string? LogSource { get; set; }

        [Parameter()]
        public string? LogName { get; set; }
        
        [Parameter()]
        public ScriptBlock? EventIdProvider { get; set; }
        
        protected override void EndProcessing()
        {
            var loggerConfiguration = new Serilog.LoggerConfiguration()
                .MinimumLevel.Is(MinimumLogLevel)
                .WriteTo.EventLog(
                    source: LogSource,
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
