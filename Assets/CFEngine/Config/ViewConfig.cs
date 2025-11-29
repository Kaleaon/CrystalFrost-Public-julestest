
namespace CrystalFrost.Config
{
    /// <summary>
    /// Configuration that effects the computation of the view
    /// </summary>
    public class ViewConfig
    {
        /// <summary>
        /// The name of the configuration subsection.
        /// </summary>
        public const string subsectionName = "View";

        // =============== Frustum Culling ===============
        /// <summary>
        /// Whether frustum culling is enabled.
        /// </summary>
        public bool FrustumCulling { get; set; } = true;
        /// <summary>
        /// The default sphere radius around an object's center to determine if it is in view.
        /// </summary>
        public float DefaultSphereRadius { get; set; } = 40f;
        /// <summary>
        /// The sphere radius for non-mesh sculpts.
        /// </summary>
        public float NonMeshSculptSphereRadius { get; set; } = 20f;
        /// <summary>
        /// The number of milliseconds between checks for new objects to add to the view.
        /// </summary>
        public int NewObjectPollMS { get; set; } = 100;
    }
}
