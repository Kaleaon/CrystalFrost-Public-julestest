using System.IO;
using UnityEngine;

namespace CrystalFrost.Config
{
	/// <summary>
	/// Contains configuration about the Animation Loading subsystem.
	/// </summary>
	public class AnimationConfig
	{
		/// <summary>
		/// The name of the configuration subsection.
		/// </summary>
		public const string subsectionName = "Animations";
		//Todo: is better to cache path is grid specific to prevent conflict of same assetID
		private readonly string cachePath = Path.Combine(Application.persistentDataPath, "assetAnimation");

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
		/// A limit on the number of Animationes waiting be loaded into the GPU
		/// </summary>
		public int MaxReadyAnimations { get; set; } = 5;

		/// <summary>
		/// A Limit on the number of Animationes waiting to be decoded.
		/// Decoding a Animation prepares it for loading into the GPU.
		/// </summary>
		public int MaxDownloadedAnimations { get; set; } = 50;

	}
}
