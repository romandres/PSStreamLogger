using System;
using System.Management.Automation;
using Microsoft.Extensions.Logging;
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
        public ActionPreference DebugAction { get; set; } = ActionPreference.Inquire;

        private ILoggerFactory? loggerFactory;

        private Microsoft.Extensions.Logging.ILogger? scriptLogger;

        private bool isVerboseEnabled = false;
        private bool isDebugEnabled = false;

        private ActionPreference? informationActionPreference;
        private ActionPreference? warningActionPreference;
        private ActionPreference? errorActionPreference;

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

            if (MyInvocation.BoundParameters.ContainsKey("Verbose") && ((SwitchParameter)MyInvocation.BoundParameters["Verbose"]).IsPresent)
            {
                isVerboseEnabled = true;
            }

            if (MyInvocation.BoundParameters.ContainsKey("Debug") && ((SwitchParameter)this.MyInvocation.BoundParameters["Debug"]).IsPresent)
            {
                isDebugEnabled = true;
            }

            if (MyInvocation.BoundParameters.ContainsKey("InformationAction"))
            {
                informationActionPreference = this.MyInvocation.BoundParameters["InformationAction"] as ActionPreference?;
            }

            if (MyInvocation.BoundParameters.ContainsKey("ErrorAction"))
            {
                errorActionPreference = this.MyInvocation.BoundParameters["ErrorAction"] as ActionPreference?;
            }

            if (MyInvocation.BoundParameters.ContainsKey("WarningAction"))
            {
                warningActionPreference = this.MyInvocation.BoundParameters["WarningAction"] as ActionPreference?;
            }

            // Determine the variable name for script arguments (different for Windows PowerShell and PowerShell Core)
            scriptArgumentVariableName = InvokeCommand.InvokeScript("if ($args[0]) { \"`$args\" } else { \"`$input\" }", new bool[] { true })[0].BaseObject.ToString();
        }

        protected override void EndProcessing()
        {
            string commonParameters = $"{(isVerboseEnabled ? " -Verbose" : string.Empty)}{(isDebugEnabled ? " -Debug" : string.Empty)}{(informationActionPreference.HasValue ? $" -InformationAction {informationActionPreference}" : string.Empty)}{(warningActionPreference.HasValue ? $" -WarningAction {warningActionPreference}" : string.Empty)}{(errorActionPreference.HasValue ? $" -ErrorAction {errorActionPreference}" : string.Empty)}";

            try
            {
                var output = InvokeCommand.InvokeScript($"& {{[CmdletBinding()]param() {(isDebugEnabled ? $"$DebugPreference = {scriptArgumentVariableName}[1];" : string.Empty)}try {{ {ScriptBlock} }} catch {{ $PSCmdlet.ThrowTerminatingError($_); }} }}{commonParameters} *>&1 | PSStreamLogger\\Out-PSStreamLogger -Logger {scriptArgumentVariableName}[0]{commonParameters}", scriptLogger, DebugAction);

                // Write script output to output stream
                WriteObject(output, true);
            }
            catch (RuntimeException ex)
            {
                dataRecordLogger!.LogRecord(ex.ErrorRecord);
                ThrowTerminatingError(ex.ErrorRecord);
            }
        }

        private void PrepareLogging()
        {
            loggerFactory = new LoggerFactory();

            foreach (var logger in Loggers!)
            {
                loggerFactory.AddSerilog(logger);
            }

            scriptLogger = loggerFactory.CreateLogger("PSScript");
            dataRecordLogger = new DataRecordLogger(scriptLogger);
        }
    }
}
