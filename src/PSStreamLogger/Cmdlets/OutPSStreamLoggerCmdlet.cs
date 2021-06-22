using System.Management.Automation;

namespace PSStreamLoggerModule
{
    [Cmdlet(VerbsData.Out, "PSStreamLogger")]
    public class OutPSStreamLoggerCmdlet : PSCmdlet
    {
        [Parameter(Mandatory = true, Position = 0, ValueFromPipeline = true)]
        public PSObject? InputObject { get; set; }

        [Parameter(Mandatory = true)]
        public DataRecordLogger? DataRecordLogger { get; set; }

        protected override void ProcessRecord()
        {
            if (DataRecordLogger.IsLogRecord(InputObject!.BaseObject))
            {
                DataRecordLogger!.LogRecord(InputObject.BaseObject);
            }
            else
            {
                WriteObject(InputObject.BaseObject);
            }
        }
    }
}
