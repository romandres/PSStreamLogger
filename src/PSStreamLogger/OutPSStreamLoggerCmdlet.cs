using System.Management.Automation;
using Microsoft.Extensions.Logging;

namespace PSStreamLoggerModule
{
    [Cmdlet(VerbsData.Out, "PSStreamLogger")]
    public class OutPSStreamLoggerCmdlet : PSCmdlet
    {
        [Parameter(Mandatory = true, Position = 0, ValueFromPipeline = true)]
        public PSObject? InputObject { get; set; }

        [Parameter(Mandatory = true)]
        public ILogger? Logger { get; set; }

        protected override void ProcessRecord()
        {
            try
            {
                var objectType = InputObject.BaseObject.GetType();

                if (objectType.Equals(typeof(ErrorRecord))
                    || objectType.Equals(typeof(WarningRecord))
                    || objectType.Equals(typeof(InformationRecord))
                    || objectType.Equals(typeof(VerboseRecord))
                    || objectType.Equals(typeof(DebugRecord)))
                {
                    DataRecordLogger.LogRecord(Logger, InputObject.BaseObject);
                }
                else
                {
                    WriteObject(InputObject.BaseObject);
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
