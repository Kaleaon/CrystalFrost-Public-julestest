using OpenMetaverse;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class WorldMap : MonoBehaviour
{
    [Header("UI References")]
    public GameObject worldMapWindow;
    public Button closeButton;
    public Button teleportButton;
    public Button showOnMapButton;
    public RawImage mapImage;
    public Transform mapContainer;
    public ScrollRect mapScrollRect;
    
    [Header("Search")]
    public TMP_InputField searchField;
    public Button searchButton;
    public Transform searchResultsRoot;
    public GameObject searchResultPrefab;
    
    [Header("Info Panel")]
    public TMP_Text regionNameText;
    public TMP_Text regionInfoText;
    public TMP_Text maturityRatingText;
    public TMP_Text ownerText;
    
    [Header("Map Controls")]
    public Button zoomInButton;
    public Button zoomOutButton;
    public Slider zoomSlider;
    public TMP_Text coordinatesText;
    
    private GridClient client;
    private Dictionary<ulong, GridRegion> regionCache = new();
    private Dictionary<string, List<GridRegion>> searchResults = new();
    private GridRegion selectedRegion;
    private Vector2 selectedMapPosition;
    private float currentZoom = 1.0f;
    private Texture2D mapTexture;
    
    // Map constants
    private const int MAP_SIZE = 1024;
    private const int REGION_SIZE = 256;
    
    void Awake()
    {
        worldMapWindow.SetActive(false);
        SetupUI();
        InitializeMap();
    }

    void SetupUI()
    {
        if (closeButton) closeButton.onClick.AddListener(CloseWorldMap);
        if (teleportButton) teleportButton.onClick.AddListener(TeleportToSelection);
        if (showOnMapButton) showOnMapButton.onClick.AddListener(ShowCurrentRegionOnMap);
        if (searchButton) searchButton.onClick.AddListener(SearchRegions);
        if (searchField) searchField.onEndEdit.AddListener((text) => { if (Input.GetKeyDown(KeyCode.Return)) SearchRegions(); });
        
        if (zoomInButton) zoomInButton.onClick.AddListener(() => ZoomMap(1.2f));
        if (zoomOutButton) zoomOutButton.onClick.AddListener(() => ZoomMap(0.8f));
        if (zoomSlider) zoomSlider.onValueChanged.AddListener(OnZoomSliderChanged);
        
        // Setup map click detection
        if (mapImage)
        {
            var eventTrigger = mapImage.gameObject.GetComponent<UnityEngine.EventSystems.EventTrigger>();
            if (eventTrigger == null)
            {
                eventTrigger = mapImage.gameObject.AddComponent<UnityEngine.EventSystems.EventTrigger>();
            }
            
            var clickEntry = new UnityEngine.EventSystems.EventTrigger.Entry();
            clickEntry.eventID = UnityEngine.EventSystems.EventTriggerType.PointerClick;
            clickEntry.callback.AddListener((data) => OnMapClick((UnityEngine.EventSystems.PointerEventData)data));
            eventTrigger.triggers.Add(clickEntry);
        }
    }

    void InitializeMap()
    {
        // Create a basic map texture
        mapTexture = new Texture2D(MAP_SIZE, MAP_SIZE, TextureFormat.RGB24, false);
        
        // Fill with ocean blue
        Color oceanColor = new Color(0.1f, 0.3f, 0.8f);
        Color[] pixels = new Color[MAP_SIZE * MAP_SIZE];
        for (int i = 0; i < pixels.Length; i++)
        {
            pixels[i] = oceanColor;
        }
        mapTexture.SetPixels(pixels);
        mapTexture.Apply();
        
        if (mapImage) mapImage.texture = mapTexture;
    }

    void Start()
    {
        client = ClientManager.client;
        
        if (client != null)
        {
            client.Grid.GridRegion += OnGridRegion;
            client.Grid.GridItems += OnGridItems;
        }
    }

    void OnDestroy()
    {
        if (client != null)
        {
            client.Grid.GridRegion -= OnGridRegion;
            client.Grid.GridItems -= OnGridItems;
        }
    }

    public void ShowWorldMap()
    {
        worldMapWindow.SetActive(true);
        RefreshCurrentLocation();
        RequestNearbyRegions();
    }

    public void CloseWorldMap()
    {
        worldMapWindow.SetActive(false);
    }

    void RefreshCurrentLocation()
    {
        if (client == null || client.Network.CurrentSim == null) return;
        
        var currentSim = client.Network.CurrentSim;
        if (regionNameText) regionNameText.text = currentSim.Name;
        
        uint x, y;
        Utils.LongToUInts(currentSim.Handle, out x, out y);
        
        if (coordinatesText) coordinatesText.text = $"<{x/256}, {y/256}>";
        
        // Update info panel
        UpdateInfoPanel(currentSim);
        
        // Center map on current location
        CenterMapOnRegion(x, y);
    }

    void UpdateInfoPanel(Simulator sim)
    {
        if (regionNameText) regionNameText.text = sim.Name;
        if (regionInfoText) regionInfoText.text = $"Size: {sim.SizeX}x{sim.SizeY}";
        
        // This would need more detailed region info from the grid
        if (maturityRatingText) maturityRatingText.text = "General"; // Default
        if (ownerText) ownerText.text = "Loading...";
    }

    void UpdateInfoPanel(GridRegion region)
    {
        if (regionNameText) regionNameText.text = region.Name;
        if (regionInfoText) regionInfoText.text = $"Size: {region.X}x{region.Y}";
        if (maturityRatingText) maturityRatingText.text = region.Access.ToString();
        if (ownerText) ownerText.text = "Region Owner"; // Would need to fetch this
    }

    void OnGridRegion(object sender, GridRegionEventArgs e)
    {
        UnityMainThreadDispatcher.Instance().Enqueue(() =>
        {
            regionCache[e.Region.RegionHandle] = e.Region;
            UpdateMapRegion(e.Region);
        });
    }

    void OnGridItems(object sender, GridItemsEventArgs e)
    {
        UnityMainThreadDispatcher.Instance().Enqueue(() =>
        {
            // Handle grid items (places, events, etc.)
            foreach (var item in e.Items)
            {
                Debug.Log($"Grid item: {item.Name} at {item.GlobalX}, {item.GlobalY}");
            }
        });
    }

    void UpdateMapRegion(GridRegion region)
    {
        if (mapTexture == null) return;
        
        // Calculate pixel position on map
        int pixelX = (int)(region.X / REGION_SIZE);
        int pixelY = (int)(region.Y / REGION_SIZE);
        
        // Ensure coordinates are within map bounds
        if (pixelX < 0 || pixelX >= MAP_SIZE || pixelY < 0 || pixelY >= MAP_SIZE) return;
        
        // Color region based on maturity/access
        Color regionColor = GetRegionColor(region);
        
        // Draw a small square for the region
        int regionPixelSize = 4;
        for (int x = 0; x < regionPixelSize && pixelX + x < MAP_SIZE; x++)
        {
            for (int y = 0; y < regionPixelSize && pixelY + y < MAP_SIZE; y++)
            {
                mapTexture.SetPixel(pixelX + x, pixelY + y, regionColor);
            }
        }
        
        mapTexture.Apply();
    }

    Color GetRegionColor(GridRegion region)
    {
        switch (region.Access)
        {
            case SimAccess.PG:
                return Color.green;      // General regions
            case SimAccess.Mature:
                return Color.yellow;     // Moderate regions  
            case SimAccess.Adult:
                return Color.red;        // Adult regions
            default:
                return Color.white;      // Unknown
        }
    }

    void OnMapClick(UnityEngine.EventSystems.PointerEventData eventData)
    {
        if (mapImage == null) return;
        
        Vector2 localPoint;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            mapImage.rectTransform, eventData.position, eventData.pressEventCamera, out localPoint);
        
        // Convert to map coordinates
        var rect = mapImage.rectTransform.rect;
        float normalizedX = (localPoint.x - rect.x) / rect.width;
        float normalizedY = (localPoint.y - rect.y) / rect.height;
        
        // Convert to world coordinates
        uint worldX = (uint)(normalizedX * MAP_SIZE * REGION_SIZE);
        uint worldY = (uint)(normalizedY * MAP_SIZE * REGION_SIZE);
        
        selectedMapPosition = new Vector2(worldX, worldY);
        
        if (coordinatesText) coordinatesText.text = $"<{worldX/REGION_SIZE}, {worldY/REGION_SIZE}>";
        
        // Check if we have info about this region
        ulong handle = Utils.UIntsToLong(worldX & 0xFFFFFF00, worldY & 0xFFFFFF00);
        if (regionCache.ContainsKey(handle))
        {
            selectedRegion = regionCache[handle];
            UpdateInfoPanel(selectedRegion);
        }
        else
        {
            selectedRegion = null;
            if (regionNameText) regionNameText.text = $"Unknown Region";
            if (regionInfoText) regionInfoText.text = $"Click to request info";
            
            // Request region info
            RequestRegionInfo(worldX, worldY);
        }
    }

    void RequestRegionInfo(uint x, uint y)
    {
        if (client == null) return;
        
        // Request map info for this region
        client.Grid.RequestMapRegion(client.Network.CurrentSim.Name, GridLayerType.Objects);
    }

    void RequestNearbyRegions()
    {
        if (client == null || client.Network.CurrentSim == null) return;
        
        var currentSim = client.Network.CurrentSim;
        uint x, y;
        Utils.LongToUInts(currentSim.Handle, out x, out y);
        
        // Request map info for nearby regions
        client.Grid.RequestMapRegion(currentSim.Name, GridLayerType.Objects);
        
        // Request a larger area around current position
        for (int offsetX = -5; offsetX <= 5; offsetX++)
        {
            for (int offsetY = -5; offsetY <= 5; offsetY++)
            {
                uint regionX = x + (uint)(offsetX * REGION_SIZE);
                uint regionY = y + (uint)(offsetY * REGION_SIZE);
                
                // This would request info about regions in the area
                // The actual implementation would depend on the grid protocol
            }
        }
    }

    void CenterMapOnRegion(uint x, uint y)
    {
        if (mapScrollRect == null) return;
        
        // Calculate normalized position on map
        float normalizedX = (float)x / (MAP_SIZE * REGION_SIZE);
        float normalizedY = (float)y / (MAP_SIZE * REGION_SIZE);
        
        // Center the scroll rect on this position
        mapScrollRect.normalizedPosition = new Vector2(normalizedX, normalizedY);
    }

    public void ShowCurrentRegionOnMap()
    {
        RefreshCurrentLocation();
    }

    public void TeleportToSelection()
    {
        if (selectedRegion != null)
        {
            TeleportToRegion(selectedRegion.Name, Vector3.zero);
        }
        else if (selectedMapPosition != Vector2.zero)
        {
            // Try to teleport to coordinates
            uint regionX = (uint)selectedMapPosition.x / REGION_SIZE;
            uint regionY = (uint)selectedMapPosition.y / REGION_SIZE;
            string regionName = $"<{regionX},{regionY}>";
            
            Vector3 localPosition = new Vector3(
                selectedMapPosition.x % REGION_SIZE,
                0,
                selectedMapPosition.y % REGION_SIZE
            );
            
            TeleportToRegion(regionName, localPosition);
        }
    }

    void TeleportToRegion(string regionName, Vector3 position)
    {
        if (client == null) return;
        
        Debug.Log($"Teleporting to {regionName} at {position}");
        
        // Convert Unity Vector3 to OMV Vector3
        var omvPosition = new OpenMetaverse.Vector3(position.x, position.y, position.z);
        var omvLookAt = new OpenMetaverse.Vector3(1, 0, 0);
        
        client.Self.Teleport(regionName, omvPosition, omvLookAt);
        
        CloseWorldMap();
    }

    public void SearchRegions()
    {
        if (searchField == null || string.IsNullOrEmpty(searchField.text)) return;
        
        string searchTerm = searchField.text.Trim();
        StartCoroutine(PerformRegionSearch(searchTerm));
    }

    IEnumerator PerformRegionSearch(string searchTerm)
    {
        // Clear previous search results
        foreach (Transform child in searchResultsRoot)
        {
            Destroy(child.gameObject);
        }
        
        // This would typically query the grid directory service
        // For now, we'll search through cached regions
        var results = new List<GridRegion>();
        
        foreach (var region in regionCache.Values)
        {
            if (region.Name.ToLower().Contains(searchTerm.ToLower()))
            {
                results.Add(region);
            }
        }
        
        // Display search results
        foreach (var region in results)
        {
            CreateSearchResultItem(region);
        }
        
        yield return null;
    }

    void CreateSearchResultItem(GridRegion region)
    {
        if (searchResultPrefab == null || searchResultsRoot == null) return;
        
        var resultObj = Instantiate(searchResultPrefab, searchResultsRoot);
        var button = resultObj.GetComponent<Button>();
        var text = resultObj.GetComponentInChildren<TMP_Text>();
        
        if (text) text.text = $"{region.Name} <{region.X/REGION_SIZE},{region.Y/REGION_SIZE}>";
        
        if (button)
        {
            button.onClick.AddListener(() =>
            {
                selectedRegion = region;
                UpdateInfoPanel(region);
                CenterMapOnRegion(region.X, region.Y);
                
                // Update coordinates display
                if (coordinatesText) coordinatesText.text = $"<{region.X/REGION_SIZE}, {region.Y/REGION_SIZE}>";
            });
        }
    }

    void ZoomMap(float zoomFactor)
    {
        currentZoom *= zoomFactor;
        currentZoom = Mathf.Clamp(currentZoom, 0.1f, 5.0f);
        
        if (mapContainer)
        {
            mapContainer.localScale = Vector3.one * currentZoom;
        }
        
        if (zoomSlider)
        {
            zoomSlider.value = currentZoom;
        }
    }

    void OnZoomSliderChanged(float value)
    {
        currentZoom = value;
        if (mapContainer)
        {
            mapContainer.localScale = Vector3.one * currentZoom;
        }
    }
}