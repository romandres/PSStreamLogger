using System.Management.Automation;

namespace PSStreamLoggerModule
{
    [OutputType(typeof(Logger))]
    public class NewLoggerCmdlet : PSCmdlet
    {
        /// <summary>
        /// <para type="description">The minimum log level this logger should include.</para>
        /// <para type="description">Possible values ordered by lowest to highest: Verbose, Debug, Information, Warning, Error, Fatal.</para>
        /// </summary>
        [Parameter()]
        public Serilog.Events.LogEventLevel MinimumLogLevel { get; set; } = Logger.DefaultMinimumLogLevel;
        
        /// <summary>
        /// <para type="description">An expression-based filter (Serilog.Expressions) that defines which log events this logger should include.</para>
        /// <para type="description">For more information and examples go to: https://github.com/serilog/serilog-expressions#filtering-example.</para>
        /// </summary>
        [Parameter()]
        public string? FilterIncludeOnlyExpression { get; set; }

        /// <summary>
        /// <para type="description">An expression-based filter (Serilog.Expressions) that defines which log events this logger should exclude.</para>
        /// <para type="description">For more information and examples go to: https://github.com/serilog/serilog-expressions#filtering-example.</para>
        /// </summary>
        [Parameter()]
        public string? FilterExcludeExpression { get; set; }
    }
}