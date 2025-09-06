using CrystalFrost.Assets.Mesh;
using CrystalFrost.Assets.Textures;
using Microsoft.Extensions.Logging;
using OpenMetaverse;
using System;
using System.Collections.Generic;

namespace CrystalFrost.Assets
{
	public interface ITextureManager
	{
		/// <summary>
		/// Requests that the texture manager download
		/// and decode the desired image asset.
		/// </summary>
		/// <param name="uuid"></param>
		void RequestImage(UUID uuid);
		IReadyTextureQueue ReadyTextures { get; }
	}

	public class TextureManager : ITextureManager, IDisposable
	{
		private readonly ILogger<TextureManager> _log;
		private readonly ITextureRequestQueue _textureRequests;

		public IReadyTextureQueue ReadyTextures { get; }

		private readonly ITextureDecodeWorker _decodeWorker;
		private readonly ITextureDownloadWorker _downloadWorker;
		private readonly ITextureCacheWorker _textureCache;

		public TextureManager(
			ILogger<TextureManager> log,
			IReadyTextureQueue readyTextureQueue,
			ITextureRequestQueue textureRequests,
			ITextureDecodeWorker decodeWorker,
			ITextureDownloadWorker downloadWorker,
			ITextureCacheWorker textureCache)
		{
			_log = log;
			_textureRequests = textureRequests;
			_textureCache = textureCache;
			ReadyTextures = readyTextureQueue;

			// We don't really use these directly, but we need to instantiate them
			// so that they will subscribe to queue events and process them.
			_decodeWorker = decodeWorker;
			_downloadWorker = downloadWorker;
		}

		public void RequestImage(UUID uuid)
		{
			_log.TextureRequested(uuid);
			_textureRequests.Enqueue(uuid);
		}

		public void Dispose()
		{
			_decodeWorker.Dispose();
			_downloadWorker.Dispose();
			GC.SuppressFinalize(this);
		}
	}
}
