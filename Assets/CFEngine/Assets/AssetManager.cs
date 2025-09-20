using CrystalFrost.Assets.Animation;
using CrystalFrost.Assets.Mesh;

namespace CrystalFrost.Assets
{
    /// <summary>
    /// Defines the interface for an asset manager.
    /// </summary>
    public interface IAssetManager
    {
        /// <summary>
        /// Gets the texture manager.
        /// </summary>
        public ITextureManager Textures { get; }
        /// <summary>
        /// Gets the mesh manager.
        /// </summary>
        public IMeshManager Meshes { get; }
        /// <summary>
        /// Gets the animation manager.
        /// </summary>
        public IAnimationManager AnimationManager { get; }
    }
    /// <summary>
    /// Manages the assets in the application.
    /// </summary>
    public class AssetManager : IAssetManager
    {
        /// <inheritdoc />
        public ITextureManager Textures { get; }
        /// <inheritdoc />
        public IMeshManager Meshes { get; }
        /// <inheritdoc />
		public IAnimationManager AnimationManager { get; }
        /// <summary>
        /// Initializes a new instance of the <see cref="AssetManager"/> class.
        /// </summary>
        /// <param name="textures">The texture manager.</param>
        /// <param name="meshes">The mesh manager.</param>
        /// <param name="animationManager">The animation manager.</param>
		public AssetManager(ITextureManager textures,
            IMeshManager meshes, IAnimationManager animationManager)
        {
            Textures = textures;
            Meshes = meshes;
            AnimationManager = animationManager;
        }
    }
}
