using Microsoft.Extensions.Logging;
using System;
using UnityEngine;

namespace CrystalFrost.Exceptions
{
    public interface IGlobalExceptionHandler
    {
        void Initialize();
    }

    public class GlobalExceptionHandler : IGlobalExceptionHandler
    {
        private readonly ILogger<GlobalExceptionHandler> _log;

        public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> log)
        {
            _log = log;
        }

        public void Initialize()
        {
            AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
            Application.logMessageReceivedThreaded += OnUnityLogMessageReceived;
        }

        private void OnUnityLogMessageReceived(string condition, string stackTrace, LogType type)
        {
            if (type == LogType.Exception)
            {
                // Note: The 'condition' string often contains the exception type and message.
                // The 'stackTrace' is separate. We combine them for a comprehensive log.
                _log.LogError("Unhandled Unity Exception:\nCondition: {Condition}\nStackTrace: {StackTrace}", condition, stackTrace);
            }
        }

        private void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            if (e.ExceptionObject is Exception ex)
            {
                _log.LogError(ex, "Unhandled AppDomain Exception");
            }
            else
            {
                _log.LogError("Unhandled AppDomain Exception with non-Exception object: {ExceptionObject}", e.ExceptionObject);
            }
        }
    }
}
