namespace PSStreamLoggerModule
{
    public class Logger
    {
        public Logger(Serilog.Events.LogEventLevel minimumLogLevel, Serilog.Core.Logger serilogLogger)
        {
            MinimumLogLevel = minimumLogLevel;
            SerilogLogger = serilogLogger;
        }
        
        internal Serilog.Events.LogEventLevel MinimumLogLevel { get; set; }
        
        public Serilog.Core.Logger SerilogLogger { get; set; }
    }
}