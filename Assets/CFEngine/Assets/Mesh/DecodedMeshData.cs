using OpenMetaverse;
using OpenMetaverse.Rendering;
using System.Collections.Generic;

namespace CrystalFrost.Assets.Mesh
{
    /// <summary>
    /// Represents a decoded mesh.
    /// </summary>
    public class DecodedMesh
    {
        /// <summary>
        /// The raw mesh data.
        /// </summary>
        public List<RawMeshData> meshData = new();
        /// <summary>
        /// Whether the mesh is skinned.
        /// </summary>
		public bool isSkinned = false;
        /// <summary>
        /// The bind shape matrix.
        /// </summary>
		public UnityEngine.Matrix4x4 bindShapeMatrix = UnityEngine.Matrix4x4.identity;
        /// <summary>
        /// The pelvis offset matrix.
        /// </summary>
		public UnityEngine.Matrix4x4 pelvisOffsetMatrix = UnityEngine.Matrix4x4.identity;
        /// <summary>
        /// The joints of the mesh.
        /// </summary>
		public JointInfo[] joints = null;
        /// <summary>
        /// The asset ID of the mesh.
        /// </summary>
		public UUID assetId;
    }
}
