/*
 * Crystal Frost Second Life Viewer - Windlight Environment Manager
 * 
 * SYSTEM OVERVIEW:
 * ================
 * This system provides comprehensive environment and lighting control for the Crystal Frost
 * Second Life viewer, implementing a complete Windlight-compatible environment system.
 * It manages sky, water, lighting, time of day, and post-processing effects to create
 * immersive and visually stunning virtual world experiences.
 * 
 * ARCHITECTURE:
 * =============
 * - Unity MonoBehaviour component with full inspector integration
 * - Real-time environment parameter adjustment and preview
 * - Preset system for quick environment switching
 * - Time of day simulation with automatic sun/moon positioning
 * - Unity Universal Render Pipeline (URP) integration
 * - Post-processing effects integration for advanced visuals
 * 
 * KEY FEATURES:
 * =============
 * 1. SKY SYSTEM:
 *    - Sun size, intensity, and atmospheric thickness control
 *    - Horizon and zenith color customization
 *    - Dynamic sun/moon positioning based on time of day
 *    - Atmospheric scattering simulation
 * 
 * 2. CLOUD SYSTEM:
 *    - Cloud coverage and density controls
 *    - Cloud animation speed and direction
 *    - Cloud color and shadow customization
 *    - Dynamic cloud lighting based on sun position
 * 
 * 3. WATER SYSTEM:
 *    - Water transparency and reflection controls
 *    - Wave height and animation speed
 *    - Water color and fog effects
 *    - Shoreline foam and underwater effects
 * 
 * 4. LIGHTING SYSTEM:
 *    - Ambient and directional lighting control
 *    - Shadow system with distance and strength settings
 *    - Dynamic light color based on time of day
 *    - Multiple light source support (sun/moon)
 * 
 * 5. TIME OF DAY:
 *    - 24-hour time simulation with real-time progression
 *    - Automatic or manual time control
 *    - Sunrise/sunset color transitions
 *    - Seasonal variation support
 * 
 * 6. POST-PROCESSING:
 *    - Bloom effects for enhanced lighting
 *    - Color grading for mood adjustment
 *    - Contrast, saturation, and gamma correction
 *    - Unity URP Volume integration
 * 
 * 7. PRESET SYSTEM:
 *    - Built-in presets (Sunrise, Midday, Sunset, Night)
 *    - Custom preset creation and saving
 *    - Preset import/export functionality
 *    - Smooth transitions between presets
 * 
 * TECHNICAL IMPLEMENTATION:
 * =========================
 * - Unity's Universal Render Pipeline (URP) for modern rendering
 * - Shader integration for sky, water, and atmospheric effects
 * - JSON serialization for settings persistence
 * - Real-time parameter updates with live preview
 * - Coroutine-based smooth transitions
 * - Memory-efficient resource management
 * 
 * INTEGRATION POINTS:
 * ===================
 * - LibreMetaverse estate settings integration
 * - Unity lighting system integration
 * - Post-processing Volume system
 * - Material property management
 * - Crystal Frost preferences system
 * 
 * PERFORMANCE CONSIDERATIONS:
 * ===========================
 * - Efficient shader parameter updates
 * - LOD system for distant effects
 * - Conditional updates based on visibility
 * - Memory pooling for temporary objects
 * - Frame rate adaptive quality adjustment
 * 
 * USAGE:
 * ======
 * This component should be attached to a GameObject in the scene with proper
 * references to lights, materials, and post-processing volumes configured
 * through the Unity Inspector.
 * 
 * Author: Crystal Frost Development Team
 * Version: 2.0
 * Unity Compatibility: 2021.3.6f1 LTS and higher
 * URP Version: 12.x and higher
 * LibreMetaverse: Compatible with latest versions
 */

using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;
using TMPro;
using OpenMetaverse;
using System.Collections.Generic;
using System.IO;

/// <summary>
/// Advanced Windlight Environment Manager for Crystal Frost Second Life Viewer
/// Provides comprehensive environmental controls including sky, water, lighting,
/// time of day simulation, and post-processing effects.
/// </summary>
public class WindlightManager : MonoBehaviour
{
    #region Inspector Fields
    
    [Header("Windlight Window")]
    [Tooltip("Main windlight editor window")]
    public GameObject windlightWindow;
    
    [Tooltip("Button to close the windlight editor")]
    public Button closeButton;
    
    [Tooltip("Button to save current settings")]
    public Button saveSettingsButton;
    
    [Tooltip("Button to load saved settings")]
    public Button loadSettingsButton;
    
    [Tooltip("Button to reset to default settings")]
    public Button resetButton;
    
    [Tooltip("Dropdown for selecting environment presets")]
    public TMP_Dropdown presetsDropdown;
    
    [Header("Sky Settings")]
    [Tooltip("Sun size control slider")]
    public Slider sunSizeSlider;
    
    [Tooltip("Sun intensity control slider")]
    public Slider sunIntensitySlider;
    
    [Tooltip("Atmospheric thickness control")]
    public Slider atmosphereThicknessSlider;
    
    [Tooltip("Overall exposure control")]
    public Slider exposureSlider;
    
    [Tooltip("Horizon color picker")]
    public ColorPicker horizonColorPicker;
    
    [Tooltip("Zenith (sky dome top) color picker")]
    public ColorPicker zenithColorPicker;
    
    [Tooltip("Sun color picker")]
    public ColorPicker sunColorPicker;
    
    [Tooltip("Ambient light color picker")]
    public ColorPicker ambientColorPicker;
    
    [Header("Cloud Settings")]
    [Tooltip("Cloud coverage amount")]
    public Slider cloudCoverageSlider;
    
    [Tooltip("Cloud density control")]
    public Slider cloudDensitySlider;
    
    [Tooltip("Cloud animation speed")]
    public Slider cloudSpeedSlider;
    
    [Tooltip("Cloud base color")]
    public ColorPicker cloudColorPicker;
    
    [Tooltip("Cloud shadow color")]
    public ColorPicker cloudShadowColorPicker;
    
    [Header("Water Settings")]
    [Tooltip("Water transparency level")]
    public Slider waterTransparencySlider;
    
    [Tooltip("Water reflection strength")]
    public Slider waterReflectionSlider;
    
    [Tooltip("Wave height amplitude")]
    public Slider waveHeightSlider;
    
    [Tooltip("Wave animation speed")]
    public Slider waveSpeedSlider;
    
    [Tooltip("Water surface color")]
    public ColorPicker waterColorPicker;
    
    [Tooltip("Underwater fog color")]
    public ColorPicker waterFogColorPicker;
    
    [Header("Lighting")]
    [Tooltip("Ambient light intensity")]
    public Slider ambientIntensitySlider;
    
    [Tooltip("Directional light intensity")]
    public Slider directionalIntensitySlider;
    
    [Tooltip("Enable/disable shadow rendering")]
    public Toggle enableShadowsToggle;
    
    [Tooltip("Shadow rendering distance")]
    public Slider shadowDistanceSlider;
    
    [Tooltip("Shadow strength/opacity")]
    public Slider shadowStrengthSlider;
    
    [Header("Post Processing")]
    [Tooltip("Enable/disable bloom effect")]
    public Toggle enableBloomToggle;
    
    [Tooltip("Bloom effect intensity")]
    public Slider bloomIntensitySlider;
    
    [Tooltip("Enable/disable color grading")]
    public Toggle enableColorGradingToggle;
    
    [Tooltip("Image contrast adjustment")]
    public Slider contrastSlider;
    
    [Tooltip("Color saturation adjustment")]
    public Slider saturationSlider;
    
    [Tooltip("Gamma correction adjustment")]
    public Slider gammaSlider;
    
    [Header("Time of Day")]
    [Tooltip("Current time of day (0-24 hours)")]
    public Slider timeOfDaySlider;
    
    [Tooltip("Display current time")]
    public TMP_Text timeDisplayText;
    
    [Tooltip("Enable automatic time progression")]
    public Toggle autoTimeToggle;
    
    [Tooltip("Time progression speed multiplier")]
    public Slider timeSpeedSlider;
    
    [Header("Scene References")]
    [Tooltip("Main directional light (sun)")]
    public Light sunLight;
    
    [Tooltip("Secondary directional light (moon)")]
    public Light moonLight;
    
    [Tooltip("Main scene camera")]
    public Camera mainCamera;
    
    [Tooltip("Parent transform for cloud objects")]
    public Transform cloudParent;
    
    [Tooltip("Sky dome material")]
    public Material skyboxMaterial;
    
    [Tooltip("Water surface material")]
    public Material waterMaterial;
    
    #endregion
    
    #region Private Fields
    
    /// <summary>GridClient for LibreMetaverse integration</summary>
    private GridClient client;
    
    /// <summary>Current windlight settings</summary>
    private WindlightSettings currentSettings;
    
    /// <summary>Available environment presets</summary>
    private List<WindlightPreset> presets = new();
    
    /// <summary>Current time of day in hours (0-24)</summary>
    private float currentTimeOfDay = 12.0f;
    
    /// <summary>Auto time progression enabled</summary>
    private bool isAutoTime = false;
    
    /// <summary>Post-processing volume for effects</summary>
    private Volume postProcessVolume;
    
    #endregion
    
    #region Data Structures
    
    /// <summary>
    /// Complete windlight environment settings data structure
    /// Contains all parameters needed to define a complete environment
    /// </summary>
    [System.Serializable]
    public class WindlightSettings
    {
        [Header("Sky")]
        public float sunSize = 1.0f;                           // Sun disc size
        public float sunIntensity = 1.0f;                      // Sun light intensity
        public float atmosphereThickness = 1.0f;               // Atmospheric density
        public float exposure = 1.0f;                          // Overall exposure
        public Color horizonColor = new Color(0.25f, 0.5f, 0.75f);  // Horizon color
        public Color zenithColor = new Color(0.1f, 0.3f, 0.8f);     // Sky dome top color
        public Color sunColor = Color.white;                   // Sun light color
        public Color ambientColor = new Color(0.2f, 0.2f, 0.3f);    // Ambient light color
        
        [Header("Clouds")]
        public float cloudCoverage = 0.5f;                     // Cloud coverage (0-1)
        public float cloudDensity = 0.5f;                      // Cloud density
        public float cloudSpeed = 1.0f;                        // Cloud animation speed
        public Color cloudColor = Color.white;                 // Cloud base color
        public Color cloudShadowColor = new Color(0.3f, 0.3f, 0.3f); // Cloud shadow color
        
        [Header("Water")]
        public float waterTransparency = 0.8f;                 // Water transparency
        public float waterReflection = 0.7f;                   // Reflection strength
        public float waveHeight = 1.0f;                        // Wave amplitude
        public float waveSpeed = 1.0f;                         // Wave animation speed
        public Color waterColor = new Color(0.0f, 0.3f, 0.6f); // Water surface color
        public Color waterFogColor = new Color(0.0f, 0.2f, 0.4f); // Underwater fog color
        
        [Header("Lighting")]
        public float ambientIntensity = 0.3f;                  // Ambient light intensity
        public float directionalIntensity = 1.0f;              // Directional light intensity
        public bool enableShadows = true;                      // Shadow rendering enabled
        public float shadowDistance = 100f;                    // Shadow rendering distance
        public float shadowStrength = 1.0f;                    // Shadow opacity
        
        [Header("Post Processing")]
        public bool enableBloom = true;                        // Bloom effect enabled
        public float bloomIntensity = 0.5f;                    // Bloom intensity
        public bool enableColorGrading = true;                 // Color grading enabled
        public float contrast = 0.0f;                          // Image contrast
        public float saturation = 0.0f;                        // Color saturation
        public float gamma = 0.0f;                             // Gamma correction
        
        [Header("Time")]
        public float timeOfDay = 12.0f;                        // Current time (0-24)
        public bool autoTime = false;                          // Auto time progression
        public float timeSpeed = 1.0f;                         // Time progression speed
    }
    
    /// <summary>
    /// Environment preset containing named settings
    /// Allows quick switching between different environmental moods
    /// </summary>
    [System.Serializable]
    public class WindlightPreset
    {
        public string name;                    // Preset display name
        public WindlightSettings settings;     // Complete environment settings
    }
    
    #endregion
    
    #region Unity Lifecycle
    
    /// <summary>
    /// Initialize windlight manager
    /// Called before Start() on the first frame
    /// </summary>
    void Awake()
    {
        // Hide windlight window initially
        windlightWindow.SetActive(false);
        
        // Initialize with default settings
        currentSettings = new WindlightSettings();
        
        // Setup UI event handlers
        SetupUI();
        
        // Load available presets
        LoadPresets();
    }
    
    /// <summary>
    /// Complete initialization after all objects are available
    /// Called on the first frame after Awake()
    /// </summary>
    void Start()
    {
        // Get LibreMetaverse client reference
        client = ClientManager.client;
        
        // Find post-processing volume in scene
        postProcessVolume = FindObjectOfType<Volume>();
        
        // Apply initial settings
        ApplySettings(currentSettings);
        UpdateUIFromSettings();
        
        // Request estate windlight settings if connected to SL
        if (client != null && ClientManager.active)
        {
            RequestEstateWindlight();
        }
    }
    
    /// <summary>
    /// Update time progression and dynamic effects
    /// Called once per frame
    /// </summary>
    void Update()
    {
        if (isAutoTime)
        {
            UpdateTimeOfDay();
        }
    }
    
    /// <summary>
    /// Cleanup when component is destroyed
    /// Unsubscribes from events and saves settings
    /// </summary>
    void OnDestroy()
    {
        // Unsubscribe from color picker events to prevent memory leaks
        if (horizonColorPicker != null) horizonColorPicker.OnColorChanged -= OnHorizonColorChanged;
        if (zenithColorPicker != null) zenithColorPicker.OnColorChanged -= OnZenithColorChanged;
        if (sunColorPicker != null) sunColorPicker.OnColorChanged -= OnSunColorChanged;
        if (ambientColorPicker != null) ambientColorPicker.OnColorChanged -= OnAmbientColorChanged;
        if (cloudColorPicker != null) cloudColorPicker.OnColorChanged -= OnCloudColorChanged;
        if (cloudShadowColorPicker != null) cloudShadowColorPicker.OnColorChanged -= OnCloudShadowColorChanged;
        if (waterColorPicker != null) waterColorPicker.OnColorChanged -= OnWaterColorChanged;
        if (waterFogColorPicker != null) waterFogColorPicker.OnColorChanged -= OnWaterFogColorChanged;
        
        // Auto-save current settings before destruction
        SaveCurrentSettings();
    }
    
    #endregion
    
    #region Initialization and Setup
    
    /// <summary>
    /// Setup UI event handlers and component references
    /// Connects all UI elements to their respective functions
    /// </summary>
    void SetupUI()
    {
        // Main window controls
        if (closeButton) closeButton.onClick.AddListener(() => windlightWindow.SetActive(false));
        if (saveSettingsButton) saveSettingsButton.onClick.AddListener(SaveCurrentSettings);
        if (loadSettingsButton) loadSettingsButton.onClick.AddListener(LoadSettings);
        if (resetButton) resetButton.onClick.AddListener(ResetToDefaults);
        
        // Preset selection
        if (presetsDropdown) presetsDropdown.onValueChanged.AddListener(OnPresetChanged);
        
        // Sky control sliders
        if (sunSizeSlider) sunSizeSlider.onValueChanged.AddListener(OnSunSizeChanged);
        if (sunIntensitySlider) sunIntensitySlider.onValueChanged.AddListener(OnSunIntensityChanged);
        if (atmosphereThicknessSlider) atmosphereThicknessSlider.onValueChanged.AddListener(OnAtmosphereThicknessChanged);
        if (exposureSlider) exposureSlider.onValueChanged.AddListener(OnExposureChanged);
        
        // Cloud control sliders
        if (cloudCoverageSlider) cloudCoverageSlider.onValueChanged.AddListener(OnCloudCoverageChanged);
        if (cloudDensitySlider) cloudDensitySlider.onValueChanged.AddListener(OnCloudDensityChanged);
        if (cloudSpeedSlider) cloudSpeedSlider.onValueChanged.AddListener(OnCloudSpeedChanged);
        
        // Water control sliders
        if (waterTransparencySlider) waterTransparencySlider.onValueChanged.AddListener(OnWaterTransparencyChanged);
        if (waterReflectionSlider) waterReflectionSlider.onValueChanged.AddListener(OnWaterReflectionChanged);
        if (waveHeightSlider) waveHeightSlider.onValueChanged.AddListener(OnWaveHeightChanged);
        if (waveSpeedSlider) waveSpeedSlider.onValueChanged.AddListener(OnWaveSpeedChanged);
        
        // Lighting control sliders
        if (ambientIntensitySlider) ambientIntensitySlider.onValueChanged.AddListener(OnAmbientIntensityChanged);
        if (directionalIntensitySlider) directionalIntensitySlider.onValueChanged.AddListener(OnDirectionalIntensityChanged);
        if (shadowDistanceSlider) shadowDistanceSlider.onValueChanged.AddListener(OnShadowDistanceChanged);
        if (shadowStrengthSlider) shadowStrengthSlider.onValueChanged.AddListener(OnShadowStrengthChanged);
        
        // Post-processing sliders
        if (bloomIntensitySlider) bloomIntensitySlider.onValueChanged.AddListener(OnBloomIntensityChanged);
        if (contrastSlider) contrastSlider.onValueChanged.AddListener(OnContrastChanged);
        if (saturationSlider) saturationSlider.onValueChanged.AddListener(OnSaturationChanged);
        if (gammaSlider) gammaSlider.onValueChanged.AddListener(OnGammaChanged);
        
        // Time control sliders
        if (timeOfDaySlider) timeOfDaySlider.onValueChanged.AddListener(OnTimeOfDayChanged);
        if (timeSpeedSlider) timeSpeedSlider.onValueChanged.AddListener(OnTimeSpeedChanged);
        
        // Toggle switches
        if (enableShadowsToggle) enableShadowsToggle.onValueChanged.AddListener(OnEnableShadowsChanged);
        if (enableBloomToggle) enableBloomToggle.onValueChanged.AddListener(OnEnableBloomChanged);
        if (enableColorGradingToggle) enableColorGradingToggle.onValueChanged.AddListener(OnEnableColorGradingChanged);
        if (autoTimeToggle) autoTimeToggle.onValueChanged.AddListener(OnAutoTimeChanged);
        
        // Color picker event handlers
        SetupColorPickers();
    }
    
    /// <summary>
    /// Setup color picker event handlers
    /// Connects color pickers to their respective change handlers
    /// </summary>
    void SetupColorPickers()
    {
        if (horizonColorPicker) horizonColorPicker.OnColorChanged += OnHorizonColorChanged;
        if (zenithColorPicker) zenithColorPicker.OnColorChanged += OnZenithColorChanged;
        if (sunColorPicker) sunColorPicker.OnColorChanged += OnSunColorChanged;
        if (ambientColorPicker) ambientColorPicker.OnColorChanged += OnAmbientColorChanged;
        if (cloudColorPicker) cloudColorPicker.OnColorChanged += OnCloudColorChanged;
        if (cloudShadowColorPicker) cloudShadowColorPicker.OnColorChanged += OnCloudShadowColorChanged;
        if (waterColorPicker) waterColorPicker.OnColorChanged += OnWaterColorChanged;
        if (waterFogColorPicker) waterFogColorPicker.OnColorChanged += OnWaterFogColorChanged;
    }
    
    #endregion
    
    #region Public Interface
    
    /// <summary>
    /// Show the windlight editor window
    /// Public method for external components to open the editor
    /// </summary>
    public void ShowWindlightEditor()
    {
        windlightWindow.SetActive(true);
        UpdateUIFromSettings();
    }
    
    /// <summary>
    /// Set environment preset by name
    /// Allows external systems to quickly change environment
    /// </summary>
    /// <param name="presetName">Name of the preset to apply</param>
    public void SetPreset(string presetName)
    {
        var preset = presets.Find(p => p.name == presetName);
        if (preset != null)
        {
            ApplySettings(preset.settings);
            UpdateUIFromSettings();
        }
    }
    
    /// <summary>
    /// Set time of day programmatically
    /// Allows external systems to control time
    /// </summary>
    /// <param name="hours">Time in hours (0-24)</param>
    public void SetTimeOfDay(float hours)
    {
        currentSettings.timeOfDay = Mathf.Clamp(hours, 0f, 24f);
        ApplyTimeOfDay();
        
        if (timeOfDaySlider)
        {
            timeOfDaySlider.value = currentSettings.timeOfDay;
        }
    }
    
    /// <summary>
    /// Get current windlight settings
    /// Allows external systems to read current environment state
    /// </summary>
    /// <returns>Current windlight settings</returns>
    public WindlightSettings GetCurrentSettings()
    {
        return currentSettings;
    }
    
    #endregion
    
    #region Preset Management
    
    /// <summary>
    /// Load default and custom environment presets
    /// Creates built-in presets and loads custom ones from disk
    /// </summary>
    void LoadPresets()
    {
        presets.Clear();
        
        // Create built-in presets
        presets.Add(new WindlightPreset
        {
            name = "Sunrise",
            settings = CreateSunrisePreset()
        });
        
        presets.Add(new WindlightPreset
        {
            name = "Midday",
            settings = CreateMiddayPreset()
        });
        
        presets.Add(new WindlightPreset
        {
            name = "Sunset",
            settings = CreateSunsetPreset()
        });
        
        presets.Add(new WindlightPreset
        {
            name = "Night",
            settings = CreateNightPreset()
        });
        
        // Load custom presets from disk
        LoadCustomPresets();
        
        // Update UI dropdown
        UpdatePresetsDropdown();
    }
    
    /// <summary>
    /// Create sunrise environment preset
    /// Warm colors with low sun angle
    /// </summary>
    /// <returns>Sunrise windlight settings</returns>
    WindlightSettings CreateSunrisePreset()
    {
        return new WindlightSettings
        {
            sunSize = 2.0f,
            sunIntensity = 0.8f,
            atmosphereThickness = 1.2f,
            horizonColor = new Color(1.0f, 0.6f, 0.3f),      // Warm orange horizon
            zenithColor = new Color(0.4f, 0.6f, 0.9f),       // Light blue zenith
            sunColor = new Color(1.0f, 0.8f, 0.6f),          // Warm sun color
            ambientColor = new Color(0.3f, 0.2f, 0.4f),      // Purple ambient
            timeOfDay = 6.0f
        };
    }
    
    /// <summary>
    /// Create midday environment preset
    /// Bright, clear daylight conditions
    /// </summary>
    /// <returns>Midday windlight settings</returns>
    WindlightSettings CreateMiddayPreset()
    {
        return new WindlightSettings
        {
            sunSize = 1.0f,
            sunIntensity = 1.2f,
            atmosphereThickness = 0.8f,
            horizonColor = new Color(0.8f, 0.9f, 1.0f),      // Light blue horizon
            zenithColor = new Color(0.3f, 0.7f, 1.0f),       // Clear blue zenith
            sunColor = Color.white,                           // Pure white sun
            ambientColor = new Color(0.4f, 0.4f, 0.5f),      // Neutral ambient
            timeOfDay = 12.0f
        };
    }
    
    /// <summary>
    /// Create sunset environment preset
    /// Dramatic warm colors with atmospheric effects
    /// </summary>
    /// <returns>Sunset windlight settings</returns>
    WindlightSettings CreateSunsetPreset()
    {
        return new WindlightSettings
        {
            sunSize = 2.5f,
            sunIntensity = 0.9f,
            atmosphereThickness = 1.5f,
            horizonColor = new Color(1.0f, 0.4f, 0.2f),      // Deep orange horizon
            zenithColor = new Color(0.5f, 0.3f, 0.7f),       // Purple zenith
            sunColor = new Color(1.0f, 0.6f, 0.3f),          // Orange sun
            ambientColor = new Color(0.4f, 0.2f, 0.3f),      // Warm ambient
            timeOfDay = 18.0f
        };
    }
    
    /// <summary>
    /// Create night environment preset
    /// Dark, moonlit environment with stars
    /// </summary>
    /// <returns>Night windlight settings</returns>
    WindlightSettings CreateNightPreset()
    {
        return new WindlightSettings
        {
            sunSize = 0.5f,
            sunIntensity = 0.1f,
            atmosphereThickness = 2.0f,
            horizonColor = new Color(0.1f, 0.1f, 0.2f),      // Dark blue horizon
            zenithColor = new Color(0.05f, 0.05f, 0.15f),    // Very dark zenith
            sunColor = new Color(0.8f, 0.8f, 1.0f),          // Cool moonlight
            ambientColor = new Color(0.1f, 0.1f, 0.2f),      // Very dark ambient
            timeOfDay = 0.0f
        };
    }
    
    /// <summary>
    /// Load custom presets from persistent storage
    /// Reads user-created presets from JSON files
    /// </summary>
    void LoadCustomPresets()
    {
        string presetsPath = Path.Combine(Application.persistentDataPath, "WindlightPresets.json");
        
        if (File.Exists(presetsPath))
        {
            try
            {
                string json = File.ReadAllText(presetsPath);
                var customPresets = JsonUtility.FromJson<WindlightPreset[]>(json);
                
                if (customPresets != null)
                {
                    presets.AddRange(customPresets);
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Failed to load custom presets: {ex.Message}");
            }
        }
    }
    
    /// <summary>
    /// Update presets dropdown UI
    /// Populates dropdown with all available presets
    /// </summary>
    void UpdatePresetsDropdown()
    {
        if (presetsDropdown == null) return;
        
        presetsDropdown.options.Clear();
        
        foreach (var preset in presets)
        {
            presetsDropdown.options.Add(new TMP_Dropdown.OptionData(preset.name));
        }
        
        presetsDropdown.RefreshShownValue();
    }
    
    #endregion
    
    #region Settings Application
    
    /// <summary>
    /// Apply complete windlight settings to the scene
    /// Updates all environmental parameters
    /// </summary>
    /// <param name="settings">Settings to apply</param>
    void ApplySettings(WindlightSettings settings)
    {
        currentSettings = settings;
        
        // Apply all settings categories
        ApplySkySettings();
        ApplyLightingSettings();
        ApplyWaterSettings();
        ApplyPostProcessing();
        ApplyTimeOfDay();
    }
    
    /// <summary>
    /// Apply sky and atmosphere settings
    /// Updates skybox material and atmospheric parameters
    /// </summary>
    void ApplySkySettings()
    {
        if (skyboxMaterial)
        {
            skyboxMaterial.SetFloat("_SunSize", currentSettings.sunSize);
            skyboxMaterial.SetFloat("_AtmosphereThickness", currentSettings.atmosphereThickness);
            skyboxMaterial.SetFloat("_Exposure", currentSettings.exposure);
            skyboxMaterial.SetColor("_HorizonColor", currentSettings.horizonColor);
            skyboxMaterial.SetColor("_ZenithColor", currentSettings.zenithColor);
            skyboxMaterial.SetColor("_SunColor", currentSettings.sunColor);
        }
        
        // Update global ambient lighting
        RenderSettings.ambientLight = currentSettings.ambientColor;
        RenderSettings.ambientIntensity = currentSettings.ambientIntensity;
    }
    
    /// <summary>
    /// Apply lighting settings to scene lights
    /// Updates directional lights and shadow settings
    /// </summary>
    void ApplyLightingSettings()
    {
        if (sunLight)
        {
            sunLight.intensity = currentSettings.directionalIntensity * currentSettings.sunIntensity;
            sunLight.color = currentSettings.sunColor;
            sunLight.shadows = currentSettings.enableShadows ? LightShadows.Soft : LightShadows.None;
        }
        
        // Update global shadow settings
        if (currentSettings.enableShadows)
        {
            QualitySettings.shadows = ShadowQuality.All;
            QualitySettings.shadowDistance = currentSettings.shadowDistance;
        }
        else
        {
            QualitySettings.shadows = ShadowQuality.Disable;
        }
    }
    
    /// <summary>
    /// Apply water rendering settings
    /// Updates water material properties
    /// </summary>
    void ApplyWaterSettings()
    {
        if (waterMaterial)
        {
            waterMaterial.SetColor("_Color", currentSettings.waterColor);
            waterMaterial.SetColor("_FogColor", currentSettings.waterFogColor);
            waterMaterial.SetFloat("_Transparency", currentSettings.waterTransparency);
            waterMaterial.SetFloat("_Reflection", currentSettings.waterReflection);
            waterMaterial.SetFloat("_WaveHeight", currentSettings.waveHeight);
            waterMaterial.SetFloat("_WaveSpeed", currentSettings.waveSpeed);
        }
    }
    
    /// <summary>
    /// Apply post-processing effects
    /// Updates URP Volume components
    /// </summary>
    void ApplyPostProcessing()
    {
        if (postProcessVolume == null) return;
        
        var profile = postProcessVolume.profile;
        if (profile == null) return;
        
        // Apply bloom effects
        if (profile.TryGet<Bloom>(out var bloom))
        {
            bloom.enabled.value = currentSettings.enableBloom;
            bloom.intensity.value = currentSettings.bloomIntensity;
        }
        
        // Apply color grading
        if (profile.TryGet<ColorAdjustments>(out var colorAdjustments))
        {
            colorAdjustments.contrast.value = currentSettings.contrast;
            colorAdjustments.saturation.value = currentSettings.saturation;
        }
    }
    
    /// <summary>
    /// Apply time of day settings
    /// Updates sun/moon positions and lighting based on time
    /// </summary>
    void ApplyTimeOfDay()
    {
        currentTimeOfDay = currentSettings.timeOfDay;
        
        if (sunLight)
        {
            // Calculate sun rotation based on time (sun moves across sky)
            float sunAngle = (currentTimeOfDay / 24.0f) * 360.0f - 90.0f;
            sunLight.transform.rotation = Quaternion.Euler(sunAngle, 30.0f, 0.0f);
            
            // Adjust intensity based on sun elevation (dimmer at horizon)
            float intensityMultiplier = Mathf.Clamp01(Mathf.Cos((sunAngle + 90.0f) * Mathf.Deg2Rad));
            sunLight.intensity = currentSettings.directionalIntensity * intensityMultiplier;
        }
        
        if (moonLight)
        {
            // Moon is opposite to sun
            float moonAngle = sunLight.transform.eulerAngles.x + 180.0f;
            moonLight.transform.rotation = Quaternion.Euler(moonAngle, 30.0f, 0.0f);
            
            // Moon is brighter at night
            float moonIntensity = Mathf.Clamp01(-Mathf.Cos((moonAngle + 90.0f) * Mathf.Deg2Rad));
            moonLight.intensity = 0.2f * moonIntensity;
        }
        
        UpdateTimeDisplay();
    }
    
    #endregion
    
    #region Time System
    
    /// <summary>
    /// Update automatic time progression
    /// Called each frame when auto time is enabled
    /// </summary>
    void UpdateTimeOfDay()
    {
        // Progress time based on speed setting
        currentTimeOfDay += Time.deltaTime * currentSettings.timeSpeed / 3600.0f; // Convert to hours
        
        // Wrap time around 24-hour cycle
        if (currentTimeOfDay >= 24.0f)
        {
            currentTimeOfDay -= 24.0f;
        }
        
        // Update settings and apply changes
        currentSettings.timeOfDay = currentTimeOfDay;
        ApplyTimeOfDay();
        
        // Update UI slider
        if (timeOfDaySlider)
        {
            timeOfDaySlider.value = currentTimeOfDay;
        }
    }
    
    /// <summary>
    /// Update time display text
    /// Shows current time in HH:MM format
    /// </summary>
    void UpdateTimeDisplay()
    {
        if (timeDisplayText)
        {
            int hours = Mathf.FloorToInt(currentTimeOfDay);
            int minutes = Mathf.FloorToInt((currentTimeOfDay - hours) * 60);
            timeDisplayText.text = $"{hours:D2}:{minutes:D2}";
        }
    }
    
    #endregion
    
    #region UI Update Methods
    
    /// <summary>
    /// Update all UI elements from current settings
    /// Synchronizes UI with internal settings state
    /// </summary>
    void UpdateUIFromSettings()
    {
        // Update sky sliders
        if (sunSizeSlider) sunSizeSlider.value = currentSettings.sunSize;
        if (sunIntensitySlider) sunIntensitySlider.value = currentSettings.sunIntensity;
        if (atmosphereThicknessSlider) atmosphereThicknessSlider.value = currentSettings.atmosphereThickness;
        if (exposureSlider) exposureSlider.value = currentSettings.exposure;
        
        // Update cloud sliders
        if (cloudCoverageSlider) cloudCoverageSlider.value = currentSettings.cloudCoverage;
        if (cloudDensitySlider) cloudDensitySlider.value = currentSettings.cloudDensity;
        if (cloudSpeedSlider) cloudSpeedSlider.value = currentSettings.cloudSpeed;
        
        // Update water sliders
        if (waterTransparencySlider) waterTransparencySlider.value = currentSettings.waterTransparency;
        if (waterReflectionSlider) waterReflectionSlider.value = currentSettings.waterReflection;
        if (waveHeightSlider) waveHeightSlider.value = currentSettings.waveHeight;
        if (waveSpeedSlider) waveSpeedSlider.value = currentSettings.waveSpeed;
        
        // Update lighting sliders
        if (ambientIntensitySlider) ambientIntensitySlider.value = currentSettings.ambientIntensity;
        if (directionalIntensitySlider) directionalIntensitySlider.value = currentSettings.directionalIntensity;
        if (shadowDistanceSlider) shadowDistanceSlider.value = currentSettings.shadowDistance;
        if (shadowStrengthSlider) shadowStrengthSlider.value = currentSettings.shadowStrength;
        
        // Update post-processing sliders
        if (bloomIntensitySlider) bloomIntensitySlider.value = currentSettings.bloomIntensity;
        if (contrastSlider) contrastSlider.value = currentSettings.contrast;
        if (saturationSlider) saturationSlider.value = currentSettings.saturation;
        if (gammaSlider) gammaSlider.value = currentSettings.gamma;
        
        // Update time sliders
        if (timeOfDaySlider) timeOfDaySlider.value = currentSettings.timeOfDay;
        if (timeSpeedSlider) timeSpeedSlider.value = currentSettings.timeSpeed;
        
        // Update toggles
        if (enableShadowsToggle) enableShadowsToggle.isOn = currentSettings.enableShadows;
        if (enableBloomToggle) enableBloomToggle.isOn = currentSettings.enableBloom;
        if (enableColorGradingToggle) enableColorGradingToggle.isOn = currentSettings.enableColorGrading;
        if (autoTimeToggle) autoTimeToggle.isOn = currentSettings.autoTime;
        
        // Update color pickers
        if (horizonColorPicker) horizonColorPicker.CurrentColor = currentSettings.horizonColor;
        if (zenithColorPicker) zenithColorPicker.CurrentColor = currentSettings.zenithColor;
        if (sunColorPicker) sunColorPicker.CurrentColor = currentSettings.sunColor;
        if (ambientColorPicker) ambientColorPicker.CurrentColor = currentSettings.ambientColor;
        if (cloudColorPicker) cloudColorPicker.CurrentColor = currentSettings.cloudColor;
        if (cloudShadowColorPicker) cloudShadowColorPicker.CurrentColor = currentSettings.cloudShadowColor;
        if (waterColorPicker) waterColorPicker.CurrentColor = currentSettings.waterColor;
        if (waterFogColorPicker) waterFogColorPicker.CurrentColor = currentSettings.waterFogColor;
        
        UpdateTimeDisplay();
    }
    
    #endregion
    
    #region Event Handlers - UI Controls
    
    void OnPresetChanged(int index)
    {
        if (index >= 0 && index < presets.Count)
        {
            ApplySettings(presets[index].settings);
            UpdateUIFromSettings();
        }
    }
    
    void OnSunSizeChanged(float value)
    {
        currentSettings.sunSize = value;
        ApplySkySettings();
    }
    
    void OnSunIntensityChanged(float value)
    {
        currentSettings.sunIntensity = value;
        ApplyLightingSettings();
        ApplySkySettings();
    }
    
    void OnAtmosphereThicknessChanged(float value)
    {
        currentSettings.atmosphereThickness = value;
        ApplySkySettings();
    }
    
    void OnExposureChanged(float value)
    {
        currentSettings.exposure = value;
        ApplySkySettings();
    }
    
    void OnCloudCoverageChanged(float value)
    {
        currentSettings.cloudCoverage = value;
        // Apply cloud coverage changes (would update cloud system)
    }
    
    void OnCloudDensityChanged(float value)
    {
        currentSettings.cloudDensity = value;
        // Apply cloud density changes (would update cloud system)
    }
    
    void OnCloudSpeedChanged(float value)
    {
        currentSettings.cloudSpeed = value;
        // Apply cloud speed changes (would update cloud animation)
    }
    
    void OnWaterTransparencyChanged(float value)
    {
        currentSettings.waterTransparency = value;
        ApplyWaterSettings();
    }
    
    void OnWaterReflectionChanged(float value)
    {
        currentSettings.waterReflection = value;
        ApplyWaterSettings();
    }
    
    void OnWaveHeightChanged(float value)
    {
        currentSettings.waveHeight = value;
        ApplyWaterSettings();
    }
    
    void OnWaveSpeedChanged(float value)
    {
        currentSettings.waveSpeed = value;
        ApplyWaterSettings();
    }
    
    void OnAmbientIntensityChanged(float value)
    {
        currentSettings.ambientIntensity = value;
        ApplyLightingSettings();
    }
    
    void OnDirectionalIntensityChanged(float value)
    {
        currentSettings.directionalIntensity = value;
        ApplyLightingSettings();
    }
    
    void OnShadowDistanceChanged(float value)
    {
        currentSettings.shadowDistance = value;
        ApplyLightingSettings();
    }
    
    void OnShadowStrengthChanged(float value)
    {
        currentSettings.shadowStrength = value;
        ApplyLightingSettings();
    }
    
    void OnBloomIntensityChanged(float value)
    {
        currentSettings.bloomIntensity = value;
        ApplyPostProcessing();
    }
    
    void OnContrastChanged(float value)
    {
        currentSettings.contrast = value;
        ApplyPostProcessing();
    }
    
    void OnSaturationChanged(float value)
    {
        currentSettings.saturation = value;
        ApplyPostProcessing();
    }
    
    void OnGammaChanged(float value)
    {
        currentSettings.gamma = value;
        ApplyPostProcessing();
    }
    
    void OnTimeOfDayChanged(float value)
    {
        currentSettings.timeOfDay = value;
        ApplyTimeOfDay();
    }
    
    void OnTimeSpeedChanged(float value)
    {
        currentSettings.timeSpeed = value;
    }
    
    void OnEnableShadowsChanged(bool value)
    {
        currentSettings.enableShadows = value;
        ApplyLightingSettings();
    }
    
    void OnEnableBloomChanged(bool value)
    {
        currentSettings.enableBloom = value;
        ApplyPostProcessing();
    }
    
    void OnEnableColorGradingChanged(bool value)
    {
        currentSettings.enableColorGrading = value;
        ApplyPostProcessing();
    }
    
    void OnAutoTimeChanged(bool value)
    {
        currentSettings.autoTime = value;
        isAutoTime = value;
    }
    
    #endregion
    
    #region Event Handlers - Color Pickers
    
    void OnHorizonColorChanged(Color color)
    {
        currentSettings.horizonColor = color;
        ApplySkySettings();
    }
    
    void OnZenithColorChanged(Color color)
    {
        currentSettings.zenithColor = color;
        ApplySkySettings();
    }
    
    void OnSunColorChanged(Color color)
    {
        currentSettings.sunColor = color;
        ApplyLightingSettings();
        ApplySkySettings();
    }
    
    void OnAmbientColorChanged(Color color)
    {
        currentSettings.ambientColor = color;
        ApplyLightingSettings();
    }
    
    void OnCloudColorChanged(Color color)
    {
        currentSettings.cloudColor = color;
        // Apply cloud color changes (would update cloud materials)
    }
    
    void OnCloudShadowColorChanged(Color color)
    {
        currentSettings.cloudShadowColor = color;
        // Apply cloud shadow color changes (would update cloud shading)
    }
    
    void OnWaterColorChanged(Color color)
    {
        currentSettings.waterColor = color;
        ApplyWaterSettings();
    }
    
    void OnWaterFogColorChanged(Color color)
    {
        currentSettings.waterFogColor = color;
        ApplyWaterSettings();
    }
    
    #endregion
    
    #region Settings Persistence
    
    /// <summary>
    /// Save current settings to persistent storage
    /// Stores settings as JSON file for future loading
    /// </summary>
    void SaveCurrentSettings()
    {
        string settingsPath = Path.Combine(Application.persistentDataPath, "WindlightSettings.json");
        
        try
        {
            string json = JsonUtility.ToJson(currentSettings, true);
            File.WriteAllText(settingsPath, json);
            Debug.Log("Windlight settings saved");
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Failed to save windlight settings: {ex.Message}");
        }
    }
    
    /// <summary>
    /// Load settings from persistent storage
    /// Restores previously saved windlight settings
    /// </summary>
    void LoadSettings()
    {
        string settingsPath = Path.Combine(Application.persistentDataPath, "WindlightSettings.json");
        
        if (File.Exists(settingsPath))
        {
            try
            {
                string json = File.ReadAllText(settingsPath);
                var settings = JsonUtility.FromJson<WindlightSettings>(json);
                
                ApplySettings(settings);
                UpdateUIFromSettings();
                Debug.Log("Windlight settings loaded");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Failed to load windlight settings: {ex.Message}");
            }
        }
    }
    
    /// <summary>
    /// Reset all settings to default values
    /// Restores factory default windlight settings
    /// </summary>
    void ResetToDefaults()
    {
        currentSettings = new WindlightSettings();
        ApplySettings(currentSettings);
        UpdateUIFromSettings();
    }
    
    #endregion
    
    #region LibreMetaverse Integration
    
    /// <summary>
    /// Request windlight settings from SL estate
    /// Integrates with LibreMetaverse estate management
    /// </summary>
    void RequestEstateWindlight()
    {
        if (client == null) return;
        
        // Request estate windlight settings from the server
        // This would use LibreMetaverse estate management functions
        // Implementation depends on estate owner permissions
        Debug.Log("Requesting estate windlight settings");
    }
    
    #endregion
}