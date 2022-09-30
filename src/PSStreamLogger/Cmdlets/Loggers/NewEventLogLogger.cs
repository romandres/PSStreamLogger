using System;
using System.Globalization;
using System.Management.Automation;
using Serilog;
using Serilog.Templates;

namespace PSStreamLoggerModule
{
    [Cmdlet(VerbsCommon.New, "EventLogLogger")]
    public class NewEventLogLogger : PSCmdlet
    {
        [Parameter(Mandatory = true)]
        public string? LogSource { get; set; }

        [Parameter()]
        public string? LogName { get; set; }

        [Parameter()]
        public string ExpressionTemplate { get; set; } = $"{{@m:lj}}{Environment.NewLine}{{{DataRecordLogger.PSExtendedInfoKey}}}";

        [Parameter()]
        public string? FilterIncludeOnlyExpression { get; set; }

        [Parameter()]
        public string? FilterExcludeExpression { get; set; }

        [Parameter()]
        public ScriptBlock? EventIdProvider { get; set; }

        [Parameter()]
        public Serilog.Events.LogEventLevel MinimumLogLevel { get; set; } = Serilog.Events.LogEventLevel.Information;

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

            WriteObject(loggerConfiguration.CreateLogger());
        }
    }
}
