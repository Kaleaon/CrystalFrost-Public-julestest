using System;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Threading.Tasks;
using CrystalFrost.Lib;
using CrystalFrost.Config;

namespace CrystalFrost.Assets.Animation
{

	/// <summary>
	/// Defines the interface for a worker that decodes animations.
	/// </summary>
	public interface IAnimationDecodeWorker : IDisposable { }

	/// <summary>
	/// A background worker that decodes animations.
	/// </summary>
	public class AnimationDecodeWorker : BackgroundWorker, IAnimationDecodeWorker
	{
		private readonly AnimationConfig _AnimationConfig;
		private readonly IDownloadedAnimationQueue _downloadedAnimationQueue;
		private readonly IDecodedAnimationQueue _readyAnimationQueue;
		private readonly IAnimationDecoder _AnimationDecoder;

		/// <summary>
		/// Initializes a new instance of the <see cref="AnimationDecodeWorker"/> class.
		/// </summary>
		/// <param name="log">A logger for logging messages.</param>
		/// <param name="runningIndicator">A signal that indicates that the application is shutting down.</param>
		/// <param name="downloadedAnimationQueue">A queue of downloaded animations to be decoded.</param>
		/// <param name="readyAnimationQueue">A queue of decoded animations.</param>
		/// <param name="AnimationDecoder">The animation decoder to use.</param>
		/// <param name="AnimationConfig">The configuration for animations.</param>
		public AnimationDecodeWorker(
			ILogger<AnimationDecodeWorker> log,
			IProvideShutdownSignal runningIndicator,
			IDownloadedAnimationQueue downloadedAnimationQueue,
			IDecodedAnimationQueue readyAnimationQueue,
			IAnimationDecoder AnimationDecoder,
			IOptions<AnimationConfig> AnimationConfig)
			: base("AnimationDecode", 0, log, runningIndicator)
		{
			_AnimationConfig = AnimationConfig.Value;
			_downloadedAnimationQueue = downloadedAnimationQueue;
			_downloadedAnimationQueue.ItemEnqueued += DownloadedAnimationQueue_ItemEnqueued;
			_readyAnimationQueue = readyAnimationQueue;
			_readyAnimationQueue.ItemDequeued += ReadyAnimationQueue_ItemDequeued;
			_AnimationDecoder = AnimationDecoder;
		}

		private void ReadyAnimationQueue_ItemDequeued(AnimationRequest obj)
		{
			CheckForWork();
		}

		private void DownloadedAnimationQueue_ItemEnqueued(AnimationRequest obj)
		{
			CheckForWork();
		}

		protected override Task<bool> DoWork()
		{
			return Task.Run(() => DoWorkImpl());
		}

		private bool DoWorkImpl()
		{
			if (_downloadedAnimationQueue.Count == 0) return false;
			if (!_downloadedAnimationQueue.TryDequeue(out var request)) return true;
			if (request is null) return true;
			// decode something
			_AnimationDecoder.Decode(request);
			return _downloadedAnimationQueue.Count > 0;
		}

		protected override bool OutputIsBacklogged()
		{
			return _readyAnimationQueue.Count > _AnimationConfig.MaxReadyAnimations;
		}

		public override void Dispose()
		{
			_downloadedAnimationQueue.ItemEnqueued -= DownloadedAnimationQueue_ItemEnqueued;
			_readyAnimationQueue.ItemDequeued -= ReadyAnimationQueue_ItemDequeued;
			base.Dispose();
		}
	}


}

