using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;

using CrystalFrost.Config;
using CrystalFrost.Lib;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using OpenMetaverse;
using OpenMetaverse.Assets;
using UnityEngine;

namespace CrystalFrost.Assets.Mesh
{
	public interface IMeshDownloadWorker : IDisposable { }

	public class MeshDownloadWorker : BackgroundWorker, IMeshDownloadWorker
	{
		private readonly MeshConfig _meshConfig;
		private readonly GridClient _client;
		private readonly IDownloadedMeshCacheQueue _downloaded;
		private readonly IMeshDownloadRequestQueue _requests;
		private readonly System.Collections.Concurrent.ConcurrentDictionary<UUID, MeshRequest> _pendingDownloads = new();

		public MeshDownloadWorker(
			ILogger<MeshDownloadWorker> log,
			IProvideShutdownSignal runningIndicator,
			GridClient client,
			IDownloadedMeshCacheQueue downloadedMeshQueue,
			IMeshDownloadRequestQueue downloadRequestQueue,
			IOptions<MeshConfig> meshConfig
			)
			: base("MeshDownload", 0, log, runningIndicator)
		{
			_meshConfig = meshConfig.Value;
			_client = client;
			_downloaded = downloadedMeshQueue;
			_downloaded.ItemDequeued += Downloaded_ItemDequeued;
			_requests = downloadRequestQueue;
			_requests.ItemEnqueued += Request_ItemEnqueud;
		}

		private void Downloaded_ItemDequeued(MeshRequest obj)
		{
			CheckForWork();
		}

		private void Request_ItemEnqueud(MeshRequest obj)
		{
			CheckForWork();
		}

		protected override async Task<bool> DoWork()
		{
			if (_requests.Count == 0) return false;
			if (!_requests.TryDequeue(out var request)) return true;
			if (request is null) return true;
			if (request.UUID == UUID.Zero)
			{
				_log.LogError("Mesh request UUID is zero");
				return true;
			}

			var tcs = new TaskCompletionSource<(bool success, AssetMesh assetMesh)>();

			_pendingDownloads.TryAdd(request.UUID, request);

			_client.Assets.RequestMesh(request.UUID,
				(bool success, AssetMesh assetMesh) =>
				{
					tcs.TrySetResult((success, assetMesh));
				});

			var result = await tcs.Task;

			_pendingDownloads.TryRemove(request.UUID, out _);

			if (!result.success)
			{
				_log.LogWarning($"Mesh download failed UUID: {request.UUID}");
				// The request is dropped. A retry mechanism could be implemented here.
			}
			else
			{
				request.AssetMesh = result.assetMesh;
				_downloaded.Enqueue(request);
			}

			return _requests.Count > 0;
		}

		protected override bool OutputIsBacklogged()
		{
			return _downloaded.Count > _meshConfig.MaxDownloadedMeshes;
		}

		protected override void ShuttingDown()
		{
			CancelPendingDownloads();
			base.ShuttingDown();
		}

		private void CancelPendingDownloads()
		{
			// The new design with awaiting downloads means there are fewer
			// "pending" downloads in the traditional sense.
			// We could potentially cancel the TaskCompletionSource tasks
			// but for now, clearing the tracking dictionary is sufficient.
			_pendingDownloads.Clear();
		}
	}
}
