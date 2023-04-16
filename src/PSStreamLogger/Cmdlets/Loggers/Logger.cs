using System;
using Serilog.Events;

namespace PSStreamLoggerModule
{
    public class Logger
    {
        public const LogEventLevel DefaultMinimumLogLevel = LogEventLevel.Information;
        
        public static readonly string DefaultExpressionTemplate = $"[{{@t:yyyy-MM-dd HH:mm:ss.fffzz}} {{@l:u3}}] {{@m:lj}}{Environment.NewLine}{{{DataRecordLogger.PSErrorDetailsKey}}}";
        
        public Logger(Serilog.Events.LogEventLevel minimumLogLevel, Serilog.Core.Logger serilogLogger)
        {
            MinimumLogLevel = minimumLogLevel;
            SerilogLogger = serilogLogger;
        }
        
        internal Serilog.Events.LogEventLevel MinimumLogLevel { get; private set; }
        
        public Serilog.Core.Logger SerilogLogger { get; private set; }
    }
}