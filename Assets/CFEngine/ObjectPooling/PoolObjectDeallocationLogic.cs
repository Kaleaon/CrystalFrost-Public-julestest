using System;

namespace CrystalFrost.ObjectPooling
{
    /// <summary>
    /// Defines the interface for the logic that determines when a pooled object should be deallocated.
    /// </summary>
    public interface IPoolObjectDeallocationLogic
    {
        /// <summary>
        /// Determines whether the object requires deallocation.
        /// </summary>
        /// <returns>True if the object requires deallocation, false otherwise.</returns>
        public bool RequiresDeallocation();
    }


    /// <summary>
    /// Deallocation logic based on the age of the object.
    /// </summary>
    public class AgeBasedDeallocationLogic : IPoolObjectDeallocationLogic
    {
        /// <summary>
        /// The maximum age of the object in seconds.
        /// </summary>
        public float MaxAge { get; set; } = 10f;

        private DateTime startingTime;

        /// <summary>
        /// Initializes a new instance of the <see cref="AgeBasedDeallocationLogic"/> class.
        /// </summary>
        public AgeBasedDeallocationLogic()
        {
            startingTime =  DateTime.Now;
        }

        /// <inheritdoc/>
        public bool RequiresDeallocation()
        {
            return (DateTime.Now - startingTime).TotalSeconds > MaxAge;
        }
    }
    


}

