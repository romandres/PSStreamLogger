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

        private DataRecordLogger? dataRecordLogger;

        protected override void BeginProcessing()
        {
            dataRecordLogger = new DataRecordLogger(Logger!);
        }

        protected override void ProcessRecord()
        {
            try
            {
                if (DataRecordLogger.IsLogRecord(InputObject!.BaseObject))
                {
                    dataRecordLogger!.LogRecord(InputObject.BaseObject);
                }
                else
                {
                    WriteObject(InputObject.BaseObject);
                }
            }
            catch (PipelineStoppedException ex)
            {
                Logger.LogError(exception: ex, message: Resources.PipelineStopped, eventId: 0);
            }
            catch (PipelineClosedException ex)
            {
                Logger.LogError(exception: ex, message: Resources.PipelineClosed, eventId: 0);
            }
            catch (ExitException ex)
            {
                Logger.LogError(exception: ex, message: Resources.Exited, eventId: 0);
            }
        }
    }
}
