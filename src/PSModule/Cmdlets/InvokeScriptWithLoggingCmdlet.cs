using System.Management.Automation;

namespace PSStreamLoggerModule
{
    [Cmdlet(VerbsLifecycle.Invoke, "ScriptWithLogging")]
    public class InvokeScriptWithLoggingCmdlet : InvokeCommandWithLoggingCmdlet
    {
        [Parameter(Mandatory = true)]
        public string? Path { get; set; }
        
        private new ScriptBlock? ScriptBlock { get; set; }

        protected override void EndProcessing()
        {
            base.ScriptBlock = ScriptBlock.Create(@$"& ""{Path}""");
            base.EndProcessing();
        }
    }
}