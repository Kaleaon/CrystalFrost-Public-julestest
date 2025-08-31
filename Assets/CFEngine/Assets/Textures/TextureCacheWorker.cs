
using System;
using Microsoft.Extensions.Logging;
using OpenMetaverse;
using OpenMetaverse.Assets;
using UnityEngine;
using System.IO;
using CrystalFrost.Client.Credentials;
using CrystalFrost.Config;
using Microsoft.Extensions.Options;
using CrystalFrost.Assets.Mesh;
using CrystalFrost.Lib;
using CrystalFrost.Assets.Textures;
using System.Threading.Tasks;
using Org.BouncyCastle.Crypto.Paddings;
using CommandLine;

namespace CrystalFrost
{
	public interface ITextureCacheWorker : IDisposable { }

	public class TextureCacheWorker : BackgroundWorker, ITextureCacheWorker
	{
		private readonly TextureConfig _textureConfig;
		private readonly IReadyTextureQueue _readyTextureQueue;
		private readonly ITextureRequestQueue _textureRequestQueue;
		private readonly ITextureDownloadRequestQueue _downloadRequestQueue;
		private readonly IDecodedTextureCacheQueue _decodedCacheQueue;
		private readonly IAesEncryptor _encryptor;

		private bool _isCachingAllowed;
		private string _cachePath;


		public TextureCacheWorker(
			ILogger<ITextureCacheWorker> log,
			IProvideShutdownSignal runningIndicator,
			IAesEncryptor aesEncryptor,
			IReadyTextureQueue readyTextureQueue,
			ITextureDownloadRequestQueue downloadRequestQueue,
			ITextureRequestQueue textureRequestQueue,
			IDecodedTextureCacheQueue decodedCache,
			IOptions<TextureConfig> textureConfig)
			: base("TextureCache", 1, log, runningIndicator)
		{
			_textureConfig = textureConfig.Value;
			_encryptor = aesEncryptor;
			_cachePath = _textureConfig.getCachePath();
			_isCachingAllowed = _textureConfig.isCachingAllowed;

			if (!Directory.Exists(_cachePath))
			{
				Directory.CreateDirectory(_cachePath);
			}

			_readyTextureQueue = readyTextureQueue;
			_textureRequestQueue = textureRequestQueue;
			_downloadRequestQueue = downloadRequestQueue;
			_decodedCacheQueue = decodedCache;

			_textureRequestQueue.ItemEnqueued += WorkItemEnqueued;
			_decodedCacheQueue.ItemEnqueued += WorkItemEnqueued;
		}

		private void WorkItemEnqueued(DecodedTexture obj)
		{
			CheckForWork();
		}

		private void WorkItemEnqueued(UUID obj)
		{
			CheckForWork();
		}

		protected override Task<bool> DoWork()
		{
			bool resultLoad;
			bool resultSave;
			if (!_isCachingAllowed)
			{
				// Just pass through the requests through queues
				resultLoad = DoWorkImplPassThroughLoad();
				resultSave = DoWorkImplPassThroughSave();
			}
			else
			{
				resultLoad = DoWorkImplLoadCache();
				resultSave = DoWorkImplSaveCache();
			}
			return Task.FromResult(resultLoad || resultSave);
		}

		private bool DoWorkImplPassThroughLoad()
		{
			if (_textureRequestQueue.Count == 0) return false;
			if (!_textureRequestQueue.TryDequeue(out var request)) return true;
			if (request == null) return true;
			_downloadRequestQueue.Enqueue(request);
			return _textureRequestQueue.Count > 0;
		}

		private bool DoWorkImplPassThroughSave()
		{
			if (_decodedCacheQueue.Count == 0) return false;
			if (!_decodedCacheQueue.TryDequeue(out var request)) return true;
			if (request == null) return true;
			//_downloadedTextureQueue.Enqueue(request);
			_readyTextureQueue.Enqueue(request);
			return _decodedCacheQueue.Count > 0;
		}

		private DecodedTexture DeserializeDecodedTexture(byte[] data)
		{
			if (data == null) return null;
			var texture = new DecodedTexture();
			using (var stream = new MemoryStream(data))
			{
				using (var reader = new BinaryReader(stream))
				{
					var uuid = reader.ReadBytes(16);
					texture.UUID = new UUID(uuid, 0);
					texture.Width = reader.ReadInt32();
					texture.Height = reader.ReadInt32();
					texture.Components = reader.ReadInt32();
					var size = texture.Width * texture.Height * texture.Components;
					texture.Data = reader.ReadBytes((int)size);
				}
			}
			return texture;
		}

		private byte[] SerializeDecodedTexture(DecodedTexture texture)
		{
			if (texture == null) return null;
			using (var stream = new MemoryStream())
			{
				using (var writer = new BinaryWriter(stream))
				{
					writer.Write(texture.UUID.GetBytes());
					writer.Write(texture.Width);
					writer.Write(texture.Height);
					writer.Write(texture.Components);
					writer.Write(texture.Data);
				}
				return stream.ToArray();
			}
		}

		private bool DoWorkImplLoadCache()
		{
			if (_textureRequestQueue.Count == 0) return false;
			if (!_textureRequestQueue.TryDequeue(out var request)) return true;
			if (request == null) return true;

			try
			{
				var cachePath = Path.Combine(_cachePath, request.ToString() + ".asset");
				if (!File.Exists(cachePath)) // texture is not cached, pass it to download queue
				{
					_downloadRequestQueue.Enqueue(request);
				}
				else // load cached texture
				{
					using (var stream = File.OpenRead(cachePath))
					{
						var encryptedData = new byte[stream.Length];
						stream.Read(encryptedData, 0, encryptedData.Length);
						var decryptedData = _encryptor.Decrypt(encryptedData);
						var texture = DeserializeDecodedTexture(decryptedData);
						_readyTextureQueue.Enqueue(texture);
					}
				}
			}
			catch (Exception ex)
			{
				_log.LogError(ex, "Error loading texture {TextureID} from cache. Treating as cache miss.", request);
				// Treat as a cache miss
				_downloadRequestQueue.Enqueue(request);
			}
			return _textureRequestQueue.Count > 0;
		}

		private bool DoWorkImplSaveCache()
		{
			if (_decodedCacheQueue.Count == 0) return false;
			if (!_decodedCacheQueue.TryDequeue(out var request)) return true;
			if (request == null) return true;

			try
			{
				var cachePath = Path.Combine(_cachePath, request.UUID.ToString() + ".asset");
				if (!File.Exists(cachePath))
				{
					using (var stream = File.Create(cachePath))
					{
						var encryptedData = _encryptor.Encrypt(SerializeDecodedTexture(request));
						stream.Write(encryptedData, 0, encryptedData.Length);
					}
				}
			}
			catch (Exception ex)
			{
				_log.LogError(ex, "Error saving texture {TextureID} to cache. Skipping cache save.", request.UUID);
				// Continue without caching, but still pass the texture on.
			}

			_readyTextureQueue.Enqueue(request);
			return _decodedCacheQueue.Count > 0;
		}


		protected override bool OutputIsBacklogged()
		{
			return false; // Temporary measure for now
		}

		protected override void ShuttingDown()
		{
			base.ShuttingDown();
		}

	}
}
