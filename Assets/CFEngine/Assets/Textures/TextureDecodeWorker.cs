using System;
using System.Threading.Tasks;

using CrystalFrost.Assets.Textures;
using CrystalFrost.Config;
using CrystalFrost.Lib;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using OpenMetaverse.Assets;

namespace CrystalFrost.Assets
{
	/// <summary>
	/// Defines the interface for a worker that decodes textures.
	/// </summary>
	public interface ITextureDecodeWorker : IDisposable { }

	/// <summary>
	/// A background worker that decodes textures.
	/// </summary>
	public class TextureDecodeWorker : BackgroundWorker, ITextureDecodeWorker
	{
		private readonly ITextureDecoder _decoder;
		private readonly IDownloadedTextureQueue _downloadedTextureQueue;
		private readonly IDecodedTextureCacheQueue _readyTextureQueue;
		private readonly TextureConfig _textureConfig;

		/// <summary>
		/// Initializes a new instance of the <see cref="TextureDecodeWorker"/> class.
		/// </summary>
		/// <param name="log">A logger for logging messages.</param>
		/// <param name="runningIndicator">A signal that indicates that the application is shutting down.</param>
		/// <param name="decoder">The texture decoder to use.</param>
		/// <param name="readyTextureQueue">A queue of decoded textures.</param>
		/// <param name="downloadedTextureQueue">A queue of downloaded textures to be decoded.</param>
		/// <param name="textureConfig">The configuration for textures.</param>
		public TextureDecodeWorker(
			ILogger<TextureDecodeWorker> log,
			IProvideShutdownSignal runningIndicator,
			ITextureDecoder decoder,
			IDecodedTextureCacheQueue readyTextureQueue,
			IDownloadedTextureQueue downloadedTextureQueue,
			IOptions<TextureConfig> textureConfig)
			: base("TextureDecode", 0, log, runningIndicator)
		{
			_textureConfig = textureConfig.Value;
			_decoder = decoder;
			_downloadedTextureQueue = downloadedTextureQueue;
			_downloadedTextureQueue.ItemEnqueued += DownloadedTextureQueue_ItemEnqueued;
			_readyTextureQueue = readyTextureQueue;
			_readyTextureQueue.ItemDequeued += ReadyTextureQueue_ItemDequeued;
		}

		private void ReadyTextureQueue_ItemDequeued(DecodedTexture obj)
		{
			CheckForWork();
		}

		private void DownloadedTextureQueue_ItemEnqueued(AssetTexture obj)
		{
			CheckForWork();
		}

		protected override async Task<bool> DoWork()
		{
			if (_downloadedTextureQueue.Count == 0) return false;

			if (!_downloadedTextureQueue.TryDequeue(out var texture)) return true;

			var decoded = await _decoder.Decode(texture);

			_readyTextureQueue.Enqueue(decoded);

			return _downloadedTextureQueue.Count > 0; // there is more work to do.
		}

		protected override bool OutputIsBacklogged()
		{
			// return true if there are enough pending
			// textures to be loaded into the GPU.
			return _readyTextureQueue.Count > _textureConfig.MaxReadyTextures;
		}

		public override void Dispose()
		{
			_downloadedTextureQueue.ItemEnqueued -= DownloadedTextureQueue_ItemEnqueued;
			_readyTextureQueue.ItemDequeued -= ReadyTextureQueue_ItemDequeued;
			base.Dispose();
		}
	}
}
