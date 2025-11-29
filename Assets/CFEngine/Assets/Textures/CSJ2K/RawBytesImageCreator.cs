using CSJ2K.j2k.image;
using CSJ2K.Util;
using System;

namespace CrystalFrost.Assets.Textures.CSJ2K
{
    /// <summary>
    /// Represents an image as a raw byte array.
    /// </summary>
    public class RawBytesImage : IImage
    {
        /// <summary>
        /// The raw image data.
        /// </summary>
        public byte[] Data { get; }
        /// <summary>
        /// The height of the image.
        /// </summary>
        public int Height { get; }
        /// <summary>
        /// The width of the image.
        /// </summary>
        public int Width { get; }    

        /// <summary>
        /// Initializes a new instance of the <see cref="RawBytesImage"/> class.
        /// </summary>
        /// <param name="width">The width of the image.</param>
        /// <param name="height">The height of the image.</param>
        /// <param name="bytes">The raw image data.</param>
        public RawBytesImage(int width, int height, byte[] bytes)
        {
            Data = bytes;
            Width = width;
            Height = height;
        }

        /// <inheritdoc/>
        public T As<T>()
        {
            if (typeof(T) == typeof(RawBytesImage))
            {
                return (T)(object)this;
            }
            throw new TextureDecodeException($"Cannot cast RawBytesImage to {typeof(T)}");
        }
    }

    /// <summary>
    /// Creates raw byte images.
    /// </summary>
    internal class RawBytesImageCreator : IImageCreator
    {
        private static readonly IImageCreator Instance = new RawBytesImageCreator();

        /// <inheritdoc/>
        public bool IsDefault => false;

        /// <summary>
        /// Registers this image creator with the image factory.
        /// </summary>
        public static void Register()
        {
            ImageFactory.Register(Instance);
        }

        /// <inheritdoc/>
        public IImage Create(int width, int height, byte[] bytes)
        {
            return new RawBytesImage(width, height, bytes);
        }

        /// <inheritdoc/>
        public BlkImgDataSrc ToPortableImageSource(object imageObject)
        {
            throw new NotImplementedException();
        }
    }
}
