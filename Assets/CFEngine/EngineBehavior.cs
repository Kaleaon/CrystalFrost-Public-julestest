using UnityEngine;

namespace CrystalFrost
{
    /// <summary>
    /// A MonoBehaviour that hooks into the Unity message loop and passes the events to the IEngineBehaviorEvents service.
    /// </summary>
    public class EngineBehavior : MonoBehaviour
    {
        private IEngineBehaviorEvents _events;

        /// <summary>
        /// Called when the script instance is being loaded.
        /// </summary>
        void Awake()
        {
            _events = Services.GetService<IEngineBehaviorEvents>();
            _events.DoAwake();
        }

        /// <summary>
        /// Called when the object becomes enabled and active.
        /// </summary>
        void OnEnable()
        {
            _events.DoOnEnable();
        }

        /// <summary>
        /// Called on the frame when a script is enabled just before any of the Update methods are called the first time.
        /// </summary>
        void Start()
        {
            _events.DoStart();
        }

        /// <summary>
        /// This function is called every fixed framerate frame, if the MonoBehaviour is enabled.
        /// </summary>
        void FixedUpdate()
        {
            _events.DoFixedUpdate();
        }

        /// <summary>
        /// Called every frame, if the MonoBehaviour is enabled.
        /// </summary>
        void Update()
        {
            _events.DoUpdate();
        }

        /// <summary>
        /// Called every frame, if the MonoBehaviour is enabled, after all Update functions have been called.
        /// </summary>
        void LateUpdate()
        {
            _events.DoLateUpdate();
        }

        /// <summary>
        /// This function is called when the behaviour becomes disabled or inactive.
        /// </summary>
        void OnDisable()
        {
            _events.DoOnDisable();
        }

        /// <summary>
        /// This function is called when the MonoBehaviour will be destroyed.
        /// </summary>
        void OnDestroy()
        {
            _events.DoOnDestroy();
        }
    }
}
