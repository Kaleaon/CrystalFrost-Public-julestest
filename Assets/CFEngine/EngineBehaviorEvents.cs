using System;
using System.Threading.Tasks;

namespace CrystalFrost
{
    public interface IEngineBehaviorEvents
    {
        event Action Awake;
        event Action OnEnable;
        event Action Start;
        event Action FixedUpdate;
        event Action Update;
        event Action LateUpdate;
        event Action OnDisable;
        event Action OnDestroy;

        void DoAwake();
        void DoOnEnable();
        void DoStart();
        void DoFixedUpdate();
        void DoUpdate();
        void DoLateUpdate();
        void DoOnDisable();
        void DoOnDestroy();
    }

    public class EngineBehaviorEvents : IEngineBehaviorEvents
    {
        public event Action Awake;
        public event Action OnEnable;
        public event Action Start;
        public event Action FixedUpdate;
        public event Action Update;
        public event Action LateUpdate;
        public event Action OnDisable;
        public event Action OnDestroy;

        public void DoAwake() => Awake?.Invoke();
        public void DoOnDestroy() => OnDestroy?.Invoke();
        public void DoOnDisable() => OnDisable?.Invoke();
        public void DoOnEnable() => OnEnable?.Invoke();
        public void DoFixedUpdate() => FixedUpdate?.Invoke();
        public void DoLateUpdate() => LateUpdate?.Invoke();
        public void DoStart() => Start?.Invoke();
        public void DoUpdate() => Update?.Invoke();
    }
}
