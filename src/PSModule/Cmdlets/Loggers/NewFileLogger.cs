using System;
using System.Globalization;
using System.IO;
using System.Management.Automation;
using Serilog;
using Serilog.Templates;

namespace PSStreamLoggerModule
{
    /// <summary>
    /// <para type="synopsis">Creates a new file logger that writes log events to plain text files.</para>
    /// <para type="description">A logger based on the Serilog.Sinks.File that writes log events to plain text files.</para>
    /// <para type="type">Cmdlet</para>
    /// </summary>
    [Cmdlet(VerbsCommon.New, Name)]
    public class NewFileLogger : NewTextLoggerCmldet
    {
        private const string Name = "FileLogger";
        
        /// <summary>
        /// <para type="description">The log file path (absolute or relative).</para>
        /// <para type="description">For relative paths the current working directory will be used as the root path.</para>
        /// </summary>
        [Parameter(Mandatory = true)]
        public string? FilePath { get; set; }

        /// <summary>
        /// <para type="description">The file size limit in bytes (default = 1GB).</para>
        /// </summary>
        [Parameter()]
        public long? FileSizeLimit { get; set; } = 1073741824; // 1GB

        /// <summary>
        /// <para type="description">The maximum number of log files to keep if rolling file is used. Older log files will automatically be cleaned up.</para>
        /// </summary>
        [Parameter()]
        public int? RetainedFileCountLimit { get; set; } = 31;

        /// <summary>
        /// <para type="description">Whether or not to create a new log file when the file size limit is reached.</para>
        /// </summary>
        [Parameter()]
        public SwitchParameter RollOnFileSizeLimit { get; set; }

        /// <summary>
        /// <para type="description">The rolling time-based interval to use for the log file.</para>
        /// <para type="description">Infinite = File will not roll (no new log file will be created) on a time-based interval.</para>
        /// </summary>
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

            WriteObject(new Logger(MinimumLogLevel, loggerConfiguration.CreateLogger(), Name));
        }
    }
}
