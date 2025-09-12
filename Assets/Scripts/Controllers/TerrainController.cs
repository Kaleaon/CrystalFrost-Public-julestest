using UnityEngine;
using CrystalFrost;
using Microsoft.Extensions.Logging;

namespace CrystalFrost.Controllers
{
    /// <summary>
    /// Handles terrain creation and management for simulators
    /// </summary>
    public class TerrainController : MonoBehaviour
    {
        [Header("Terrain Settings")]
        public Terrain terrainPrefab;
        
        private ILogger<TerrainController> _logger;

        private void Awake()
        {
            _logger = Services.GetService<ILogger<TerrainController>>();
        }

        public void CreateSimulatorTerrainTiles(string name, uint handle, uint sizeX, uint sizeY)
        {
            _logger.LogInformation($"Creating terrain tiles for {name} with size {sizeX}x{sizeY}");

            GameObject terrainRoot = new GameObject(name);
            Terrain[,] terrains = new Terrain[sizeX, sizeY];

            for (uint x = 0; x < sizeX; x++)
            {
                for (uint y = 0; y < sizeY; y++)
                {
                    // Create terrain tile
                    GameObject terrainGO = CreateTerrainTile(terrainRoot.transform, (int)x, (int)y);
                    terrains[x, y] = terrainGO.GetComponent<Terrain>();
                    
                    // Position the terrain tile
                    Vector3 position = new Vector3(x * 256f, 0, y * 256f);
                    terrainGO.transform.position = position;
                }
            }

            // Set up neighbor connections for seamless terrain
            SetupTerrainNeighbors(terrains, sizeX, sizeY);
        }

        private GameObject CreateTerrainTile(Transform parent, int x, int y)
        {
            GameObject terrainGO;
            
            if (terrainPrefab != null)
            {
                terrainGO = Instantiate(terrainPrefab.gameObject, parent);
            }
            else
            {
                // Create terrain from scratch if no prefab is available
                terrainGO = Terrain.CreateTerrainGameObject(null);
                terrainGO.transform.SetParent(parent);
            }

            terrainGO.name = $"Terrain_{x}_{y}";
            
            return terrainGO;
        }

        private void SetupTerrainNeighbors(Terrain[,] terrains, uint sizeX, uint sizeY)
        {
            for (uint x = 0; x < sizeX; x++)
            {
                for (uint y = 0; y < sizeY; y++)
                {
                    Terrain current = terrains[x, y];
                    
                    // Set neighbors for seamless terrain transitions
                    Terrain left = (x > 0) ? terrains[x - 1, y] : null;
                    Terrain top = (y < sizeY - 1) ? terrains[x, y + 1] : null;
                    Terrain right = (x < sizeX - 1) ? terrains[x + 1, y] : null;
                    Terrain bottom = (y > 0) ? terrains[x, y - 1] : null;
                    
                    current.SetNeighbors(left, top, right, bottom);
                }
            }
        }

        public uint GetNorth(uint handle) => handle + 256;
        public uint GetSouth(uint handle) => handle - 256;
        public uint GetEast(uint handle) => handle + (256 << 16);
        public uint GetWest(uint handle) => handle - (256 << 16);

        // Handle the region handle calculations more properly
        public ulong GetNorth(ulong handle) => (handle & 0xFFFFFFFF00000000) | ((uint)handle + 256);
        public ulong GetSouth(ulong handle) => (handle & 0xFFFFFFFF00000000) | ((uint)handle - 256);
        public ulong GetEast(ulong handle) => (((ulong)((uint)(handle >> 32) + 256)) << 32) | (uint)handle;
        public ulong GetWest(ulong handle) => (((ulong)((uint)(handle >> 32) - 256)) << 32) | (uint)handle;
    }
}