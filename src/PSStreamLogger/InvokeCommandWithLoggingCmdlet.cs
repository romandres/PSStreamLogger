﻿using System;
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

        [Parameter]
        public SwitchParameter IncludeInvocationInfo { get; set; }

        private ILoggerFactory? loggerFactory;

        private Microsoft.Extensions.Logging.ILogger? scriptLogger;

        private bool isVerboseEnabled = false;
        private bool isDebugEnabled = false;

        private ActionPreference? informationActionPreference;
        private ActionPreference? warningActionPreference;
        private ActionPreference? errorActionPreference;

        private bool disposed = false;

        protected override void BeginProcessing()
        {
            string logFormat = $"[{{Timestamp:yyyy-MM-dd HH:mm:ss}} {{Level:u3}}] {{Message:lj}}{(IncludeInvocationInfo.IsPresent ? " {PSInvocationInfo}" : string.Empty)}{{NewLine}}{{PSExtendedInfo}}";

            // Configure Serilog console and file logger
            var serilogLogger = new Serilog.LoggerConfiguration()
                .MinimumLevel.Is(Serilog.Events.LogEventLevel.Verbose)
                .WriteTo.Console(Serilog.Events.LogEventLevel.Verbose, logFormat, formatProvider: CultureInfo.CurrentCulture).Enrich.FromLogContext()
                .WriteTo.File(LogFilePath, Serilog.Events.LogEventLevel.Verbose, logFormat, formatProvider: CultureInfo.CurrentCulture).Enrich.FromLogContext()
                .CreateLogger();

            loggerFactory = new LoggerFactory();
            loggerFactory.AddSerilog(serilogLogger, true);

            scriptLogger = loggerFactory.CreateLogger("PSScript");

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
                string newLineScriptStart = ScriptBlock.ToString().StartsWith("\n") ? string.Empty : "\n";
                string newLineScriptEnd = ScriptBlock.ToString().EndsWith("\n") ? string.Empty : "\n";

                var output = InvokeCommand.InvokeScript($"& {{[CmdletBinding()]param() {(isDebugEnabled ? $"$DebugPreference = {scriptArgumentVariableName}[1];" : string.Empty)}try {{ {newLineScriptStart}{ScriptBlock}{newLineScriptEnd} }} catch {{ $PSCmdlet.ThrowTerminatingError($_); }} }}{commonParameters} *>&1 | PSStreamLogger\\Out-PSStreamLogger -Logger {scriptArgumentVariableName}[0]{commonParameters}", scriptLogger, DebugAction);

                // Write script output to output stream
                WriteObject(output, true);
            }
            catch (RuntimeException ex)
            {
                DataRecordLogger.LogRecord(scriptLogger, ex.ErrorRecord);
                ThrowTerminatingError(ex.ErrorRecord);
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