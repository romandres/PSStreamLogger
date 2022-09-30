using System;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;

namespace PSStreamLoggerModule
{
    public class DataRecordLogger
    {
        public const string PSTagsKey = "PSTags";
        public const string PSCommandInvocationInfoKey = "PSCommandInvocationInfo";
        public const string PSErrorDetailsKey = "PSErrorDetails";
        public const string PSErrorInfoKey = "PSErrorInfo";
        public const string PSFullyQualifiedErrorIdKey = "PSFullyQualifiedErrorId";
        public const string PSErrorIdKey = "PSErrorId";
        public const string PSErrorCommandNameKey = "PSErrorCommandName";
        public const string PSErrorScriptStackTraceKey = "PSErrorScriptStackTrace";
        public const string PSErrorExceptionStackTraceKey = "PSErrorExceptionStackTrace";

        private readonly ILogger logger;

        private readonly int numberOfStackTraceLinesToRemove;

        public DataRecordLogger(ILogger logger, int numberOfStackTraceLinesToRemove = 0)
        {
            this.logger = logger;
            this.numberOfStackTraceLinesToRemove = numberOfStackTraceLinesToRemove;
        }

        public static bool IsLogRecord<T>(T record)
        {
            if (record is null)
            {
                throw new ArgumentNullException(nameof(record));
            }

            var recordType = record.GetType();

            return recordType.Equals(typeof(ErrorRecord))
                    || recordType.Equals(typeof(WarningRecord))
                    || recordType.Equals(typeof(InformationRecord))
                    || recordType.Equals(typeof(VerboseRecord))
                    || recordType.Equals(typeof(DebugRecord));
        }

        public void LogRecord<T>(T record) =>
            GetLogAction(record).Invoke();

        private Action GetLogAction<T>(T record) =>
            record switch
            {
                VerboseRecord verboseRecord => () => LogVerbose(verboseRecord),
                DebugRecord debugRecord => () => LogDebug(debugRecord),
                ErrorRecord errorRecord => () => LogError(errorRecord),
                WarningRecord warningRecord => () => LogWarning(warningRecord),
                InformationRecord infoRecord => () => LogInformation(infoRecord),
                null => throw new ArgumentNullException(nameof(record)),
                _ => throw new ArgumentException(Resources.InvalidRecordType, nameof(record))
            };

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "CA2254:Template should be a static expression", Justification = "This method is passing through log messages to the logger which is why the template cannot be static.")]
        private void LogVerbose(VerboseRecord verboseRecord)
        {
            string message = verboseRecord.Message;
            string moduleName = verboseRecord.InvocationInfo.MyCommand.ModuleName;
            string commandName = verboseRecord.InvocationInfo.MyCommand.Name;
            string scriptFile = verboseRecord.InvocationInfo.ScriptName;
            int scriptLine = verboseRecord.InvocationInfo.ScriptLineNumber;

            string? invocationInfo = GetInvocationInfo(commandName, moduleName, scriptFile, scriptLine);

            var scope = new Dictionary<string, object?>
            {
                { PSCommandInvocationInfoKey, invocationInfo }
            };

            using (logger.BeginScope(scope))
            {
                logger.LogTrace(message);
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "CA2254:Template should be a static expression", Justification = "This method is passing through log messages to the logger which is why the template cannot be static.")]
        private void LogDebug(DebugRecord debugRecord)
        {
            string message = debugRecord.Message;
            string moduleName = debugRecord.InvocationInfo.MyCommand.ModuleName;
            string commandName = debugRecord.InvocationInfo.MyCommand.Name;
            string scriptFile = debugRecord.InvocationInfo.ScriptName;
            int scriptLine = debugRecord.InvocationInfo.ScriptLineNumber;

            string? invocationInfo = GetInvocationInfo(commandName, moduleName, scriptFile, scriptLine);

            var scope = new Dictionary<string, object?>
            {
                { PSCommandInvocationInfoKey, invocationInfo }
            };

            using (logger.BeginScope(scope))
            {
                logger.LogDebug(message);
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "CA2254:Template should be a static expression", Justification = "This method is passing through log messages to the logger which is why the template cannot be static.")]
        private void LogInformation(InformationRecord informationRecord)
        {
            List<string> tags = informationRecord.Tags;
            object messageData = informationRecord.MessageData;
            string? scriptFile = "Write-Information".Equals(informationRecord.Source, StringComparison.OrdinalIgnoreCase) ? null : informationRecord.Source;

            string? invocationInfo = GetInvocationInfo(scriptFile, null, null, null);

            var scope = new Dictionary<string, object?>
            {
                { PSCommandInvocationInfoKey, invocationInfo }
            };

            if (tags.Count > 0)
            {
                scope.Add(PSTagsKey, tags);
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

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "CA2254:Template should be a static expression", Justification = "This method is passing through log messages to the logger which is why the template cannot be static.")]
        private void LogWarning(WarningRecord warningRecord)
        {
            string message = warningRecord.Message;
            string moduleName = warningRecord.InvocationInfo.MyCommand.ModuleName;
            string commandName = warningRecord.InvocationInfo.MyCommand.Name;
            string scriptFile = warningRecord.InvocationInfo.ScriptName;
            int scriptLine = warningRecord.InvocationInfo.ScriptLineNumber;

            string? invocationInfo = GetInvocationInfo(commandName, moduleName, scriptFile, scriptLine);

            var scope = new Dictionary<string, object?>
            {
                { PSCommandInvocationInfoKey, invocationInfo }
            };

            using (logger.BeginScope(scope))
            {
                logger.LogWarning(message);
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "CA2254:Template should be a static expression", Justification = "This method is passing through log messages to the logger which is why the template cannot be static.")]
        private void LogError(ErrorRecord errorRecord)
        {
            string fullyQualifiedErrorId = errorRecord.FullyQualifiedErrorId;
            string errorId = fullyQualifiedErrorId;
            string? errorCommand = null;
            if (fullyQualifiedErrorId.Contains(","))
            {
                string[] errorParts = fullyQualifiedErrorId.Split(',');
                errorId = errorParts[0];
                errorCommand = errorParts[1];
            }

            string? scriptStackTrace;
            if (!string.IsNullOrEmpty(errorRecord.ScriptStackTrace))
            {
                if (numberOfStackTraceLinesToRemove > 0)
                {
                    // Remove the last two lines of the script stack trace which come from the InvokeCommandWithLogging cmdlet
                    var scriptStackTraceParts = Regex.Split(errorRecord.ScriptStackTrace, Environment.NewLine);
                    scriptStackTrace = $"{string.Join(Environment.NewLine, scriptStackTraceParts.Take(scriptStackTraceParts.Length - numberOfStackTraceLinesToRemove))}{Environment.NewLine}";
                }
                else
                {
                    scriptStackTrace = $"{errorRecord.ScriptStackTrace}{Environment.NewLine}";
                }
            }
            else
            {
                scriptStackTrace = null;
            }

            string activity = errorRecord.CategoryInfo.Activity;
            string targetName = errorRecord.CategoryInfo.TargetName;
            string targetTypeName = errorRecord.CategoryInfo.TargetType;
            string category = errorRecord.CategoryInfo.Category.ToString();
            string reason = errorRecord.CategoryInfo.Reason;
            string? errorMessage = string.IsNullOrEmpty(errorRecord.ErrorDetails?.Message) ? errorRecord.Exception?.Message : errorRecord.ErrorDetails?.Message;
            Exception? ex = errorRecord.Exception;

            string errorInfo = GetErrorInfo(fullyQualifiedErrorId, activity, targetName, targetTypeName, category, reason);
            string? exceptionStackTrace = GetExceptionStackTrace(ex);

            var scope = new Dictionary<string, object?>
            {
                { PSErrorDetailsKey, $"{errorInfo}{Environment.NewLine}{scriptStackTrace}{Environment.NewLine}{exceptionStackTrace}" },
                { PSErrorInfoKey, errorInfo },
                { PSFullyQualifiedErrorIdKey, fullyQualifiedErrorId },
                { PSErrorIdKey, errorId },
                { PSErrorCommandNameKey, errorCommand },
                { PSErrorScriptStackTraceKey, scriptStackTrace },
                { PSErrorExceptionStackTraceKey, exceptionStackTrace }
            };

            using (logger.BeginScope(scope))
            {
                logger.LogError(exception: ex, message: errorMessage, eventId: 0);
            }
        }

        private static string? GetInvocationInfo(string? commandName, string? moduleName, string? scriptFile, int? lineNumber)
        {
            if (string.IsNullOrEmpty(commandName) && string.IsNullOrEmpty(moduleName) && string.IsNullOrEmpty(scriptFile) && !lineNumber.HasValue)
            {
                return null;
            }

            if (string.IsNullOrEmpty(commandName))
            {
                commandName = "<ScriptBlock>";
            }

            var stringBuilder = new StringBuilder();

            if (!string.IsNullOrEmpty(moduleName))
            {
                stringBuilder.Append($"{moduleName}\\{commandName}");
            }
            else
            {
                stringBuilder.Append(commandName);
            }

            if (string.IsNullOrEmpty(scriptFile))
            {
                scriptFile = "<No file>";
            }

            if (!string.IsNullOrEmpty(scriptFile))
            {
                stringBuilder.Append(", ");
                stringBuilder.Append(scriptFile);
            }

            if (lineNumber.HasValue)
            {
                if (!string.IsNullOrEmpty(commandName) || !string.IsNullOrEmpty(scriptFile))
                {
                    stringBuilder.Append(": ");
                }

                stringBuilder.Append($"line {lineNumber}");
            }

            return stringBuilder.ToString();
        }

        private static string? GetExceptionStackTrace(Exception? exception)
        {
            if (exception is object)
            {
                var stringBuilder = new StringBuilder();

                stringBuilder.Append($"{exception.GetType().FullName}: {exception.Message}");

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

                return stringBuilder.ToString();
            }

            return null;
        }

        private static string GetErrorInfo(string fullyQualifiedErrorId, string activity, string targetName, string targetTypeName, string category, string reason)
        {
            var stringBuilder = new StringBuilder();
            stringBuilder.Append($"FullyQualifiedErrorID: {fullyQualifiedErrorId}{Environment.NewLine}");

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

            return stringBuilder.ToString();
        }
    }
}
