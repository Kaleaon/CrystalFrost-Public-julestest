using System;
using System.Threading.Tasks;

namespace CrystalFrost
{
    /// <summary>
    /// Defines events that are raised in response to the Unity message loop.
    /// </summary>
    public interface IEngineBehaviorEvents
    {
        /// <summary>
        /// This event is raised when the script instance is being loaded.
        /// </summary>
        event Action Awake;
        /// <summary>
        /// This event is raised when the object becomes enabled and active.
        /// </summary>
        event Action OnEnable;
        /// <summary>
        /// This event is raised on the frame when a script is enabled just before any of the Update methods are called the first time.
        /// </summary>
        event Action Start;
        /// <summary>
        /// This event is raised every fixed framerate frame, if the MonoBehaviour is enabled.
        /// </summary>
        event Action FixedUpdate;
        /// <summary>
        /// This event is raised every frame, if the MonoBehaviour is enabled.
        /// </summary>
        event Action Update;
        /// <summary>
        /// This event is raised every frame, if the MonoBehaviour is enabled, after all Update functions have been called.
        /// </summary>
        event Action LateUpdate;
        /// <summary>
        /// This event is raised when the behaviour becomes disabled or inactive.
        /// </summary>
        event Action OnDisable;
        /// <summary>
        /// This event is raised when the MonoBehaviour will be destroyed.
        /// </summary>
        event Action OnDestroy;

        /// <summary>
        /// Raises the Awake event.
        /// </summary>
        void DoAwake();
        /// <summary>
        /// Raises the OnEnable event.
        /// </summary>
        void DoOnEnable();
        /// <summary>
        /// Raises the Start event.
        /// </summary>
        void DoStart();
        /// <summary>
        /// Raises the FixedUpdate event.
        /// </summary>
        void DoFixedUpdate();
        /// <summary>
        /// Raises the Update event.
        /// </summary>
        void DoUpdate();
        /// <summary>
        /// Raises the LateUpdate event.
        /// </summary>
        void DoLateUpdate();
        /// <summary>
        /// Raises the OnDisable event.
        /// </summary>
        void DoOnDisable();
        /// <summary>
        /// Raises the OnDestroy event.
        /// </summary>
        void DoOnDestroy();
    }

    /// <summary>
    /// Implements the <see cref="IEngineBehaviorEvents"/> interface.
    /// </summary>
    public class EngineBehaviorEvents : IEngineBehaviorEvents
    {
        /// <inheritdoc />
        public event Action Awake;
        /// <inheritdoc />
        public event Action OnEnable;
        /// <inheritdoc />
        public event Action Start;
        /// <inheritdoc />
        public event Action FixedUpdate;
        /// <inheritdoc />
        public event Action Update;
        /// <inheritdoc />
        public event Action LateUpdate;
        /// <inheritdoc />
        public event Action OnDisable;
        /// <inheritdoc />
        public event Action OnDestroy;

        private static void DoInBackground(Action action)
        {
            // capture the current event handler
            // in case it changes on another thread
            // while we are using it.
            var a = action;
            if (a is null) return;
            _ = Task.Run(() => a?.Invoke());
        }

        /// <inheritdoc />
        public void DoAwake() => DoInBackground(Awake);
        /// <inheritdoc />
        public void DoOnDestroy() => DoInBackground(OnDestroy);
        /// <inheritdoc />
        public void DoOnDisable() => DoInBackground(OnDisable);
        /// <inheritdoc />
        public void DoOnEnable() => DoInBackground(OnEnable);
        /// <inheritdoc />
        public void DoFixedUpdate() => DoInBackground(FixedUpdate);
        /// <inheritdoc />
        public void DoLateUpdate() => DoInBackground(LateUpdate);
        /// <inheritdoc />
        public void DoStart() => DoInBackground(Start);
        /// <inheritdoc />
        public void DoUpdate() => DoInBackground(Update);
    }
}
