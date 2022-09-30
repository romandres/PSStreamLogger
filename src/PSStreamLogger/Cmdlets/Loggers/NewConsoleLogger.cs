using System;
using System.Globalization;
using System.Management.Automation;
using System.Runtime.Serialization;
using Serilog;
using Serilog.Templates;

namespace PSStreamLoggerModule
{
    [Cmdlet(VerbsCommon.New, "ConsoleLogger")]
    public class NewConsoleLogger : PSCmdlet
    {
        [Parameter()]
        public string ExpressionTemplate { get; set; } = $"[{{@t:yyyy-MM-dd HH:mm:ss.fffzz}} {{@l:u3}}] {{@m:lj}}{Environment.NewLine}{{{DataRecordLogger.PSExtendedInfoKey}}}";

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
                .WriteTo.Console(
                    formatter: new ExpressionTemplate(template: ExpressionTemplate, theme: Serilog.Templates.Themes.TemplateTheme.Code),
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

            WriteObject(loggerConfiguration.CreateLogger());
        }
    }
}
