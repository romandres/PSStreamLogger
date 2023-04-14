using System;
using System.Collections.Generic;
using System.Management.Automation;
using System.Management.Automation.Runspaces;
using System.Text;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Events;

namespace PSStreamLoggerModule
{

    [Cmdlet(VerbsLifecycle.Invoke, "CommandWithLogging")]
    public class InvokeCommandWithLoggingCmdlet : PSCmdlet, IDisposable
    {
        [Parameter(Mandatory = true)]
        public ScriptBlock? ScriptBlock { get; set; }

        [Parameter(Mandatory = true)]
        public Logger[]? Loggers { get; set; }

        [Parameter]
        public SwitchParameter UseNewRunspace { get; set; }

        private LogEventLevel minimumLogLevel = LogEventLevel.Information;
        
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
            PSDataCollection<PSObject> output = new PSDataCollection<PSObject>();

            Action exec;
            if (!UseNewRunspace.IsPresent)
            {
                var streamConfiguration = PowerShellExecutor.GetStreamConfiguration(minimumLogLevel);
                StringBuilder logLevelCommandBuilder = new StringBuilder();
                foreach (var streamConfigurationItem in streamConfiguration)
                {
                    logLevelCommandBuilder.Append($"${streamConfigurationItem.Key} = \"{streamConfigurationItem.Value}\"; ");
                }

                exec = () =>
                {
                    InvokeCommand.InvokeScript($"{logLevelCommandBuilder} & {{ {ScriptBlock} {Environment.NewLine}}} *>&1 | PSStreamLogger\\Out-PSStreamLogger -DataRecordLogger $input[0]", true, PipelineResultTypes.Output, new List<object>() { dataRecordLogger! });
                };
            }
            else
            {
                // Get current directory (Environment.CurrentDirectory does not work when the current directory was changed after starting the process)
                var currentPath = InvokeCommand.InvokeScript("Get-Location")[0].BaseObject.ToString();

                powerShellExecutor = new PowerShellExecutor(dataRecordLogger!, minimumLogLevel, currentPath);

                exec = () =>
                {
                    powerShellExecutor.Execute(ScriptBlock!.ToString(), output);
                };
            }

            try
            {
                exec.Invoke();
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
            
            minimumLogLevel = Serilog.Events.LogEventLevel.Information;
            foreach (var logger in Loggers!)
            {
                if (logger.MinimumLogLevel < minimumLogLevel)
                {
                    minimumLogLevel = logger.MinimumLogLevel;
                }

                loggerFactory.AddSerilog(logger.SerilogLogger);
            }

            scriptLogger = loggerFactory.CreateLogger("PSScriptBlock");
            dataRecordLogger = new DataRecordLogger(scriptLogger, UseNewRunspace.IsPresent ? 0 : 2);
        }
    }
}
