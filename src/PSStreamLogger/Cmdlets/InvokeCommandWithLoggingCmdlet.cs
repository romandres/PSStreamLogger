using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Management.Automation;
using System.Management.Automation.Runspaces;
using System.Text;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Events;

namespace PSStreamLoggerModule
{
    /// <summary>
    /// <para type="synopsis">Executes a command and logs PowerShell stream output.</para>
    /// <para type="description">Executes a command and sends data written into PowerShell streams (Verbose, Debug, Information, Warning, Error) as log events to the configured loggers.</para>
    /// <para type="description">Output is passed through.</para>
    /// <para type="type">Cmdlet</para>
    /// </summary>
    [Cmdlet(VerbsLifecycle.Invoke, "CommandWithLogging")]
    public class InvokeCommandWithLoggingCmdlet : PSCmdlet, IDisposable
    {
        /// <summary>
        /// <para type="description">The script block to execute.</para>
        /// </summary>
        [Parameter(Mandatory = true)]
        public ScriptBlock? ScriptBlock { get; set; }

        /// <summary>
        /// <para type="description">The loggers to send log events to. If no loggers are configured, a console logger with minimum log level information will be used.</para>
        /// </summary>
        [Parameter()]
        public Logger[]? Loggers { get; set; }

        /// <summary>
        /// <para type="description">The mode to execute the script block. NewScope executes the script block in a new scope (default), CurrentScope executes it in the current scope (in the same scope this Cmdlet is executed from) and NewRunspace executes it in a new PowerShell runspace.</para>
        /// </summary>
        [Parameter]
        public RunMode RunMode { get; set; } = RunMode.NewScope;

        /// <summary>
        /// <para type="description">Disable the automatic PowerShell stream configuration. based on the lowest log level.</para>
        /// <para type="description">By default the PowerShell streams are configured based on the lowest log level across all loggers.</para>
        /// </summary>
        [Parameter]
        public SwitchParameter DisableStreamConfiguration { get; set; }

        private LogEventLevel minimumLogLevel;
        
        private ILoggerFactory? loggerFactory;

        private Microsoft.Extensions.Logging.ILogger? scriptLogger;

        private bool disposed;

        private DataRecordLogger? dataRecordLogger;

        private PowerShellExecutor? powerShellExecutor;

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposed)
            {
                if (disposing)
                {
                    loggerFactory?.Dispose();

                    if (Loggers is object)
                    {
                        foreach (var logger in Loggers)
                        {
                            logger.SerilogLogger.Dispose();
                        }
                    }

                    powerShellExecutor?.Dispose();
                }

                disposed = true;
            }
        }

        protected override void BeginProcessing()
        {
            PrepareLogging();
        }

        protected override void EndProcessing()
        {
            Func<Collection<PSObject>> exec;

            var streamConfiguration = !DisableStreamConfiguration.IsPresent ? new PSStreamConfiguration(minimumLogLevel) : null;

            if (RunMode != RunMode.NewRunspace)
            {
                StringBuilder logLevelCommandBuilder = new StringBuilder();
                
                if (streamConfiguration is object)
                {
                    foreach (var streamConfigurationItem in streamConfiguration.StreamConfiguration)
                    {
                        logLevelCommandBuilder.Append($"${streamConfigurationItem.Key} = \"{streamConfigurationItem.Value}\"; ");
                    }
                }

                exec = () =>
                {
                    return InvokeCommand.InvokeScript($"{logLevelCommandBuilder}& {{ {ScriptBlock} {Environment.NewLine}}} *>&1 | PSStreamLogger\\Out-PSStreamLogger -DataRecordLogger $input[0]", RunMode == RunMode.NewScope, PipelineResultTypes.Output, new List<object>() { dataRecordLogger! });
                };
            }
            else
            {
                string currentPath = SessionState.Path.CurrentLocation.Path;
                powerShellExecutor = new PowerShellExecutor(dataRecordLogger!, streamConfiguration, currentPath);

                exec = () =>
                {
                    return powerShellExecutor.Execute(ScriptBlock!.ToString());
                };
            }

            try
            {
                var output = exec.Invoke();
                WriteObject(output, true);
            }
            catch (RuntimeException ex)
            {
                dataRecordLogger!.LogRecord(ex.ErrorRecord);
                throw;
            }
        }

        private void PrepareLogging()
        {
            loggerFactory = new LoggerFactory();
            
            minimumLogLevel = Logger.DefaultMinimumLogLevel;

            Loggers ??= new[]
            {
                NewConsoleLogger.CreateDefaultLogger()
            };
            
            foreach (var logger in Loggers!)
            {
                if (logger.MinimumLogLevel < minimumLogLevel)
                {
                    minimumLogLevel = logger.MinimumLogLevel;
                }

                loggerFactory.AddSerilog(logger.SerilogLogger);
            }

            scriptLogger = loggerFactory.CreateLogger("PSScriptBlock");
            dataRecordLogger = new DataRecordLogger(scriptLogger, RunMode == RunMode.NewRunspace ? 0 : 2);
        }
    }
}
