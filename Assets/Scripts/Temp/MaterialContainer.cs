using System;
using OpenMetaverse;
using UnityEngine;
using Material = UnityEngine.Material;

// todo, material property block
// we need to run with game object and use mesh renderer to change property block
namespace Temp
{
	/// <summary>
	/// Container for managing materials and textures with proper disposal pattern.
	/// Implements IDisposable to prevent memory leaks from Unity resources.
	/// </summary>
	public class MaterialContainer : IDisposable
	{
		#region Constants
		private const float ColorThreshold = 0.999f;
		private const float GlowThreshold = 0.001f;
		private const float FullbrightEmissiveMultiplier = 1.0001f;
		private const float GlowEmissiveMultiplier = 2f;
		#endregion

		#region Public Fields
		public bool ready = false;
		public readonly uint components;
		public readonly UUID uuid;
		public readonly Texture2D texture;
		#endregion

		#region Material Fields
		public Material materialOpaque;
		private MaterialPropertyBlock _opaqueBlock;
		
		public Material materialAlpha;
		private MaterialPropertyBlock _alphaBlock;
		
		public Material materialOpaqueFullbright;
		private MaterialPropertyBlock _opaqueFBBlock;
		public Material materialAlphaFullbright;
		private MaterialPropertyBlock _alphaFBBlock;
		#endregion

			//attempting to rewrite this to branchless
			/*public Material GetMaterial(Color color, float glow, bool fullbright)
			{
				if (components == 3)
				{
					return GetMaterialOpaque(color, glow, fullbright);
				}
				else// if(components == 4)
				{
					return GetMaterialOpaque(color, glow, fullbright);
				}
			}*/

			/// <summary>
			/// Gets the appropriate material based on texture components and rendering parameters.
			/// Optimized for performance with minimal allocations.
			/// </summary>
			/// <param name="color">The color to apply to the material</param>
			/// <param name="glow">The glow/emission intensity</param>
			/// <param name="fullbright">Whether to use fullbright rendering</param>
			/// <returns>The configured material instance</returns>
			public Material GetMaterial(Color color, float glow, bool fullbright)
			{
				// Simple conditional is more performant than function delegate arrays
				// Avoids unnecessary allocations and indirect calls
				return components == 3 ? GetMaterialOpaque(color, glow, fullbright) : GetMaterialAlpha(color, glow, fullbright);
			}


			/// <summary>
			/// Gets or creates an opaque material with the specified properties.
			/// Implements efficient caching to minimize material instantiation.
			/// </summary>
			/// <param name="color">The base color to apply</param>
			/// <param name="glow">The glow/emission intensity (0-1)</param>
			/// <param name="fullbright">Whether to enable fullbright rendering</param>
			/// <returns>A configured opaque material</returns>
			public Material GetMaterialOpaque(Color color, float glow, bool fullbright)
			{
				
				// Early fallback for alpha components
				if (components == 4 || color.a < ColorThreshold) 
					return GetMaterialAlpha(color, glow, fullbright);

				// Lazy initialization of base material
				if (materialOpaque == null)
				{
					Material baseMat = fullbright ? ResourceCache.opaqueFullbrightMaterial : ResourceCache.opaqueMaterial;
					materialOpaque = Material.Instantiate(baseMat);
					
#if UNITY_EDITOR
					materialOpaque.name = $"opaque_{uuid}";
#endif
					materialOpaque.SetTexture(ClientManager.DiffuseName, texture);
				}

				// Check if we need color modifications
				bool needsColorModification = color.r < ColorThreshold || color.g < ColorThreshold || 
				                              color.b < ColorThreshold || glow > GlowThreshold || fullbright;

				if (needsColorModification)
				{
					// Create a modified instance for non-default properties
					Material modifiedMat = Material.Instantiate(materialOpaque);
					modifiedMat.SetColor(ClientManager.ColorName, color);
					
					if (fullbright || glow > GlowThreshold)
					{
						float emissiveMultiplier = fullbright ? 1.0001f : ((1f + glow) * 2f);
						Color emissiveColor = color * emissiveMultiplier;
						modifiedMat.SetColor(ClientManager.EmissiveColorName, emissiveColor);
						modifiedMat.SetTexture(ClientManager.EmissiveMapName, texture);
					}
					
					return modifiedMat;
				}

				return materialOpaque;
			}
			/// <summary>
			/// Gets or creates an alpha-blended material with the specified properties.
			/// Handles transparency and emission effects efficiently.
			/// </summary>
			/// <param name="color">The base color including alpha channel</param>
			/// <param name="glow">The glow/emission intensity (0-1)</param>
			/// <param name="fullbright">Whether to enable fullbright rendering</param>
			/// <returns>A configured alpha material</returns>
			public Material GetMaterialAlpha(Color color, float glow, bool fullbright)
			{
				
				// Lazy initialization of base alpha material
				if (materialAlpha == null)
				{
					Material baseMat = fullbright ? ResourceCache.alphaFullbrightMaterial : ResourceCache.alphaMaterial;
					materialAlpha = Material.Instantiate(baseMat);
					
#if UNITY_EDITOR
					materialAlpha.name = $"alpha_{uuid}";
#endif
					materialAlpha.SetTexture(ClientManager.DiffuseName, texture);
				}

				// Check if we need color modifications
				bool needsColorModification = color.r < ColorThreshold || color.g < ColorThreshold || 
				                              color.b < ColorThreshold || color.a < ColorThreshold || 
				                              glow > GlowThreshold || fullbright;

				if (needsColorModification)
				{
					// Create a modified instance for non-default properties
					Material modifiedMat = Material.Instantiate(materialAlpha);
					modifiedMat.SetColor(ClientManager.ColorName, color);
					
					if (fullbright || glow > GlowThreshold)
					{
						float emissiveMultiplier = fullbright ? 1.0001f : ((1f + glow) * 2f);
						Color emissiveColor = color * emissiveMultiplier;
						modifiedMat.SetColor(ClientManager.EmissiveColorName, emissiveColor);
						modifiedMat.SetTexture(ClientManager.EmissiveMapName, texture);
					}
					
					return modifiedMat;
				}

				return materialAlpha;
			}


			/// <summary>
			/// Initializes a new MaterialContainer with immutable properties.
			/// </summary>
			/// <param name="uuid">The unique identifier for this material</param>
			/// <param name="texture">The texture to apply to materials</param>
			/// <param name="components">The number of color components (3 for RGB, 4 for RGBA)</param>
			public MaterialContainer(UUID uuid, Texture2D texture, uint components)
			{
				this.uuid = uuid;
				this.texture = texture ?? throw new ArgumentNullException(nameof(texture));
				this.components = components;
			}

			#region IDisposable Implementation

			private bool _disposed = false;

			/// <summary>
			/// Disposes of Unity resources to prevent memory leaks.
			/// </summary>
			public void Dispose()
			{
				Dispose(true);
				GC.SuppressFinalize(this);
			}

			/// <summary>
			/// Protected dispose method following standard disposal pattern.
			/// </summary>
			/// <param name="disposing">True if disposing from Dispose(), false if from finalizer</param>
			protected virtual void Dispose(bool disposing)
			{
				if (_disposed) return;

				if (disposing)
				{
					// Dispose managed resources
					DisposeUnityResources();
				}

				_disposed = true;
			}

			/// <summary>
			/// Properly dispose Unity materials and textures to prevent memory leaks.
			/// </summary>
			private void DisposeUnityResources()
			{
				// Destroy materials if they were instantiated
				DisposeMaterial(ref materialOpaque);
				DisposeMaterial(ref materialAlpha);
				DisposeMaterial(ref materialOpaqueFullbright);
				DisposeMaterial(ref materialAlphaFullbright);

				// Note: We don't destroy the texture here as it might be shared
				// The texture disposal should be handled by the texture manager
			}

			/// <summary>
			/// Helper method to dispose a Unity material safely.
			/// </summary>
			private void DisposeMaterial(ref Material material)
			{
				if (material != null)
				{
					if (Application.isPlaying)
						UnityEngine.Object.Destroy(material);
					else
						UnityEngine.Object.DestroyImmediate(material);
					material = null;
				}
			}

			/// <summary>
			/// Finalizer to ensure resources are cleaned up if Dispose is not called.
			/// </summary>
			~MaterialContainer()
			{
				Dispose(false);
			}

			#endregion
		}

}