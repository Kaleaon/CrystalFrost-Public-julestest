using OpenMetaverse;
using System.Collections.Generic;

namespace CrystalFrost.WorldState
{
	/// <summary>
	/// Represents a region in the world.
	/// </summary>
	public class Region
	{
		/// <summary>
		/// The name of the region.
		/// </summary>
		public string Name;
		/// <summary>
		/// The UUID of the region.
		/// </summary>
		public UUID RegionId;
		/// <summary>
		/// The objects in the region.
		/// </summary>
		public List<SimObject> Objects = new();
		/// <summary>
		/// The handle of the region.
		/// </summary>
		public ulong Handle;
	}
}
