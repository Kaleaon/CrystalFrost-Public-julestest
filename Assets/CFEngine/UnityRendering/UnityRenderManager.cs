using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CrystalFrost.UnityRendering
{
	/// <summary>
	/// Defines the interface for a Unity render manager.
	/// </summary>
	public interface IUnityRenderManager
	{
		/// <summary>
		/// Gets the collection of all scene objects.
		/// </summary>
		public IAllSceneObjects SceneObjects { get; }
	}

	/// <summary>
	/// Manages rendering in Unity.
	/// </summary>
	public class UnityRenderManager : IUnityRenderManager
	{
		/// <inheritdoc/>
		public IAllSceneObjects SceneObjects { get; private set; }

		/// <summary>
		/// Initializes a new instance of the <see cref="UnityRenderManager"/> class.
		/// </summary>
		/// <param name="allSceneObjects">The collection of all scene objects.</param>
		public UnityRenderManager(IAllSceneObjects allSceneObjects)
		{
			SceneObjects = allSceneObjects;
		}
	}
}
