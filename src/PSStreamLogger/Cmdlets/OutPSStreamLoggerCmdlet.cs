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
                if (DataRecordLogger.IsLogRecord(InputObject!.BaseObject))
                {
                    DataRecordLogger.LogRecord(Logger!, InputObject.BaseObject);
                }
                else
                {
                    WriteObject(InputObject.BaseObject);
                }
            }
            catch (PipelineStoppedException ex)
            {
                Logger.LogError(ex, Resources.PipelineStopped);
            }
            catch (PipelineClosedException ex)
            {
                Logger.LogError(ex, Resources.PipelineClosed);
            }
            catch (ExitException ex)
            {
                Logger.LogError(ex, Resources.Exited);
            }
        }
    }
}
