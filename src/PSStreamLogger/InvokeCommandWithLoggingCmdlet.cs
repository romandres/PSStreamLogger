using System;
using System.Globalization;
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
        public string? LogFilePath { get; set; }

        [Parameter]
        public ActionPreference DebugAction { get; set; } = ActionPreference.Inquire;

        private ILoggerFactory? loggerFactory;

        private Microsoft.Extensions.Logging.ILogger? logger;
        private Microsoft.Extensions.Logging.ILogger? scriptLogger;

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

            logger.LogTrace("Invoke-CommandWithLogging started");

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
                string newLineScriptStart = ScriptBlock.ToString().StartsWith("\n") ? string.Empty : "\n";
                string newLineScriptEnd = ScriptBlock.ToString().EndsWith("\n") ? string.Empty : "\n";

                // Use ScriptBlock
                //var scriptBlock = InvokeCommand.NewScriptBlock($"& {{[CmdletBinding()]param() {(isDebugEnabled ? $"$DebugPreference = $args[1];" : string.Empty)}try {{ {start}{ScriptBlock}{end} }} catch {{ $PSCmdlet.ThrowTerminatingError($_); }} }}{commonParameters} *>&1 | PSStreamLogger\\Out-PSStreamLogger -Logger $args[0]{commonParameters}");
                //var output = InvokeCommand.InvokeScript(false, scriptBlock, null, scriptLogger, DebugAction);

                // Use script
                var output = InvokeCommand.InvokeScript($"& {{[CmdletBinding()]param() {(isDebugEnabled ? $"$DebugPreference = $args[1];" : string.Empty)}try {{ {newLineScriptStart}{ScriptBlock}{newLineScriptEnd} }} catch {{ $PSCmdlet.ThrowTerminatingError($_); }} }}{commonParameters} *>&1 | PSStreamLogger\\Out-PSStreamLogger -Logger $args[0]{commonParameters}", scriptLogger);

                // Write script output to output stream
                WriteObject(output, true);
            }
            catch (RuntimeException ex)
            {
                DataRecordLogger.LogRecord(scriptLogger, ex.ErrorRecord);
                ThrowTerminatingError(ex.ErrorRecord);
            }
            finally
            {
                logger.LogTrace("Invoke-CommandWithLogging finished");
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
