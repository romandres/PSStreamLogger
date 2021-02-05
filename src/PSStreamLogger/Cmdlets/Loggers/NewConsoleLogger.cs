using System.Globalization;
using System.Management.Automation;
using Serilog;

namespace PSStreamLoggerModule
{
    [Cmdlet(VerbsCommon.New, "ConsoleLogger")]
    public class NewConsoleLogger : PSCmdlet
    {
        [Parameter()]
        public string OutputTemplate { get; set; } = $"[{{Timestamp:yyyy-MM-dd HH:mm:ss.fffzz}} {{Level:u3}}] {{Message:lj}}{{NewLine}}{{{DataRecordLogger.PSExtendedInfoKey}}}";

        [Parameter()]
        public string? FilterIncludeExpression { get; set; }

        protected override void EndProcessing()
        {
            var loggerConfiguration = new Serilog.LoggerConfiguration()
                .MinimumLevel.Is(Serilog.Events.LogEventLevel.Verbose)
                .WriteTo.Console(Serilog.Events.LogEventLevel.Verbose, OutputTemplate, formatProvider: CultureInfo.CurrentCulture)
                .Enrich.FromLogContext();

            if (FilterIncludeExpression is object)
            {
                loggerConfiguration = loggerConfiguration
                    .Filter.ByIncludingOnly(FilterIncludeExpression);
            }

            WriteObject(loggerConfiguration.CreateLogger());
        }
    }
}
