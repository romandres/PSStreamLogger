using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Management.Automation;
using System.Management.Automation.Runspaces;
using Microsoft.Extensions.Logging;
using Microsoft.PowerShell;
using Serilog;

namespace PSStreamLoggerModule
{

    [Cmdlet(VerbsLifecycle.Invoke, "CommandWithLogging")]
    public class InvokeCommandWithLoggingCmdlet : PSCmdlet, IDisposable
    {
        [Parameter(Mandatory = true)]
        public ScriptBlock? ScriptBlock { get; set; }

        [Parameter(Mandatory = true)]
        public Serilog.Core.Logger[]? Loggers { get; set; }

        [Parameter]
        public SwitchParameter UseSeparateScope { get; set; }

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
                            logger.Dispose();
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
            if (!UseSeparateScope.IsPresent)
            {
                exec = () =>
                {
                    return InvokeCommand.InvokeScript($"& {{ {ScriptBlock} {Environment.NewLine}}} *>&1 | PSStreamLogger\\Out-PSStreamLogger -DataRecordLogger $input[0]", false, PipelineResultTypes.Output, new List<object>() { dataRecordLogger! });
                };
            }
            else
            {
                // Get current directory
                var currentPath = InvokeCommand.InvokeScript("Get-Location")[0].BaseObject.ToString();
                // Get current execution policy
                var executionPolicy = InvokeCommand.InvokeScript("Get-ExecutionPolicy -Scope Process")[0].BaseObject as ExecutionPolicy?;

                powerShellExecutor = new PowerShellExecutor(dataRecordLogger!, currentPath, executionPolicy);

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

            foreach (var logger in Loggers!)
            {
                loggerFactory.AddSerilog(logger);
            }

            scriptLogger = loggerFactory.CreateLogger("PSScriptBlock");
            dataRecordLogger = new DataRecordLogger(scriptLogger, UseSeparateScope.IsPresent ? 0 : 2);
        }
    }
}
