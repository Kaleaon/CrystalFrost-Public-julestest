using Microsoft.Extensions.Logging;
using System;

namespace CrystalFrost.Exceptions
{
    /// <summary>
    /// Defines the interface for a global exception handler.
    /// </summary>
    public interface IGlobalExceptionHandler
    {
        /// <summary>
        /// Initializes the global exception handler.
        /// </summary>
        void Initialize();
    }

    /// <summary>
    /// Handles global exceptions.
    /// </summary>
    public class GlobalExceptionHandler : IGlobalExceptionHandler
    {
        private readonly ILogger<GlobalExceptionHandler> _log;

        /// <summary>
        /// Initializes a new instance of the <see cref="GlobalExceptionHandler"/> class.
        /// </summary>
        /// <param name="log">A logger for logging messages.</param>
        public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> log)
        {
            _log = log;
        }

        /// <inheritdoc/>
        public void Initialize()
        {
            AppDomain.CurrentDomain.FirstChanceException += FirstChanceException;
            AppDomain.CurrentDomain.UnhandledException += UnhandledException;
            AppDomain.CurrentDomain.ProcessExit += CurrentDomain_ProcessExit; // this might require security permissions
        }

        private void CurrentDomain_ProcessExit(object sender, EventArgs e)
        {
            _log.LogInformation("ProcessExit");
        }

        private void UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            var ex = (Exception)e.ExceptionObject;
            _log.LogError("Unhandled Exception: " + ex.ToString());
        }

        private void FirstChanceException(object sender, System.Runtime.ExceptionServices.FirstChanceExceptionEventArgs e)
        {
            _log.LogWarning("First Chance Exception: " + e.Exception.ToString());
        }
    }
}
