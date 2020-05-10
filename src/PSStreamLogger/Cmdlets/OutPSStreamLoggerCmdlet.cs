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
#pragma warning disable CS8602 // Dereference of a possibly null reference. InputObject cannot be null because the parameter is mandatory.
                if (DataRecordLogger.IsLogRecord(InputObject.BaseObject))
#pragma warning restore CS8602 // Dereference of a possibly null reference. InputObject cannot be null because the parameter is mandatory.
                {
#pragma warning disable CS8604 // Possible null reference argument. Logger cannot be null because the parameter is mandatory.
                    DataRecordLogger.LogRecord(Logger, InputObject.BaseObject);
#pragma warning restore CS8604 // Possible null reference argument. Logger cannot be null because the parameter is mandatory.
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
