﻿using System;
using System.Collections.Generic;
using System.IO;
using Serilog.Core;
using Serilog.Events;
using Serilog.Formatting;
using UnityEngine;

namespace Serilog.Sinks.Unity3D
{
    public sealed class Unity3DLogEventSink : ILogEventSink
    {
        public const string UNITY_CONTEXT_KEY = "%UNITY_ID%";

        static internal readonly Dictionary<int, UnityEngine.Object> _objectstoLog = new Dictionary<int, UnityEngine.Object>();

        private readonly ITextFormatter _formatter;
        private readonly UnityEngine.ILogger _logger;

        public Unity3DLogEventSink(ITextFormatter formatter, UnityEngine.ILogger logger = null)
        {
            _formatter = formatter;
            _logger = logger ?? Debug.unityLogger;
        }

        public void Emit(LogEvent logEvent)
        {
            using (var buffer = new StringWriter())
            {
                _formatter.Format(logEvent, buffer);

                LogType logType = LogType.Log;
                switch (logEvent.Level)
                {
                    case LogEventLevel.Verbose:
                    case LogEventLevel.Debug:
                    case LogEventLevel.Information:
                        logType = LogType.Log;
                        break;
                    case LogEventLevel.Warning:
                        logType = LogType.Warning;
                        break;
                    case LogEventLevel.Error:
                    case LogEventLevel.Fatal:
                        logType = LogType.Error;
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(logEvent.Level), "Unknown log level");
                }

                object message = buffer.ToString().Trim();

                if (TryGetContext(logEvent, out var context))
                {
                    _logger.Log(logType, message, context);
                }
                else
                {
                    _logger.Log(logType, message);
                }
            }
        }

        private static bool TryGetContext(LogEvent logEvent, out UnityEngine.Object unityContext)
        {
            unityContext = null;
#if UNITY_EDITOR 
            if (logEvent.Properties.TryGetValue(UNITY_CONTEXT_KEY, out var propertyValue)
                && propertyValue is ScalarValue scalarValue
                && scalarValue.Value is int id
                && _objectstoLog.TryGetValue(id, out var unityObj))
            {
                unityContext = unityObj;
                _objectstoLog.Remove(id);
                return true;
            }
#endif
            return false;
        }
    }
}