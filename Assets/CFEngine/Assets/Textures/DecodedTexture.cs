using OpenMetaverse;

namespace CrystalFrost.Assets.Textures
{
    /// <summary>
    /// Represents a decoded texture.
    /// </summary>
    public class DecodedTexture
    {
        /// <summary>
        /// The UUID of the texture.
        /// </summary>
        public UUID UUID { get; set; }
        /// <summary>
        /// The raw texture data.
        /// </summary>
        public byte[] Data { get; set; }
        /// <summary>
        /// The width of the texture.
        /// </summary>
        public int Width { get; set; }
        /// <summary>
        /// The height of the texture.
        /// </summary>
        public int Height { get; set; }

        /// <summary>
        /// Bytes per pixel
        /// </summary>
        public int Components { get; set; }
    }
}
