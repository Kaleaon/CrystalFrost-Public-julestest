using CrystalFrost.Lib;
using CrystalFrost.Timing;
using Microsoft.Extensions.Logging;
using OpenMetaverse.Assets;
using System;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace CrystalFrost.Assets.Textures.AVLJ2K
{
	public class AVLJ2KTextureDecoder : ITextureDecoder
	{
		[DllImport("avl_j2k")]
		private static extern bool AVL_j2k_decode(System.IntPtr raw_j2k_bytes, int size, System.IntPtr pixel_buffer_out);

		[DllImport("avl_j2k")]
		private static extern int AVL_j2k_width(System.IntPtr raw_j2k_bytes, int size);

		[DllImport("avl_j2k")]
		private static extern int AVL_j2k_height(System.IntPtr raw_j2k_bytes, int size);

		[DllImport("avl_j2k")]
		private static extern int AVL_j2k_channels(System.IntPtr raw_j2k_bytes, int size);


		private readonly ILogger<AVLJ2KTextureDecoder> _log;
		private readonly ITgaReader _tgaReader;

		public AVLJ2KTextureDecoder(
			ILogger<AVLJ2KTextureDecoder> log,
			ITgaReader tgaReader)
		{
			_log = log;
			_tgaReader = tgaReader;
		}

		private static readonly byte[] FallbackTextureData = new byte[] {
			127, 0, 127, 255, 0, 0, 0, 255,
			0, 0, 0, 255, 127, 0, 127, 255
		};

		public Task<DecodedTexture> Decode(AssetTexture texture)
		{
			return Perf.Measure("AVLJ2KTextureDecoder.Decode",
				() => Task.FromResult(DecodeInternal(texture)));
		}

		private DecodedTexture CreateFallbackTexture(UUID assetId)
		{
			return new DecodedTexture()
			{
				UUID = assetId,
				Data = FallbackTextureData,
				Width = 2,
				Height = 2,
				Components = 4
			};
		}

		private DecodedTexture DecodeInternal(AssetTexture texture)
		{
			if (texture.AssetData == null || texture.AssetData.Length == 0)
			{
				_log.LogWarning("Texture has no data {AssetID}", texture.AssetID);
				return CreateFallbackTexture(texture.AssetID);
			}

			GCHandle pinnedInHandle = default;
			GCHandle pinnedOutHandle = default;

			try
			{
				pinnedInHandle = GCHandle.Alloc(texture.AssetData, GCHandleType.Pinned);
				IntPtr inPtr = pinnedInHandle.AddrOfPinnedObject();
				var width = AVL_j2k_width(inPtr, texture.AssetData.Length);
				var height = AVL_j2k_height(inPtr, texture.AssetData.Length);
				var channels = AVL_j2k_channels(inPtr, texture.AssetData.Length);

				if (width <= 0 || height <= 0)
				{
					_log.LogError("Failed to decode texture header {AssetID}", texture.AssetID);
					return CreateFallbackTexture(texture.AssetID);
				}

				if (channels != 3 && channels != 4)
				{
					_log.LogWarning("Unsupported number of channels ({Channels}) in texture {AssetID}", channels, texture.AssetID);
					return CreateFallbackTexture(texture.AssetID);
				}

				var decodedTexture = new byte[width * height * channels];
				pinnedOutHandle = GCHandle.Alloc(decodedTexture, GCHandleType.Pinned);
				IntPtr outPtr = pinnedOutHandle.AddrOfPinnedObject();

				if (!AVL_j2k_decode(inPtr, texture.AssetData.Length, outPtr))
				{
					_log.LogError("Failed to decode texture data {AssetID}", texture.AssetID);
					return CreateFallbackTexture(texture.AssetID);
				}

				return new DecodedTexture
				{
					Data = decodedTexture,
					Width = width,
					Height = height,
					Components = channels,
					UUID = texture.AssetID
				};
			}
			catch (Exception ex)
			{
				_log.LogError(ex, "Texture decode error on {AssetID}", texture.AssetID);
				return CreateFallbackTexture(texture.AssetID);
			}
			finally
			{
				if (pinnedInHandle.IsAllocated)
				{
					pinnedInHandle.Free();
				}
				if (pinnedOutHandle.IsAllocated)
				{
					pinnedOutHandle.Free();
				}
			}
		}
	}
}
