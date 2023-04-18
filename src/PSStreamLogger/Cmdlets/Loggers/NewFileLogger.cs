using System;
using System.Globalization;
using System.IO;
using System.Management.Automation;
using Serilog;
using Serilog.Templates;

namespace PSStreamLoggerModule
{
    [Cmdlet(VerbsCommon.New, "FileLogger")]
    public class NewFileLogger : NewTextLoggerCmldet
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

        protected override void EndProcessing()
        {
            string filePath = FilePath!;
            if (!Path.IsPathRooted(filePath))
            {
                filePath = Path.Combine(SessionState.Path.CurrentFileSystemLocation.Path, filePath);
            }

            var loggerConfiguration = new Serilog.LoggerConfiguration()
                .MinimumLevel.Is(MinimumLogLevel)
                .WriteTo.File(
                    path: filePath,
                    formatter: new ExpressionTemplate(template: ExpressionTemplate, formatProvider: CultureInfo.CurrentCulture),
                    fileSizeLimitBytes: FileSizeLimit,
                    retainedFileCountLimit: RetainedFileCountLimit,
                    rollOnFileSizeLimit: RollOnFileSizeLimit.IsPresent,
                    rollingInterval: RollingInterval,
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
