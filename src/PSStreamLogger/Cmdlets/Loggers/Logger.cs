using System;
using Serilog.Events;

namespace PSStreamLoggerModule
{
    /// <summary>
    /// <para type="description">A PSStreamLogger logger.</para>
    /// </summary>
    public class Logger
    {
        public const LogEventLevel DefaultMinimumLogLevel = LogEventLevel.Information;
        
        public static readonly string DefaultExpressionTemplate = $"[{{@t:yyyy-MM-dd HH:mm:ss.fffzz}} {{@l:u3}}] {{@m:lj}}{Environment.NewLine}{{{DataRecordLogger.PSErrorDetailsKey}}}";
        
        public Logger(Serilog.Events.LogEventLevel minimumLogLevel, Serilog.Core.Logger serilogLogger, string name)
        {
            MinimumLogLevel = minimumLogLevel;
            SerilogLogger = serilogLogger;
            Name = $"{name}_{Guid.NewGuid()}";
        }

        public string Name { get; private set; }

        internal Serilog.Events.LogEventLevel MinimumLogLevel { get; private set; }
        
        internal Serilog.Core.Logger? SerilogLogger { get; set; }
    }
}