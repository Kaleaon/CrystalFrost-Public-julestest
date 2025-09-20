using CrystalFrost.Lib;

namespace CrystalFrost.Assets.Mesh
{
	/// <summary>
	/// Represents a queue of decoded meshes that are ready for
	/// the main thread to pick up and read into Graphics memory.
	/// </summary>
	public interface IDecodedMeshQueue : IConcurrentQueue<MeshRequest> { }

	/// <inheritdoc/>
	public class DecodedMeshQueue : AbstractedConcurrentQueue<MeshRequest>, IDecodedMeshQueue { }

	/// <summary>
	/// A queue for meshes that have been download but not cached.
	/// </summary>
	public interface IDownloadedMeshCacheQueue : IConcurrentQueue<MeshRequest> { }
	/// <inheritdoc/>
	public class DownloadedMeshCacheQueue : AbstractedConcurrentQueue<MeshRequest>, IDownloadedMeshCacheQueue { }


	/// <summary>
	/// A queue for meshes that have been dowloaded, but have not been converted.
	/// </summary>
	public interface IDownloadedMeshQueue : IConcurrentQueue<MeshRequest> { }
	/// <inheritdoc/>
	public class DownloadedMeshQueue : AbstractedConcurrentQueue<MeshRequest>, IDownloadedMeshQueue { }

	/// <summary>
	/// A queue for meshes that have not been downloaded.
	/// </summary>
	public interface IMeshDownloadRequestQueue : IConcurrentQueue<MeshRequest> { }
	/// <inheritdoc/>
	public class MeshDownloadRequestQueue : AbstractedConcurrentQueue<MeshRequest>, IMeshDownloadRequestQueue { }

	/// <summary>
	/// A queue for meshes that have been requested (from cache or downloads)
	/// </summary>
	public interface IMeshRequestQueue : IConcurrentQueue<MeshRequest> { }
	/// <inheritdoc/>
	public class MeshRequestQueue : AbstractedConcurrentQueue<MeshRequest>, IMeshRequestQueue { }

}