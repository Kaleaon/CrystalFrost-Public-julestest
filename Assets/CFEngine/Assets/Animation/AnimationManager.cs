using Microsoft.Extensions.Logging;
using OpenMetaverse;
using System;

namespace CrystalFrost.Assets.Animation
{

	/// <summary>
	/// Defines the interface for an animation manager.
	/// </summary>
	public interface IAnimationManager : IDisposable
	{
		/// <summary>
		/// Requests an animation.
		/// </summary>
		/// <param name="primitive">The primitive to which the animation will be applied.</param>
		/// <param name="animationId">The UUID of the animation to request.</param>
		public void RequestAnimation(Primitive primitive, UUID animationId);
	}

	/// <summary>
	/// Manages animations.
	/// </summary>
	public class AnimationManager : IAnimationManager
	{
		private readonly ILogger<AnimationManager> _log;
		private readonly IAnimationRequestQueue _requestQueue;
		private readonly IAnimationDownloadWorker _downloadWorker;
		private readonly IAnimationDecodeWorker _decodeWorker;
		private readonly IAnimationCacheWorker _animationCache;

		/// <summary>
		/// Initializes a new instance of the <see cref="AnimationManager"/> class.
		/// </summary>
		/// <param name="log">A logger for logging messages.</param>
		/// <param name="requestQueue">A queue of animation requests.</param>
		/// <param name="downloadWorker">The worker that downloads animations.</param>
		/// <param name="decodeWorker">The worker that decodes animations.</param>
		/// <param name="animationCache">The animation cache.</param>
		public AnimationManager(ILogger<AnimationManager> log,
			IAnimationRequestQueue requestQueue,
			IAnimationDownloadWorker downloadWorker,
			IAnimationDecodeWorker decodeWorker,
			IAnimationCacheWorker animationCache)
		{
			this._log = log;
			this._requestQueue = requestQueue;
			this._downloadWorker = downloadWorker;
			this._decodeWorker = decodeWorker;
			this._animationCache = animationCache;
		}

		/// <inheritdoc/>
		public void RequestAnimation(Primitive primitive, UUID animationId)
		{
			//_log.LogInformation($"Request AnimationId: {animationId}");
			AnimationRequest request = new AnimationRequest
			{
				Primitive = primitive,
				UUID = animationId
			};
			this._requestQueue.Enqueue(request);

		}

		/// <inheritdoc/>
		void IDisposable.Dispose()
		{
			_animationCache.Dispose();
			_decodeWorker.Dispose();
			_downloadWorker.Dispose();
			GC.SuppressFinalize(this);
		}
	}
}

