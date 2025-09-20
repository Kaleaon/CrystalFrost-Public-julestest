using Microsoft.Extensions.Logging;
using OpenMetaverse;
using System;
using UnityEngine;

namespace CrystalFrost.Assets.Mesh
{
    /// <summary>
    /// Defines the interface for a mesh manager.
    /// </summary>
    public interface IMeshManager : IDisposable
    {
        /// <summary>
        /// Gets the queue of decoded meshes that are ready to be processed.
        /// </summary>
        IDecodedMeshQueue ReadyMeshes { get; }
        /// <summary>
        /// Requests a mesh.
        /// </summary>
        /// <param name="gameObject">The game object to which the mesh will be applied.</param>
        /// <param name="primitive">The primitive to which the mesh belongs.</param>
        /// <param name="uuid">The UUID of the mesh to request.</param>
        /// <param name="meshHolder">The game object that will hold the mesh.</param>
        void RequestMesh (GameObject gameObject, Primitive primitive, UUID uuid, GameObject meshHolder);
    }

    /// <summary>
    /// Manages meshes.
    /// </summary>
    public class MeshManager : IMeshManager
    {
        private readonly ILogger<MeshManager> _log;
        private readonly IMeshRequestQueue _requestQueue;
        private readonly IMeshDownloadWorker _downloadWorker;
        private readonly IMeshDecodeWorker _decodeWorker;
		private readonly IMeshCacheWorker _meshCache;

        /// <inheritdoc/>
		public IDecodedMeshQueue ReadyMeshes { get; }

		/// <summary>
		/// Initializes a new instance of the <see cref="MeshManager"/> class.
		/// </summary>
		/// <param name="logger">A logger for logging messages.</param>
		/// <param name="readyMeshQueue">A queue of decoded meshes.</param>
		/// <param name="requestQueue">A queue of mesh requests.</param>
		/// <param name="downloadWorker">The worker that downloads meshes.</param>
		/// <param name="decodeWorker">The worker that decodes meshes.</param>
		/// <param name="meshCache">The mesh cache.</param>
		public MeshManager(
            ILogger<MeshManager> logger,
            IDecodedMeshQueue readyMeshQueue,
            IMeshRequestQueue requestQueue,
            IMeshDownloadWorker downloadWorker,
            IMeshDecodeWorker decodeWorker,
			IMeshCacheWorker meshCache)
        {
            _log = logger;
            _requestQueue = requestQueue;
            _downloadWorker = downloadWorker;
            _decodeWorker = decodeWorker;
            ReadyMeshes = readyMeshQueue;
            _meshCache = meshCache;
		}

        /// <inheritdoc/>
        public void RequestMesh(GameObject gameObject, Primitive primitive, UUID uuid, GameObject meshHolder)
		{
			_log.MeshRequested(uuid);
            MeshRequest request = new MeshRequest
            {
                GameObject = gameObject,
                Primitive = primitive,
                UUID = uuid,
                MeshHolder = meshHolder
            };
			_requestQueue.Enqueue(request);
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            _meshCache.Dispose();
            _decodeWorker.Dispose();
            _downloadWorker.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}
