using System;
using System.Collections.ObjectModel;
using System.Management.Automation;
using System.Management.Automation.Runspaces;
using Microsoft.PowerShell;

namespace PSStreamLoggerModule
{
    internal class PowerShellExecutor : IDisposable
    {
        private readonly PowerShell powerShell;

        private bool disposed = false;

        private readonly DataRecordLogger dataRecordLogger;

        public PowerShellExecutor(DataRecordLogger dataRecordLogger, string workingDirectory, ExecutionPolicy? executionPolicy)
        {
            this.dataRecordLogger = dataRecordLogger;

            if (executionPolicy.HasValue)
            {
                Environment.SetEnvironmentVariable("PSExecutionPolicyPreference", executionPolicy.Value.ToString(), EnvironmentVariableTarget.Process);
            }

            var initialSessionState = InitialSessionState.CreateDefault();

            Runspace runspace = RunspaceFactory.CreateRunspace(initialSessionState);
            runspace.Open();

            powerShell = PowerShell.Create();
            powerShell.Runspace = runspace;

            powerShell.Runspace.SessionStateProxy.Path.SetLocation(workingDirectory);

            powerShell.Streams.Warning.DataAdded += Warning_DataAdded;
            powerShell.Streams.Verbose.DataAdded += Verbose_DataAdded;
            powerShell.Streams.Information.DataAdded += Information_DataAdded;
            powerShell.Streams.Error.DataAdded += Error_DataAdded;
            powerShell.Streams.Debug.DataAdded += Debug_DataAdded;
        }

        public Collection<PSObject> Execute(string script)
        {
            powerShell.AddScript(script);

            return Execute();
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
                    powerShell?.Dispose();
                }

                disposed = true;
            }
        }

        private Collection<PSObject> Execute()
        {
            return powerShell.Invoke();
        }

        private void Debug_DataAdded(object sender, DataAddedEventArgs e)
        {
            if (sender is PSDataCollection<DebugRecord> dataCollection)
            {
                dataRecordLogger.LogRecord(dataCollection[e.Index]);
            }
        }

        private void Error_DataAdded(object sender, DataAddedEventArgs e)
        {
            if (sender is PSDataCollection<ErrorRecord> dataCollection)
            {
                dataRecordLogger.LogRecord(dataCollection[e.Index]);
            }
        }

        private void Information_DataAdded(object sender, DataAddedEventArgs e)
        {
            if (sender is PSDataCollection<InformationRecord> dataCollection)
            {
                dataRecordLogger.LogRecord(dataCollection[e.Index]);
            }
        }

        private void Verbose_DataAdded(object sender, DataAddedEventArgs e)
        {
            if (sender is PSDataCollection<VerboseRecord> dataCollection)
            {
                dataRecordLogger.LogRecord(dataCollection[e.Index]);
            }
        }

        private void Warning_DataAdded(object sender, DataAddedEventArgs e)
        {
            if (sender is PSDataCollection<WarningRecord> dataCollection)
            {
                dataRecordLogger.LogRecord(dataCollection[e.Index]);
            }
        }
    }
}