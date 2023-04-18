using System;
using System.Management.Automation;
using Serilog;
using Serilog.Events;
using Serilog.Templates;

namespace PSStreamLoggerModule
{
    /// <summary>
    /// <para type="synopsis">Creates a new console logger that writes log events to the console.</para>
    /// <para type="description">A console logger (based on the Serilog.Sinks.Console) writes log events to the console via standard output.</para>
    /// <para type="type">Cmdlet</para>
    /// </summary>
    [Cmdlet(VerbsCommon.New, "ConsoleLogger")]
    public class NewConsoleLogger : NewTextLoggerCmldet
    {
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

        private static LoggerConfiguration CreateLoggerConfiguration(string expressionTemplate, LogEventLevel minimumLogLevel)
        {
            return new Serilog.LoggerConfiguration()
                .MinimumLevel.Is(minimumLogLevel)
                .WriteTo.Console(
                    formatter: new ExpressionTemplate(template: Logger.DefaultExpressionTemplate, theme: Serilog.Templates.Themes.TemplateTheme.Code),
                    restrictedToMinimumLevel: minimumLogLevel)
                .Enrich.FromLogContext();
        }
        
        internal static Logger CreateDefaultLogger()
        {
            var minimumLogLevel = Logger.DefaultMinimumLogLevel;

            var loggerConfiguration = CreateLoggerConfiguration(Logger.DefaultExpressionTemplate, Logger.DefaultMinimumLogLevel);
            return new Logger(minimumLogLevel, loggerConfiguration.CreateLogger());
        }
    }
}
