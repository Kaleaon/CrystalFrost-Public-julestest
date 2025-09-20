using OpenMetaverse.Assets;
using System;
using System.Threading.Tasks;

namespace CrystalFrost.Assets.Textures
{

    /// <summary>
    /// Defines the interface for a texture decoder.
    /// </summary>
    public interface ITextureDecoder
    {
        /// <summary>
        /// Decodes a texture.
        /// </summary>
        /// <param name="texture">The texture to decode.</param>
        /// <returns>A decoded texture.</returns>
        Task<DecodedTexture> Decode(AssetTexture texture);
    }

    /// <summary>
    /// The exception that is thrown when a texture fails to decode.
    /// </summary>
    public class TextureDecodeException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="TextureDecodeException"/> class.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        public TextureDecodeException(string message) : base(message) { }
        /// <summary>
        /// Initializes a new instance of the <see cref="TextureDecodeException"/> class.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        /// <param name="inner">The exception that is the cause of the current exception.</param>
        public TextureDecodeException(string message, Exception inner) : base(message, inner) { }
    }
}
