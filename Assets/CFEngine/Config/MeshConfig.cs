using System.IO;
using UnityEngine;

namespace CrystalFrost.Config
{
	/// <summary>
	/// Contains configuration about the Mesh Loading subsystem.
	/// </summary>
    public class MeshConfig
    {
        /// <summary>
        /// The name of the configuration subsection.
        /// </summary>
        public const string subsectionName = "Meshes";
		//Todo: is better to cache path is grid specific to prevent conflict of same assetID
		private readonly string cachePath = Path.Combine(Application.persistentDataPath, "assetmesh");

        /// <summary>
        /// Whether caching is allowed.
        /// </summary>
		public bool isCachingAllowed { get; set; } = true;

        /// <summary>
        /// Gets the path to the cache.
        /// </summary>
        /// <returns>The path to the cache.</returns>
		public string GetCachePath()
		{
			return cachePath;
		}

		/// <summary>
		/// A limit on the number of meshes waiting be loaded into the GPU
		/// </summary>
		public int MaxReadyMeshes { get; set; } = 5;

		/// <summary>
		/// A Limit on the number of meshes waiting to be decoded.
		/// Decoding a mesh prepares it for loading into the GPU.
		/// </summary>
        public int MaxDownloadedMeshes { get; set; } = 2000;

    }
}
