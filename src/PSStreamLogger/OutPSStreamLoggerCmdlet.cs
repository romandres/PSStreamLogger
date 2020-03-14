using System.Management.Automation;
using Microsoft.Extensions.Logging;

namespace PSStreamLogger
{
    [Cmdlet(VerbsData.Out, "PSStreamLogger")]
    public class OutPSStreamLoggerCmdlet : PSCmdlet
    {
        [Parameter(Mandatory = true, Position = 0, ValueFromPipeline = true)]
        public PSObject InputObject { get; set; }

        [Parameter(Mandatory = true)]
        public ILogger Logger { get; set; }

        //protected override void BeginProcessing()
        //{
        //    base.BeginProcessing();
        //}

        protected override void ProcessRecord()
        {
            if (InputObject is null)
            {
                return;
            }

            try
            {
                var objectTypeName = InputObject.BaseObject.GetType().FullName;

                switch (objectTypeName)
                {
                    case "System.Management.Automation.ErrorRecord":
                        var errorRecord = (ErrorRecord)InputObject.BaseObject;
                        DataRecordLogger.LogRecord(Logger, errorRecord);
                        break;
                    case "System.Management.Automation.WarningRecord":
                        var warningRecord = (WarningRecord)InputObject.BaseObject;
                        DataRecordLogger.LogRecord(Logger, warningRecord);
                        break;
                    case "System.Management.Automation.InformationRecord":
                        var infoRecord = (InformationRecord)InputObject.BaseObject;
                        DataRecordLogger.LogRecord(Logger, infoRecord);
                        break;
                    case "System.Management.Automation.VerboseRecord":
                        var verboseRecord = (VerboseRecord)InputObject.BaseObject;
                        DataRecordLogger.LogRecord(Logger, verboseRecord);
                        break;
                    case "System.Management.Automation.DebugRecord":
                        var debugRecord = (DebugRecord)InputObject.BaseObject;
                        DataRecordLogger.LogRecord(Logger, debugRecord);
                        break;
                    default:
                        WriteObject(InputObject.BaseObject);
                        break;
                }
            }
            catch (PipelineStoppedException ex)
            {
                Logger.LogError(ex, "Pipeline stopped");
            }
            catch (PipelineClosedException ex)
            {
                Logger.LogError(ex, "Pipeline closed");
            }
            catch (ExitException ex)
            {
                Logger.LogError(ex, "Exited");
            }
        }
    }
}
