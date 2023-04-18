using System.Management.Automation;

namespace PSStreamLoggerModule
{
    /// <summary>
    /// <para type="synopsis">Sends redirected stream data to the DataRecordLogger.</para>
    /// <para type="description">This is internally used by the Invoke-CommandWithLogging command if one of the execution modes NewScope or CurrentScope are used to leverage stream redirection to send stream data to the DataRecordLogger through this command.</para>
    /// <para type="description">Output is passed through.</para>
    /// <para type="type">Cmdlet</para>
    /// </summary>
    [Cmdlet(VerbsData.Out, "PSStreamLogger")]
    public class OutPSStreamLoggerCmdlet : PSCmdlet
    {
        /// <summary>
        /// <para type="description">PSObject to send to the DataRecordLogger.</para>
        /// </summary>
        [Parameter(Mandatory = true, Position = 0, ValueFromPipeline = true)]
        public PSObject? InputObject { get; set; }

        /// <summary>
        /// <para type="description">The DataRecordLogger that will process PSObjects received.</para>
        /// <para type="description">PSObjects of type Verbose-, Debug-, Information-, Warning- and ErrorRecord will be processed as log events by the DataRecordLogger while any other object will be passed through.</para>
        /// </summary>
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
