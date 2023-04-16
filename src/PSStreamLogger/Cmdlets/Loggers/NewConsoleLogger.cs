using System;
using System.Management.Automation;
using Serilog;
using Serilog.Events;
using Serilog.Templates;

namespace PSStreamLoggerModule
{
    [Cmdlet(VerbsCommon.New, "ConsoleLogger")]
    public class NewConsoleLogger : PSCmdlet
    {
        [Parameter()]
        public string ExpressionTemplate { get; set; } = Logger.DefaultExpressionTemplate;

        [Parameter()]
        public string? FilterIncludeOnlyExpression { get; set; }

        [Parameter()]
        public string? FilterExcludeExpression { get; set; }

        [Parameter()]
        public Serilog.Events.LogEventLevel MinimumLogLevel { get; set; } = Logger.DefaultMinimumLogLevel;

        protected override void EndProcessing()
        {
            var loggerConfiguration = CreateLoggerConfiguration(ExpressionTemplate, MinimumLogLevel);

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

        public static LoggerConfiguration CreateLoggerConfiguration(string expressionTemplate, LogEventLevel minimumLogLevel)
        {
            return new Serilog.LoggerConfiguration()
                .MinimumLevel.Is(minimumLogLevel)
                .WriteTo.Console(
                    formatter: new ExpressionTemplate(template: Logger.DefaultExpressionTemplate, theme: Serilog.Templates.Themes.TemplateTheme.Code),
                    restrictedToMinimumLevel: minimumLogLevel)
                .Enrich.FromLogContext();
        }
        
        public static Logger CreateDefaultLogger()
        {
            var minimumLogLevel = Logger.DefaultMinimumLogLevel;

            var loggerConfiguration = CreateLoggerConfiguration(Logger.DefaultExpressionTemplate, Logger.DefaultMinimumLogLevel);
            return new Logger(minimumLogLevel, loggerConfiguration.CreateLogger());
        }
    }
}
