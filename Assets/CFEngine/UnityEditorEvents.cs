using Microsoft.Extensions.Logging;
using System;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace CrystalFrost
{
    /// <summary>
    /// Provides an abstraction around Unity Editor Events.
    /// </summary>
    /// 
    /// 
    public interface IUnityEditorEvents
    {
        /// <summary>
        /// Gets a value indicating whether the code is running in the Unity Editor.
        /// </summary>
        bool IsEditor { get; }
        /// <summary>
        /// Event raised before an assembly is reloaded.
        /// </summary>
        event Action BeforeAssemblyReload;
        /// <summary>
        /// Event raised after an assembly has been reloaded.
        /// </summary>
        event Action AfterAssemblyReload;
        /// <summary>
        /// Event raised when the hierarchy of objects has changed.
        /// </summary>
        event Action HierarchyChanged;
        /// <summary>
        /// Event raised when the editor is paused.
        /// </summary>
        event Action EditorPaused;
        /// <summary>
        /// Event raised when the editor is unpaused.
        /// </summary>
        event Action EditorUnpaused;
        /// <summary>
        /// Event raised when the editor enters edit mode.
        /// </summary>
        event Action EnteredEditMode;
        /// <summary>
        /// Event raised when the editor enters play mode.
        /// </summary>
        event Action EnteredPlayMode;
        /// <summary>
        /// Event raised when the editor is exiting edit mode.
        /// </summary>
        event Action ExitingEditMode;
        /// <summary>
        /// Event raised when the editor is exiting play mode.
        /// </summary>
        event Action ExitingPlayMode;
        /// <summary>
        /// Event raised when the project has changed.
        /// </summary>
        event Action ProjectChanged;
        /// <summary>
        /// Event raised when the editor is quitting.
        /// </summary>
        event Action Quitting;
        /// <summary>
        /// Event raised when the editor wants to quit.
        /// </summary>
        event Action WantsToQuit;
    }

    /// <inheritdoc/>
    public class UnityEditorEvents : IUnityEditorEvents, IDisposable
    {
        private readonly ILogger<UnityEditorEvents> _log;
        /// <inheritdoc/>
        public event Action BeforeAssemblyReload;
        /// <inheritdoc/>
        public event Action AfterAssemblyReload;
        /// <inheritdoc/>
        public event Action HierarchyChanged;
        /// <inheritdoc/>
        public event Action EditorPaused;
        /// <inheritdoc/>
        public event Action EditorUnpaused;
        /// <inheritdoc/>
        public event Action EnteredEditMode;
        /// <inheritdoc/>
        public event Action EnteredPlayMode;
        /// <inheritdoc/>
        public event Action ExitingEditMode;
        /// <inheritdoc/>
        public event Action ExitingPlayMode;
        /// <inheritdoc/>
        public event Action ProjectChanged;
        /// <inheritdoc/>
        public event Action Quitting;
        /// <inheritdoc/>
        public event Action WantsToQuit;
        
        private const bool _isEditor =
#if UNITY_EDITOR
            true;
#else
            false;
#endif

        /// <inheritdoc/>
        public bool IsEditor => _isEditor;

        /// <summary>
        /// Initializes a new instance of the <see cref="UnityEditorEvents"/> class.
        /// </summary>
        /// <param name="log">A logger for logging messages.</param>
        public UnityEditorEvents(ILogger<UnityEditorEvents> log)
        { 
            _log = log;

#if UNITY_EDITOR
            EditorApplication.hierarchyChanged += EditorApplication_hierarchyChanged;
            EditorApplication.pauseStateChanged += EditorApplication_pauseStateChanged;
            EditorApplication.playModeStateChanged += EditorApplication_playModeStateChanged;
            EditorApplication.projectChanged += EditorApplication_projectChanged;
            EditorApplication.quitting += EditorApplication_quitting;
            EditorApplication.wantsToQuit += EditorApplication_wantsToQuit;

            AssemblyReloadEvents.beforeAssemblyReload += AssemblyReloadEvents_beforeAssemblyReload;
            AssemblyReloadEvents.afterAssemblyReload += AssemblyReloadEvents_afterAssemblyReload;
#endif
        }

#if UNITY_EDITOR
        private void AssemblyReloadEvents_afterAssemblyReload()
        {
            _log.EditorEvent_AfterAssemblyReload();
            AfterAssemblyReload?.Invoke();
        }

        private void AssemblyReloadEvents_beforeAssemblyReload()
        {
            _log.EditorEvent_BeforeAssemblyReload();;
            BeforeAssemblyReload?.Invoke();
        }

        private bool EditorApplication_wantsToQuit()
        {
            _log.EditorEvent_WantsToQuit();
            WantsToQuit?.Invoke();
            return true;
        }

        private void EditorApplication_quitting()
        {
            _log.EditorEvent_Quitting();
            Quitting?.Invoke();
        }

        private void EditorApplication_projectChanged()
        {
            _log.EditorEvent_ProjectChanged();
            ProjectChanged?.Invoke();
        }

        private void EditorApplication_playModeStateChanged(PlayModeStateChange mode)
        {
            _log.EditorEvent_PlayModeChange(mode);
            var e = mode switch
            {
                PlayModeStateChange.EnteredPlayMode => EnteredPlayMode,
                PlayModeStateChange.EnteredEditMode => EnteredEditMode,
                PlayModeStateChange.ExitingPlayMode => ExitingPlayMode,
                PlayModeStateChange.ExitingEditMode => ExitingEditMode,
                _ => null
            };
            e?.Invoke();
        }

        private void EditorApplication_pauseStateChanged(PauseState state)
        {
            _log.EditorEvent_PauseStateChange(state);
            var e = state switch
            {
                PauseState.Paused => EditorPaused,
                PauseState.Unpaused => EditorUnpaused,
                _ => null
            };
            e?.Invoke();
        }

        private void EditorApplication_hierarchyChanged()
        {
            _log.EditorEvent_HierarchyChanged();
            HierarchyChanged?.Invoke();
        }
#endif

        public void Dispose()
        {
#if UNITY_EDITOR
            EditorApplication.hierarchyChanged -= EditorApplication_hierarchyChanged;
            EditorApplication.pauseStateChanged -= EditorApplication_pauseStateChanged;
            EditorApplication.playModeStateChanged -= EditorApplication_playModeStateChanged;
            EditorApplication.projectChanged -= EditorApplication_projectChanged;
            EditorApplication.quitting -= EditorApplication_quitting;
            EditorApplication.wantsToQuit -= EditorApplication_wantsToQuit;

            AssemblyReloadEvents.beforeAssemblyReload -= AssemblyReloadEvents_beforeAssemblyReload;
            AssemblyReloadEvents.afterAssemblyReload -= AssemblyReloadEvents_afterAssemblyReload;
#endif
            GC.SuppressFinalize(this);
        }
    }
}
