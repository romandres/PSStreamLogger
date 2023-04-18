using System.Management.Automation;

namespace PSStreamLoggerModule
{
    public class NewTextLoggerCmldet : NewLoggerCmdlet
    {
        /// <summary>
        /// <para type="description">The expression template (Serilog.Expressions) defines how log events are converted to text.</para>
        /// <para type="description">For more information and examples go to: https://github.com/serilog/serilog-expressions#formatting-with-expressiontemplate.</para>
        /// <para type="description">More examples: https://nblumhardt.com/2021/06/customize-serilog-text-output/</para>
        /// </summary>
        [Parameter()]
        public string ExpressionTemplate { get; set; } = Logger.DefaultExpressionTemplate;
    }
}