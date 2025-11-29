using CrystalFrost.WorldState;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace CrystalFrost.UnityRendering
{
	/// <summary>
	/// A counterpart to SimObject, but containing Data related
	/// to rendering with Unity.
	/// </summary>
	public class SceneObject
	{
		/// <summary>
		/// Relates this object to a SimObject (and to a LibreMetavers Primitive)
		/// </summary>
		public uint LocalID;

		/// <summary>
		/// The game object that holds the hierarchy of the scene object.
		/// </summary>
		public GameObject HeirachyHolder;

		/// <summary>
		/// The game object that represents the scene object.
		/// </summary>
		public GameObject GameObject;

		/// <summary>
		/// The game object that holds the mesh of the scene object.
		/// </summary>
		public GameObject MeshHolder;

		/// <summary>
		/// The sim object that this scene object represents.
		/// </summary>
		public SimObject SimObject;

		/// <summary>
		/// The parent scene object.
		/// </summary>
		public SceneObject Parent;

		/// <summary>
		/// Whether the scene object is water.
		/// </summary>
		public bool IsWater;

		/// <summary>
		/// The renderers for the scene object.
		/// </summary>
		public Renderer[] Renderers;

#if USE_KWS
	public WaterSystem WaterSystem;

	public GameObject WaterBox;
#endif
		/// <summary>
		/// The light for the scene object.
		/// </summary>
		public GameObject Light;

	}
}
