using System.Globalization;
using System.Management.Automation;
using Serilog;

namespace PSStreamLoggerModule
{
    [Cmdlet(VerbsCommon.New, "FileLogger")]
    public class NewFileLogger : PSCmdlet
    {
        [Parameter(Mandatory = true)]
        public string? FilePath { get; set; }

        [Parameter()]
        public int? FileSizeLimit { get; set; } = 1073741824; // 1GB

        [Parameter()]
        public int? RetainedFileCountLimit { get; set; } = 31;

        [Parameter()]
        public SwitchParameter RollOnFileSizeLimit { get; set; }

        [Parameter()]
        public RollingInterval RollingInterval { get; set; } = RollingInterval.Infinite;

        [Parameter()]
        public string OutputTemplate { get; set; } = $"[{{Timestamp:yyyy-MM-dd HH:mm:ss.fffzz}} {{Level:u3}}] {{Message:lj}}{{NewLine}}{{{DataRecordLogger.PSExtendedInfoKey}}}";

        [Parameter()]
        public string? FilterIncludeExpression { get; set; }

        [Parameter()]
        public Serilog.Events.LogEventLevel MinimumLogLevel = Serilog.Events.LogEventLevel.Information;

        protected override void EndProcessing()
        {
            var loggerConfiguration = new Serilog.LoggerConfiguration()
                .MinimumLevel.Is(MinimumLogLevel)
                .WriteTo.File(FilePath, MinimumLogLevel, OutputTemplate, formatProvider: CultureInfo.CurrentCulture, fileSizeLimitBytes: FileSizeLimit, retainedFileCountLimit: RetainedFileCountLimit, rollOnFileSizeLimit: RollOnFileSizeLimit.IsPresent, rollingInterval: RollingInterval)
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
