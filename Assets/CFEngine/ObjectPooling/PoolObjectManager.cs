using UnityEngine;
using System;


namespace CrystalFrost.ObjectPooling
{

    public partial class ObjectPool 
    {
        /// <summary>
        /// Manages a pooled object.
        /// </summary>
        public class PoolObjectManager : MonoBehaviour
        {
            private Type[] requiredComponents = null;
            private ObjectPool pool = null;
            private IPoolObjectDeallocationLogic deallocationLogic = null;
            private bool isActivated = false;
            private float updationTime = 0f;
            private string uid = null;
            /// <summary>
            /// The time the object was created.
            /// </summary>
            public float CreationTime => updationTime;
            /// <summary>
            /// Whether the object is activated.
            /// </summary>
            public bool IsActivated => isActivated;
            /// <summary>
            /// The age of the object.
            /// </summary>
            public float Age => Time.time - updationTime; // returns age of object alive when its alive or age of object dead when its dead

            /// <summary>
            /// The unique ID of the object.
            /// </summary>
            public string UID { get => uid; set => uid = value;}
            /// <summary>
            /// The required components for the object.
            /// </summary>
            public Type[] RequiredComponents { set => requiredComponents = value; }
            /// <summary>
            /// The object pool that this object belongs to.
            /// </summary>
            public ObjectPool Pool { set => pool = value; }
            private bool requiresDeallocationCall = false;

            /// <summary>
            /// Allocates the object and assigns deallocation logic.
            /// </summary>
            /// <param name="deallocationLogic">The deallocation logic to use.</param>
            public void AllocateSelf(IPoolObjectDeallocationLogic deallocationLogic)
            {
                // set the object to active
                gameObject.SetActive(true);

                updationTime = Time.time;

                isActivated = true;
                this.deallocationLogic = deallocationLogic;
            }

            /// <summary>
            /// Updates the object, checking if it requires deallocation.
            /// </summary>
            public void UpdateObject()
            {
                if (isActivated && deallocationLogic != null && deallocationLogic.RequiresDeallocation())
                {
                    requiresDeallocationCall = true;
                }
            }

            private void LateUpdate()
            {
                if (requiresDeallocationCall)
                {
                    this.DeallocateSelf();
                    requiresDeallocationCall = false;
                }
            }

            /// <summary>
            /// Sets the deallocation logic for the object.
            /// </summary>
            /// <param name="deallocationLogic">The deallocation logic to use.</param>
            public void SetDeallocationLogic(IPoolObjectDeallocationLogic deallocationLogic)
            {
                this.deallocationLogic = deallocationLogic;
            }

            /// <summary>
            /// Deallocates the object and performs cleanup.
            /// </summary>
            public void DeallocateSelf()
            {
                isActivated = false;

                // destroy any component that is not in the requiredComponents list and is not a Transform
                // May be for later
                // foreach (Component cmpnt in gameObject.GetComponents<Component>())
                // {
                //     if (!requiredComponents.Contains(cmpnt.GetType()) && !(cmpnt is Transform))
                //     {
                //         Destroy(cmpnt);
                //     }
                // }

                // reset the transform
                transform.SetParent(pool.poolParentObject.transform);

                gameObject.SetActive(false);

                updationTime = Time.time;
            }
            
        }
    }

}
