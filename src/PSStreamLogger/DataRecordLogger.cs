using System;
using System.Collections.Generic;
using System.Management.Automation;
using System.Text;
using Microsoft.Extensions.Logging;

namespace PSStreamLoggerModule
{
    public static class DataRecordLogger
    {
        private const string PSExtendedInfoKey = "PSExtendedInfo";
        private const string PSInvocationInfoKey = "PSInvocationInfo";

        public static void LogRecord<T>(ILogger logger, T record)
        {
            if (record is null)
            {
                throw new ArgumentNullException(nameof(record));
            }

            if (record.GetType().Equals(typeof(VerboseRecord)))
            {
                LogVerbose(logger, record as VerboseRecord);
            }
            else if (record.GetType().Equals(typeof(DebugRecord)))
            {
                LogDebug(logger, record as DebugRecord);
            }
            else if (record.GetType().Equals(typeof(ErrorRecord)))
            {
                LogError(logger, record as ErrorRecord);
            }
            else if (record.GetType().Equals(typeof(WarningRecord)))
            {
                LogWarning(logger, record as WarningRecord);
            }
            else if (record.GetType().Equals(typeof(InformationRecord)))
            {
                LogInformation(logger, record as InformationRecord);
            }
            else
            {
                throw new ArgumentException("Invalid record type", nameof(record));
            }
        }

        private static void LogVerbose(ILogger logger, VerboseRecord verboseRecord)
        {
            string message = verboseRecord.Message;
            string moduleName = verboseRecord.InvocationInfo.MyCommand.ModuleName;
            string commandName = verboseRecord.InvocationInfo.MyCommand.Name;
            string scriptFile = verboseRecord.InvocationInfo.ScriptName;
            int scriptLine = verboseRecord.InvocationInfo.ScriptLineNumber;

            string invocationInfo = GetInvocationInfo(commandName, moduleName, scriptFile, scriptLine);

            var scope = new Dictionary<string, object>();
            scope.Add(PSInvocationInfoKey, invocationInfo);

            using (logger.BeginScope(scope))
            {
                logger.LogTrace(message);
            }
        }

        private static void LogDebug(ILogger logger, DebugRecord debugRecord)
        {
            string message = debugRecord.Message;
            string moduleName = debugRecord.InvocationInfo.MyCommand.ModuleName;
            string commandName = debugRecord.InvocationInfo.MyCommand.Name;
            string scriptFile = debugRecord.InvocationInfo.ScriptName;
            int scriptLine = debugRecord.InvocationInfo.ScriptLineNumber;

            string invocationInfo = GetInvocationInfo(commandName, moduleName, scriptFile, scriptLine);

            var scope = new Dictionary<string, object>();
            scope.Add(PSInvocationInfoKey, invocationInfo);

            using (logger.BeginScope(scope))
            {
                logger.LogDebug(message);
            }
        }

        private static void LogInformation(ILogger logger, InformationRecord informationRecord)
        {
            List<string> tags = informationRecord.Tags;
            object messageData = informationRecord.MessageData;
            string scriptFile = informationRecord.Source;

            string invocationInfo = GetInvocationInfo(scriptFile, null, null, null);

            var scope = new Dictionary<string, object>();
            scope.Add(PSInvocationInfoKey, invocationInfo);

            if (tags.Count > 0)
            {
                scope.Add(PSExtendedInfoKey, $"Tags: {string.Join(", ", tags)}{Environment.NewLine}");
            }

            using (logger.BeginScope(scope))
            {
                if (messageData.GetType() == typeof(HostInformationMessage))
                {
                    string message = ((HostInformationMessage)messageData).Message;
                    logger.LogInformation(message);
                }
                else
                {
                    logger.LogInformation("{@MessageData}", messageData);
                }
            }
        }

        private static void LogWarning(ILogger logger, WarningRecord warningRecord)
        {
            string message = warningRecord.Message;
            string fullyQualifiedWarningId = warningRecord.FullyQualifiedWarningId;
            string moduleName = warningRecord.InvocationInfo.MyCommand.ModuleName;
            string commandName = warningRecord.InvocationInfo.MyCommand.Name;
            string scriptFile = warningRecord.InvocationInfo.ScriptName;
            int scriptLine = warningRecord.InvocationInfo.ScriptLineNumber;

            string invocationInfo = GetInvocationInfo(commandName, moduleName, scriptFile, scriptLine);

            var scope = new Dictionary<string, object>();
            scope.Add(PSInvocationInfoKey, invocationInfo);

            string? extendedInfo = null;
            if (!string.IsNullOrEmpty(fullyQualifiedWarningId))
            {
                extendedInfo = $"{fullyQualifiedWarningId}{Environment.NewLine}";
                scope.Add(PSExtendedInfoKey, extendedInfo);
            }

            using (logger.BeginScope(scope))
            {
                logger.LogWarning(message);
            }
        }

        private static void LogError(ILogger logger, ErrorRecord errorRecord)
        {
            string fullyQualifiedErrorId = errorRecord.FullyQualifiedErrorId;
            string scriptStackTrace = errorRecord.ScriptStackTrace;
            string activity = errorRecord.CategoryInfo.Activity;
            string targetName = errorRecord.CategoryInfo.TargetName;
            string targetTypeName = errorRecord.CategoryInfo.TargetType;
            string category = errorRecord.CategoryInfo.Category.ToString();
            string reason = errorRecord.CategoryInfo.Reason;
            string? errorMessage = string.IsNullOrEmpty(errorRecord.ErrorDetails?.Message) ? errorRecord.Exception?.Message : errorRecord.ErrorDetails?.Message;
            Exception? ex = errorRecord.Exception;

            string extendedInfo = GetExtendedErrorInfo(fullyQualifiedErrorId, activity, targetName, targetTypeName, category, reason, scriptStackTrace, ex);

            using (logger.BeginScope<Dictionary<string, object>>(new Dictionary<string, object>() { [PSExtendedInfoKey] = extendedInfo }))
            {
                logger.LogError(ex, errorMessage);
            }
        }

        private static string GetInvocationInfo(string commandName, string? moduleName, string? scriptFile, int? lineNumber)
        {
            var stringBuilder = new StringBuilder();
            stringBuilder.Append("[");

            if (!string.IsNullOrEmpty(moduleName))
            {
                stringBuilder.Append($"{moduleName}\\{commandName}");
            }
            else
            {
                stringBuilder.Append(commandName);
            }

            if (!string.IsNullOrEmpty(scriptFile))
            {
                stringBuilder.Append($"|{scriptFile}");

                if (lineNumber.HasValue)
                {
                    stringBuilder.Append($" at line: {lineNumber}");
                }
            }

            stringBuilder.Append("]");

            return stringBuilder.ToString();
        }

        private static string GetExtendedErrorInfo(string fullyQualifiedErrorId, string activity, string targetName, string targetTypeName, string category, string reason, string scriptStackTrace, Exception? exception)
        {
            var stringBuilder = new StringBuilder();
            stringBuilder.Append($"{fullyQualifiedErrorId}{Environment.NewLine}");

            if (!string.IsNullOrEmpty(activity))
            {
                stringBuilder.Append($"Activity: {activity}{Environment.NewLine}");
            }

            if (!string.IsNullOrEmpty(targetName))
            {
                stringBuilder.Append($"Target: {targetName} [{targetTypeName}]{Environment.NewLine}");
            }

            if (!string.IsNullOrEmpty(category))
            {
                stringBuilder.Append($"Category: {category}{Environment.NewLine}");
            }

            if (!string.IsNullOrEmpty(reason))
            {
                stringBuilder.Append($"Reason: {reason}{Environment.NewLine}");
            }

            if (!string.IsNullOrEmpty(scriptStackTrace))
            {
                stringBuilder.Append($"{scriptStackTrace}{Environment.NewLine}");
            }

            if (exception != null)
            {
                stringBuilder.Append($"{Environment.NewLine}{exception.GetType().FullName}: {exception.Message}");

                if (!string.IsNullOrEmpty(exception.StackTrace))
                {
                    stringBuilder.Append($"{Environment.NewLine}{exception.StackTrace}");

                    if (!exception.StackTrace.EndsWith(Environment.NewLine, StringComparison.OrdinalIgnoreCase))
                    {
                        stringBuilder.Append(Environment.NewLine);
                    }
                }
                else
                {
                    stringBuilder.Append(Environment.NewLine);
                }
            }

            return stringBuilder.ToString();
        }
    }
}
