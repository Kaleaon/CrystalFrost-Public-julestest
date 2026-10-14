using CrystalFrost.Assets.Animation;
using CrystalFrost.Assets.Mesh;
using OpenMetaverse;
using UnityEngine;

namespace CrystalFrost.Assets
{
    /// <summary>
    /// Defines an interface for a manager that oversees all asset types.
    /// </summary>
    public interface IAssetManager
    {
        /// <summary>
        /// Gets the texture asset manager.
        /// </summary>
        public ITextureManager Textures { get; }
        /// <summary>
        /// Gets the mesh asset manager.
        /// </summary>
        public IMeshManager Meshes { get; }
        /// <summary>
        /// Gets the animation asset manager.
        /// </summary>
        public IAnimationManager AnimationManager { get; }

        /// <summary>
        /// Requests a texture asset by UUID.
        /// </summary>
        Texture2D RequestTexture(UUID uuid);

        /// <summary>
        /// Requests a material asset by UUID and configuration.
        /// </summary>
        Material RequestMaterial(UUID uuid, Renderer renderer = null, int subMeshIndex = 0, Color color = default, float glow = 0f, bool fullbright = false);
    }

    /// <summary>
    /// Manages all asset types, including textures, meshes, and animations.
    /// </summary>
    public class AssetManager : IAssetManager
    {
        public ITextureManager Textures { get; }
        public IMeshManager Meshes { get; }
		public IAnimationManager AnimationManager { get; }

		/// <summary>
		/// Initializes a new instance of the <see cref="AssetManager"/> class.
		/// </summary>
		/// <param name="textures">The texture asset manager.</param>
		/// <param name="meshes">The mesh asset manager.</param>
		/// <param name="animationManager">The animation asset manager.</param>
		public AssetManager(ITextureManager textures,
            IMeshManager meshes, IAnimationManager animationManager)
        {
            Textures = textures;
            Meshes = meshes;
            AnimationManager = animationManager;
        }

        public Texture2D RequestTexture(UUID uuid)
        {
            Textures?.RequestImage(uuid);
            return null;
        }

        public Material RequestMaterial(UUID uuid, Renderer renderer = null, int subMeshIndex = 0, Color color = default, float glow = 0f, bool fullbright = false)
        {
            Textures?.RequestImage(uuid);
            return null;
        }
    }
}
