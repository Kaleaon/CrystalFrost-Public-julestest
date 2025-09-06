using CrystalFrost.Lib;
using Microsoft.Extensions.Logging;
using OpenMetaverse.Assets;
using OpenMetaverse;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CrystalFrost.Config;
using Microsoft.Extensions.Options;
using System.Diagnostics;

namespace CrystalFrost.Assets.Textures
{
	public interface ITextureDownloadWorker : IDisposable { }

	public class TextureDownloadWorker : BackgroundWorker, ITextureDownloadWorker
	{
		private readonly TextureConfig _textureConfig;
		private readonly GridClient _client;
		private readonly IDownloadedTextureQueue _downloaded;
		private readonly ITextureDownloadRequestQueue _requests;
		private readonly System.Collections.Concurrent.ConcurrentDictionary<UUID, UUID> _pendingDownloads = new();

		public TextureDownloadWorker(
			ILogger<TextureDownloadWorker> log,
			IProvideShutdownSignal runningIndicator,
			GridClient client,
			IDownloadedTextureQueue downloadedTextureQueue,
			ITextureDownloadRequestQueue downloadRequestQueue,
			IOptions<TextureConfig> textureConfig)
			: base("TextureDownload", 0, log, runningIndicator)
		{
			_textureConfig = textureConfig.Value;
			_client = client;
			_downloaded = downloadedTextureQueue;
			_requests = downloadRequestQueue;
			_requests.ItemEnqueued += (_) => CheckForWork();
		}

		protected override async Task<bool> DoWork()
		{
			if (_requests.Count == 0) return false;
			if (!_requests.TryDequeue(out var requestID)) return true;
			if (requestID == UUID.Zero)
			{
				_log.LogError("Texture request UUID is zero");
				return true;
			}

			var tcs = new TaskCompletionSource<(TextureRequestState state, AssetTexture assetTexture)>();

			_pendingDownloads.TryAdd(requestID, requestID);

			_client.Assets.RequestImage(requestID, (state, assetTexture) => {
				tcs.TrySetResult((state, assetTexture));
			});

			var result = await tcs.Task;

			_pendingDownloads.TryRemove(requestID, out _);

			if (result.state == TextureRequestState.Finished)
			{
				_downloaded.Enqueue(result.assetTexture);
			}
			else
			{
				_log.LogWarning("Texture download failed for {TextureID} with status {Status}", requestID, result.state);
				// Request is dropped. A retry or failed queue could be implemented here.
			}

			return _requests.Count > 0;
		}

		protected override bool OutputIsBacklogged()
		{
			return _downloaded.Count > _textureConfig.MaxDownloadedTextures;
		}

		protected override void ShuttingDown()
		{
			CancelPendingDownloads();
			base.ShuttingDown();
		}

		private void CancelPendingDownloads()
		{
			foreach (var uuid in _pendingDownloads.Keys)
			{
				_client.Assets.RequestImageCancel(uuid);
			}
			_pendingDownloads.Clear();
		}
	}
}
