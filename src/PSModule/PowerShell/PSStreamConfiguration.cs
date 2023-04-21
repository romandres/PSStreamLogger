using System.Collections.Generic;
using System.Collections.ObjectModel;
using Serilog.Events;

namespace PSStreamLoggerModule
{
    public class PSStreamConfiguration
    {
        private readonly IDictionary<string, string> streamConfiguration;

        public PSStreamConfiguration(LogEventLevel minimumLogLevel)
        {
            streamConfiguration = new Dictionary<string, string>
            {
                { "VerbosePreference", minimumLogLevel <= LogEventLevel.Verbose ? "Continue" : "SilentlyContinue" },
                { "DebugPreference", minimumLogLevel <= LogEventLevel.Debug ? "Continue" : "SilentlyContinue" },
                { "InformationPreference", minimumLogLevel <= LogEventLevel.Information ? "Continue" : "SilentlyContinue" },
                { "WarningPreference", minimumLogLevel <= LogEventLevel.Warning ? "Continue" : "SilentlyContinue" },
                { "ErrorActionPreference", minimumLogLevel <= LogEventLevel.Fatal ? "Continue" : "SilentlyContinue" },
            };
        }

        public IDictionary<string, string> StreamConfiguration
        {
            get
            {
                return streamConfiguration;
            }
        }
    }
}