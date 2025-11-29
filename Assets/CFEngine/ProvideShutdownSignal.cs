using Microsoft.Extensions.Logging;
using System;

namespace CrystalFrost
{
	/// <summary>
	/// Broadcasts an event to subscribers that
	/// a shutdown has started.
	/// </summary>
	/// 

	public interface IProvideShutdownSignal : IDisposable
    {
        /// <summary>
        /// Event that is raised when a shutdown starts.
        /// </summary>
        event Action OnShutdown;

        /// <summary>
        /// Causes the OnShutdown event to be raised.
        /// </summary>
        public void SignalShutdown();
    }

    /// <summary>
    /// Provides a signal that a shutdown has started.
    /// </summary>
    public class ProvideShutdownSignal : IProvideShutdownSignal
    {
        private readonly ILogger<ProvideShutdownSignal> _log;
        private readonly IUnityEditorEvents _editorEvents;
        /// <inheritdoc/>
        public event Action OnShutdown;

        /// <summary>
        /// Initializes a new instance of the <see cref="ProvideShutdownSignal"/> class.
        /// </summary>
        /// <param name="log">A logger for logging messages.</param>
        /// <param name="unityMode">The Unity editor events provider.</param>
        public ProvideShutdownSignal(ILogger<ProvideShutdownSignal> log,
            IUnityEditorEvents unityMode)
        {
            _log = log;

            // tap into the assembly before reload.
            // because if we are about to reload, we definitily want 
            // anything that is pending to shut down so the editor can move along with life.
            _editorEvents = unityMode;
            _editorEvents.BeforeAssemblyReload += BeforeEditorAssemblyReload;
        }

        private void BeforeEditorAssemblyReload()
        {
            SignalShutdown();
        }

        /// <inheritdoc/>
        public void SignalShutdown()
        {
            _log.ShutdownSignalSet();
            OnShutdown?.Invoke();
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            _editorEvents.BeforeAssemblyReload -= BeforeEditorAssemblyReload;
        }
    }
}
