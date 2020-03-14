using System;
using System.Globalization;
using System.Management.Automation;
using Microsoft.Extensions.Logging;
using Serilog;

namespace PSStreamLogger
{
    [Cmdlet(VerbsLifecycle.Invoke, "CommandWithLogging")]
    public class InvokeCommandWithLoggingCmdlet : PSCmdlet, IDisposable
    {
        [Parameter(Mandatory = true)]
        public ScriptBlock ScriptBlock { get; set; }

        [Parameter(Mandatory = true)]
        public string LogFilePath { get; set; }

        [Parameter]
        public ActionPreference DebugAction { get; set; } = ActionPreference.Inquire;

        private ILoggerFactory loggerFactory;

        private Microsoft.Extensions.Logging.ILogger logger;
        private Microsoft.Extensions.Logging.ILogger scriptLogger;

        private bool isVerboseEnabled = false;
        private bool isDebugEnabled = false;

        private ActionPreference? informationActionPreference;
        private ActionPreference? warningActionPreference;
        private ActionPreference? errorActionPreference;

        private bool disposed = false;

        protected override void BeginProcessing()
        {
            // Configure Serilog console and file logger
            var serilogLogger = new Serilog.LoggerConfiguration()
                .MinimumLevel.Is(Serilog.Events.LogEventLevel.Verbose)
                .WriteTo.Console(Serilog.Events.LogEventLevel.Verbose, "[{Timestamp:yyyy-MM-dd HH:mm:ss} {Level:u3}] {Message:lj} {PSInvocationInfo}{NewLine}{PSExtendedInfo}", formatProvider: CultureInfo.CurrentCulture).Enrich.FromLogContext()
                .WriteTo.File(LogFilePath, Serilog.Events.LogEventLevel.Verbose, "[{Timestamp:yyyy-MM-dd HH:mm:ss} {Level:u3}] {Message:lj} {PSInvocationInfo}{NewLine}{PSExtendedInfo}", formatProvider: CultureInfo.CurrentCulture).Enrich.FromLogContext()
                .CreateLogger();

            loggerFactory = new LoggerFactory();
            loggerFactory.AddSerilog(serilogLogger, true);

            logger = loggerFactory.CreateLogger("PSStreamLogger");
            scriptLogger = loggerFactory.CreateLogger("PSScript");

            logger.LogTrace("Invoke-CommandWithLogger started");

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
        }

        protected override void EndProcessing()
        {
            string commonParameters = $"{(isVerboseEnabled ? " -Verbose" : string.Empty)}{(isDebugEnabled ? " -Debug" : string.Empty)}{(informationActionPreference.HasValue ? $" -InformationAction {informationActionPreference}" : string.Empty)}{(warningActionPreference.HasValue ? $" -WarningAction {warningActionPreference}" : string.Empty)}{(errorActionPreference.HasValue ? $" -ErrorAction {errorActionPreference}" : string.Empty)}";

            try
            {
                var scriptArgumentVariableName = this.Host.Version.Major >= 7 ? "$args" : "$input";

                //var scriptBlock = InvokeCommand.NewScriptBlock($"& {{[CmdletBinding()]param() {(isDebugEnabled ? $"$DebugPreference = [System.Management.Automation.ActionPreference]::{DebugAction};" : string.Empty)}try {{ {ScriptBlock} }} catch {{ $PSCmdlet.ThrowTerminatingError($_); }} }}{commonParameters} *>&1 | PSPipelineLoggerModule\\Out-Logger -LoggerFactory $input[0]{commonParameters}");
                //var output = InvokeCommand.InvokeScript(false, scriptBlock, new List<object>() { loggerFactory });
                var output = InvokeCommand.InvokeScript($"& {{[CmdletBinding()]param() {(isDebugEnabled ? $"$DebugPreference = [System.Management.Automation.ActionPreference]::{DebugAction};" : string.Empty)}try {{ {ScriptBlock} }} catch {{ $PSCmdlet.ThrowTerminatingError($_); }} }}{commonParameters} *>&1 | PSStreamLogger\\Out-PSStreamLogger -Logger {scriptArgumentVariableName}[0]{commonParameters}", scriptLogger);
                //var output = InvokeCommand.InvokeScript($"& {{ {ScriptBlock} }}{commonParameters} *>&1 | PSStreamLogger\\Out-PSStreamLogger -Logger $input[0]{commonParameters}", scriptLogger);

                //var output = InvokeCommand.InvokeScript("Write-Output 'hello'");
                WriteObject(output, true);
            }
            catch (RuntimeException ex)
            {
                DataRecordLogger.LogRecord(scriptLogger, ex.ErrorRecord);
                ThrowTerminatingError(ex.ErrorRecord);
            }
            finally
            {
                logger.LogTrace("Invoke-CommandWithLogger finished");
            }
        }

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
                }

                disposed = true;
            }
        }
    }
}
