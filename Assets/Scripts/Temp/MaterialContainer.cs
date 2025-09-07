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
			public bool ready = false;
			public Material materialOpaque;
			private MaterialPropertyBlock _opaqueBlock;
			
			public Material materialAlpha;
			private MaterialPropertyBlock _alphaBlock;
			
			public Material materialOpaqueFullbright;
			private MaterialPropertyBlock _opaqueFBBlock;
			public Material materialAlphaFullbright;
			private MaterialPropertyBlock _alphaFBBlock;
			
			public Texture2D texture;
			public uint components;
			public UUID uuid;

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

			//attempted branchless refactor
			public Material GetMaterial(Color color, float glow, bool fullbright)
			{
				// An array of function delegates
				Func<Color, float, bool, Material>[] functions = new Func<Color, float, bool, Material>[]
				{
					GetMaterialOpaque,
					GetMaterialAlpha
				};

				// Using the value of components as an index into the array
				return functions[components - 3](color, glow, fullbright);
			}


			public Material GetMaterialOpaque(Color color, float glow, bool fullbright)
			{
				Material mat = fullbright ? ResourceCache.opaqueFullbrightMaterial : ResourceCache.opaqueMaterial;

				if (materialOpaque == null)
				{
					materialOpaque = Material.Instantiate(mat);
#if UNITY_EDITOR
					materialOpaque.name = $"opaque {uuid}";
#endif
					materialOpaque.SetTexture(ClientManager.DiffuseName, texture);
				}

				if (components == 4 || color.a < 0.999f) return GetMaterialAlpha(color, glow, fullbright);

				bool colorChange = color.r < 0.999f || color.g < 0.999f || color.b < 0.999f || glow > 0.001f || fullbright;

				if (colorChange)
				{
					mat = Material.Instantiate(materialOpaque);
					mat.SetColor(ClientManager.ColorName, color);
					if (fullbright || glow > 0.001f)
					{
						Color emissiveColor = color * (fullbright ? 1.0001f : ((1f + glow) * 2f));
						mat.SetColor(ClientManager.EmissiveColorName, emissiveColor);
						mat.SetTexture(ClientManager.EmissiveMapName, texture);
					}
					return mat;
				}

				return materialOpaque;
			}
			public Material GetMaterialAlpha(Color color, float glow, bool fullbright)
			{
				Material mat = fullbright ? ResourceCache.alphaFullbrightMaterial : ResourceCache.alphaMaterial;

				if (materialAlpha == null)
				{
					materialAlpha = Material.Instantiate(mat);
#if UNITY_EDITOR
					materialAlpha.name = $"alpha {uuid}";
#endif
					materialAlpha.SetTexture(ClientManager.DiffuseName, texture);
				}

				bool colorChange = color.r < 0.999f || color.g < 0.999f || color.b < 0.999f || color.a < 0.999f || glow > 0.001f || fullbright;

				if (colorChange)
				{
					mat = Material.Instantiate(materialAlpha);
					mat.SetColor(ClientManager.ColorName, color);
					if (fullbright || glow > 0.001f)
					{
						Color emissiveColor = color * (fullbright ? 1.0001f : ((1f + glow) * 2f));
						mat.SetColor(ClientManager.EmissiveColorName, emissiveColor);
						mat.SetTexture(ClientManager.EmissiveMapName, texture);
					}
					return mat;
				}

				return materialAlpha;
			}


			public MaterialContainer(UUID uuid, Texture2D texture, uint components)
			{
				this.uuid = uuid;
				this.texture = texture;
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
				if (materialOpaque != null)
				{
					if (Application.isPlaying)
						UnityEngine.Object.Destroy(materialOpaque);
					else
						UnityEngine.Object.DestroyImmediate(materialOpaque);
					materialOpaque = null;
				}

				if (materialAlpha != null)
				{
					if (Application.isPlaying)
						UnityEngine.Object.Destroy(materialAlpha);
					else
						UnityEngine.Object.DestroyImmediate(materialAlpha);
					materialAlpha = null;
				}

				if (materialOpaqueFullbright != null)
				{
					if (Application.isPlaying)
						UnityEngine.Object.Destroy(materialOpaqueFullbright);
					else
						UnityEngine.Object.DestroyImmediate(materialOpaqueFullbright);
					materialOpaqueFullbright = null;
				}

				if (materialAlphaFullbright != null)
				{
					if (Application.isPlaying)
						UnityEngine.Object.Destroy(materialAlphaFullbright);
					else
						UnityEngine.Object.DestroyImmediate(materialAlphaFullbright);
					materialAlphaFullbright = null;
				}

				// Note: We don't destroy the texture here as it might be shared
				// The texture disposal should be handled by the texture manager
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