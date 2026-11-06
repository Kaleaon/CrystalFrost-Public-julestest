using UnityEngine;
using OpenMetaverse;

namespace CrystalFrost.Assets
{
    /// <summary>
    /// Represents a request to bind a material to a renderer submesh on the main thread.
    /// Serializes material application operations to eliminate multithreading race conditions and prevent material cloning.
    /// </summary>
    public class MaterialBindingRequest
    {
        public UUID TextureUuid { get; set; }
        public Renderer Renderer { get; set; }
        public int SubMeshIndex { get; set; }
        public Color Color { get; set; }
        public float Glow { get; set; }
        public bool Fullbright { get; set; }
        public Material Material { get; set; }

        public MaterialBindingRequest()
        {
        }

        public MaterialBindingRequest(Renderer renderer, int subMeshIndex, Material material, UUID textureUuid = default, Color color = default, float glow = 0f, bool fullbright = false)
        {
            Renderer = renderer;
            SubMeshIndex = subMeshIndex;
            Material = material;
            TextureUuid = textureUuid;
            Color = color;
            Glow = glow;
            Fullbright = fullbright;
        }
    }
}
