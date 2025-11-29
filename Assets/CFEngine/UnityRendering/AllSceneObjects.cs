using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;

namespace CrystalFrost.UnityRendering
{
	/// <summary>
	/// Defines the interface for a collection of all scene objects.
	/// </summary>
	public interface IAllSceneObjects
	{
		/// <summary>
		/// Adds a scene object to the collection.
		/// </summary>
		/// <param name="sceneObject">The scene object to add.</param>
		/// <returns>True if the object was added, false otherwise.</returns>
		bool Add(SceneObject sceneObject);
		/// <summary>
		/// Gets a scene object from the collection by its local ID.
		/// </summary>
		/// <param name="localID">The local ID of the scene object to get.</param>
		/// <returns>The scene object, or null if it was not found.</returns>
		SceneObject Get(uint localID);
	}

	/// <summary>
	/// A collection of all scene objects.
	/// </summary>
	public class AllSceneObjects : IAllSceneObjects
	{
		private readonly ConcurrentDictionary<uint, SceneObject> _objects = new();
		private readonly ILogger<AllSceneObjects> _log;

		/// <summary>
		/// Initializes a new instance of the <see cref="AllSceneObjects"/> class.
		/// </summary>
		/// <param name="log">A logger for logging messages.</param>
		public AllSceneObjects(ILogger<AllSceneObjects> log)
		{
			_log = log;
		}

		/// <inheritdoc/>
		public bool Add(SceneObject sceneObject)
		{
			if (_objects.TryAdd(sceneObject.LocalID, sceneObject)) return true;
			_log.FailedAddingToAllSceneObjects(sceneObject.LocalID);
			return false;
		}

		/// <inheritdoc/>
		public SceneObject Get(uint localID)
		{
			if (_objects.ContainsKey(localID)) return _objects[localID];
			return default;
		}
	}
}
