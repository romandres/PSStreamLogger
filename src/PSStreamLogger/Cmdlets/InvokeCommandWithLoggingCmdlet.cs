using System;
using System.Collections.ObjectModel;
using System.Management.Automation;
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
        public ActionPreference DebugAction { get; set; } = ActionPreference.Continue;

        [Parameter]
        public SwitchParameter UseStreamRedirection;

        private ILoggerFactory? loggerFactory;

        private Microsoft.Extensions.Logging.ILogger? scriptLogger;

        private bool disposed = false;

        private string? scriptArgumentVariableName;

        private DataRecordLogger? dataRecordLogger;

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
                    foreach (var logger in Loggers!)
                    {
                        logger.Dispose();
                    }
                }

                disposed = true;
            }
        }

        protected override void BeginProcessing()
        {
            PrepareLogging();

            // Determine the variable name for script arguments (different for Windows PowerShell and PowerShell Core)
            scriptArgumentVariableName = InvokeCommand.InvokeScript("if ($args[0]) { \"`$args\" } else { \"`$input\" }", new bool[] { true })[0].BaseObject.ToString();
        }

        protected override void EndProcessing()
        {
            Func<Collection<PSObject>> exec;
            if (UseStreamRedirection.IsPresent)
            {
                exec = () => { return InvokeCommand.InvokeScript($"& {{ {ScriptBlock} {Environment.NewLine}}} *>&1 | PSStreamLogger\\Out-PSStreamLogger -DataRecordLogger {scriptArgumentVariableName}[0]", dataRecordLogger); };
            }
            else
            {
                var currentPath = InvokeCommand.InvokeScript("Get-Location")[0].BaseObject.ToString();
                var executionPolicy = InvokeCommand.InvokeScript("Get-ExecutionPolicy -Scope Process")[0].BaseObject as ExecutionPolicy?;

                var psExec = new PowerShellExecutor(dataRecordLogger!, currentPath, executionPolicy);
                exec = () => { return psExec.Execute(ScriptBlock!.ToString()); };
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
            dataRecordLogger = new DataRecordLogger(scriptLogger, UseStreamRedirection.IsPresent ? 2 : 0);
        }
    }
}
