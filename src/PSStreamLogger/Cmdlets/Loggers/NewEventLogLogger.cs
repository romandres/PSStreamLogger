using System.Management.Automation;
using Serilog;

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
        public string OutputTemplate { get; set; } = $"{{Message:lj}}{{NewLine}}{{{DataRecordLogger.PSExtendedInfoKey}}}";

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
                .WriteTo.EventLog(LogSource, LogName, outputTemplate: OutputTemplate, eventIdProvider: EventIdProvider is object ? new EventIdScriptBlockProvider(EventIdProvider) : null)
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
