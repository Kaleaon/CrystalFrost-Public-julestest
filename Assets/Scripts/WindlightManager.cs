using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;
using TMPro;
using OpenMetaverse;
using System.Collections.Generic;
using System.IO;

public class WindlightManager : MonoBehaviour
{
    [Header("Windlight Window")]
    public GameObject windlightWindow;
    public Button closeButton;
    public Button saveSettingsButton;
    public Button loadSettingsButton;
    public Button resetButton;
    public TMP_Dropdown presetsDropdown;
    
    [Header("Sky Settings")]
    public Slider sunSizeSlider;
    public Slider sunIntensitySlider;
    public Slider atmosphereThicknessSlider;
    public Slider exposureSlider;
    public ColorPicker horizonColorPicker;
    public ColorPicker zenithColorPicker;
    public ColorPicker sunColorPicker;
    public ColorPicker ambientColorPicker;
    
    [Header("Cloud Settings")]
    public Slider cloudCoverageSlider;
    public Slider cloudDensitySlider;
    public Slider cloudSpeedSlider;
    public ColorPicker cloudColorPicker;
    public ColorPicker cloudShadowColorPicker;
    
    [Header("Water Settings")]
    public Slider waterTransparencySlider;
    public Slider waterReflectionSlider;
    public Slider waveHeightSlider;
    public Slider waveSpeedSlider;
    public ColorPicker waterColorPicker;
    public ColorPicker waterFogColorPicker;
    
    [Header("Lighting")]
    public Slider ambientIntensitySlider;
    public Slider directionalIntensitySlider;
    public Toggle enableShadowsToggle;
    public Slider shadowDistanceSlider;
    public Slider shadowStrengthSlider;
    
    [Header("Post Processing")]
    public Toggle enableBloomToggle;
    public Slider bloomIntensitySlider;
    public Toggle enableColorGradingToggle;
    public Slider contrastSlider;
    public Slider saturationSlider;
    public Slider gammaSlider;
    
    [Header("Time of Day")]
    public Slider timeOfDaySlider;
    public TMP_Text timeDisplayText;
    public Toggle autoTimeToggle;
    public Slider timeSpeedSlider;
    
    [Header("Scene References")]
    public Light sunLight;
    public Light moonLight;
    public Camera mainCamera;
    public Transform cloudParent;
    public Material skyboxMaterial;
    public Material waterMaterial;
    
    private GridClient client;
    private WindlightSettings currentSettings;
    private List<WindlightPreset> presets = new();
    private float currentTimeOfDay = 12.0f; // 12 PM
    private bool isAutoTime = false;
    private Volume postProcessVolume;
    
    [System.Serializable]
    public class WindlightSettings
    {
        [Header("Sky")]
        public float sunSize = 1.0f;
        public float sunIntensity = 1.0f;
        public float atmosphereThickness = 1.0f;
        public float exposure = 1.0f;
        public Color horizonColor = new Color(0.25f, 0.5f, 0.75f);
        public Color zenithColor = new Color(0.1f, 0.3f, 0.8f);
        public Color sunColor = Color.white;
        public Color ambientColor = new Color(0.2f, 0.2f, 0.3f);
        
        [Header("Clouds")]
        public float cloudCoverage = 0.5f;
        public float cloudDensity = 0.5f;
        public float cloudSpeed = 1.0f;
        public Color cloudColor = Color.white;
        public Color cloudShadowColor = new Color(0.3f, 0.3f, 0.3f);
        
        [Header("Water")]
        public float waterTransparency = 0.8f;
        public float waterReflection = 0.7f;
        public float waveHeight = 1.0f;
        public float waveSpeed = 1.0f;
        public Color waterColor = new Color(0.0f, 0.3f, 0.6f);
        public Color waterFogColor = new Color(0.0f, 0.2f, 0.4f);
        
        [Header("Lighting")]
        public float ambientIntensity = 0.3f;
        public float directionalIntensity = 1.0f;
        public bool enableShadows = true;
        public float shadowDistance = 100f;
        public float shadowStrength = 1.0f;
        
        [Header("Post Processing")]
        public bool enableBloom = true;
        public float bloomIntensity = 0.5f;
        public bool enableColorGrading = true;
        public float contrast = 0.0f;
        public float saturation = 0.0f;
        public float gamma = 0.0f;
        
        [Header("Time")]
        public float timeOfDay = 12.0f;
        public bool autoTime = false;
        public float timeSpeed = 1.0f;
    }
    
    [System.Serializable]
    public class WindlightPreset
    {
        public string name;
        public WindlightSettings settings;
    }

    void Awake()
    {
        windlightWindow.SetActive(false);
        currentSettings = new WindlightSettings();
        SetupUI();
        LoadPresets();
    }

    void SetupUI()
    {
        if (closeButton) closeButton.onClick.AddListener(() => windlightWindow.SetActive(false));
        if (saveSettingsButton) saveSettingsButton.onClick.AddListener(SaveCurrentSettings);
        if (loadSettingsButton) loadSettingsButton.onClick.AddListener(LoadSettings);
        if (resetButton) resetButton.onClick.AddListener(ResetToDefaults);
        
        if (presetsDropdown) presetsDropdown.onValueChanged.AddListener(OnPresetChanged);
        
        // Sky sliders
        if (sunSizeSlider) sunSizeSlider.onValueChanged.AddListener(OnSunSizeChanged);
        if (sunIntensitySlider) sunIntensitySlider.onValueChanged.AddListener(OnSunIntensityChanged);
        if (atmosphereThicknessSlider) atmosphereThicknessSlider.onValueChanged.AddListener(OnAtmosphereThicknessChanged);
        if (exposureSlider) exposureSlider.onValueChanged.AddListener(OnExposureChanged);
        
        // Cloud sliders
        if (cloudCoverageSlider) cloudCoverageSlider.onValueChanged.AddListener(OnCloudCoverageChanged);
        if (cloudDensitySlider) cloudDensitySlider.onValueChanged.AddListener(OnCloudDensityChanged);
        if (cloudSpeedSlider) cloudSpeedSlider.onValueChanged.AddListener(OnCloudSpeedChanged);
        
        // Water sliders
        if (waterTransparencySlider) waterTransparencySlider.onValueChanged.AddListener(OnWaterTransparencyChanged);
        if (waterReflectionSlider) waterReflectionSlider.onValueChanged.AddListener(OnWaterReflectionChanged);
        if (waveHeightSlider) waveHeightSlider.onValueChanged.AddListener(OnWaveHeightChanged);
        if (waveSpeedSlider) waveSpeedSlider.onValueChanged.AddListener(OnWaveSpeedChanged);
        
        // Lighting sliders
        if (ambientIntensitySlider) ambientIntensitySlider.onValueChanged.AddListener(OnAmbientIntensityChanged);
        if (directionalIntensitySlider) directionalIntensitySlider.onValueChanged.AddListener(OnDirectionalIntensityChanged);
        if (shadowDistanceSlider) shadowDistanceSlider.onValueChanged.AddListener(OnShadowDistanceChanged);
        if (shadowStrengthSlider) shadowStrengthSlider.onValueChanged.AddListener(OnShadowStrengthChanged);
        
        // Post processing
        if (bloomIntensitySlider) bloomIntensitySlider.onValueChanged.AddListener(OnBloomIntensityChanged);
        if (contrastSlider) contrastSlider.onValueChanged.AddListener(OnContrastChanged);
        if (saturationSlider) saturationSlider.onValueChanged.AddListener(OnSaturationChanged);
        if (gammaSlider) gammaSlider.onValueChanged.AddListener(OnGammaChanged);
        
        // Time controls
        if (timeOfDaySlider) timeOfDaySlider.onValueChanged.AddListener(OnTimeOfDayChanged);
        if (timeSpeedSlider) timeSpeedSlider.onValueChanged.AddListener(OnTimeSpeedChanged);
        
        // Toggles
        if (enableShadowsToggle) enableShadowsToggle.onValueChanged.AddListener(OnEnableShadowsChanged);
        if (enableBloomToggle) enableBloomToggle.onValueChanged.AddListener(OnEnableBloomChanged);
        if (enableColorGradingToggle) enableColorGradingToggle.onValueChanged.AddListener(OnEnableColorGradingChanged);
        if (autoTimeToggle) autoTimeToggle.onValueChanged.AddListener(OnAutoTimeChanged);
        
        // Color pickers
        SetupColorPickers();
    }

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

    void Start()
    {
        client = ClientManager.client;
        
        // Find post process volume
        postProcessVolume = FindObjectOfType<Volume>();
        
        // Initialize with default settings
        ApplySettings(currentSettings);
        UpdateUIFromSettings();
        
        // Request estate settings if connected
        if (client != null && ClientManager.active)
        {
            RequestEstateWindlight();
        }
    }

    void Update()
    {
        if (isAutoTime)
        {
            UpdateTimeOfDay();
        }
    }

    public void ShowWindlightEditor()
    {
        windlightWindow.SetActive(true);
        UpdateUIFromSettings();
    }

    void LoadPresets()
    {
        // Add default presets
        presets.Clear();
        
        // Sunrise preset
        presets.Add(new WindlightPreset
        {
            name = "Sunrise",
            settings = CreateSunrisePreset()
        });
        
        // Midday preset
        presets.Add(new WindlightPreset
        {
            name = "Midday",
            settings = CreateMiddayPreset()
        });
        
        // Sunset preset
        presets.Add(new WindlightPreset
        {
            name = "Sunset",
            settings = CreateSunsetPreset()
        });
        
        // Night preset
        presets.Add(new WindlightPreset
        {
            name = "Night",
            settings = CreateNightPreset()
        });
        
        // Load custom presets from file
        LoadCustomPresets();
        
        // Update dropdown
        UpdatePresetsDropdown();
    }

    WindlightSettings CreateSunrisePreset()
    {
        return new WindlightSettings
        {
            sunSize = 2.0f,
            sunIntensity = 0.8f,
            atmosphereThickness = 1.2f,
            horizonColor = new Color(1.0f, 0.6f, 0.3f),
            zenithColor = new Color(0.4f, 0.6f, 0.9f),
            sunColor = new Color(1.0f, 0.8f, 0.6f),
            ambientColor = new Color(0.3f, 0.2f, 0.4f),
            timeOfDay = 6.0f
        };
    }

    WindlightSettings CreateMiddayPreset()
    {
        return new WindlightSettings
        {
            sunSize = 1.0f,
            sunIntensity = 1.2f,
            atmosphereThickness = 0.8f,
            horizonColor = new Color(0.8f, 0.9f, 1.0f),
            zenithColor = new Color(0.3f, 0.7f, 1.0f),
            sunColor = Color.white,
            ambientColor = new Color(0.4f, 0.4f, 0.5f),
            timeOfDay = 12.0f
        };
    }

    WindlightSettings CreateSunsetPreset()
    {
        return new WindlightSettings
        {
            sunSize = 2.5f,
            sunIntensity = 0.9f,
            atmosphereThickness = 1.5f,
            horizonColor = new Color(1.0f, 0.4f, 0.2f),
            zenithColor = new Color(0.5f, 0.3f, 0.7f),
            sunColor = new Color(1.0f, 0.6f, 0.3f),
            ambientColor = new Color(0.4f, 0.2f, 0.3f),
            timeOfDay = 18.0f
        };
    }

    WindlightSettings CreateNightPreset()
    {
        return new WindlightSettings
        {
            sunSize = 0.5f,
            sunIntensity = 0.1f,
            atmosphereThickness = 2.0f,
            horizonColor = new Color(0.1f, 0.1f, 0.2f),
            zenithColor = new Color(0.05f, 0.05f, 0.15f),
            sunColor = new Color(0.8f, 0.8f, 1.0f),
            ambientColor = new Color(0.1f, 0.1f, 0.2f),
            timeOfDay = 0.0f
        };
    }

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

    void ApplySettings(WindlightSettings settings)
    {
        currentSettings = settings;
        
        // Apply sky settings
        ApplySkySettings();
        
        // Apply lighting settings
        ApplyLightingSettings();
        
        // Apply water settings
        ApplyWaterSettings();
        
        // Apply post processing
        ApplyPostProcessing();
        
        // Apply time of day
        ApplyTimeOfDay();
    }

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
        
        // Update ambient lighting
        RenderSettings.ambientLight = currentSettings.ambientColor;
        RenderSettings.ambientIntensity = currentSettings.ambientIntensity;
    }

    void ApplyLightingSettings()
    {
        if (sunLight)
        {
            sunLight.intensity = currentSettings.directionalIntensity * currentSettings.sunIntensity;
            sunLight.color = currentSettings.sunColor;
            sunLight.shadows = currentSettings.enableShadows ? LightShadows.Soft : LightShadows.None;
        }
        
        // Update shadow settings
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

    void ApplyPostProcessing()
    {
        if (postProcessVolume == null) return;
        
        var profile = postProcessVolume.profile;
        if (profile == null) return;
        
        // Apply bloom
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

    void ApplyTimeOfDay()
    {
        currentTimeOfDay = currentSettings.timeOfDay;
        
        if (sunLight)
        {
            // Calculate sun rotation based on time
            float sunAngle = (currentTimeOfDay / 24.0f) * 360.0f - 90.0f;
            sunLight.transform.rotation = Quaternion.Euler(sunAngle, 30.0f, 0.0f);
            
            // Adjust intensity based on time
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

    void UpdateTimeOfDay()
    {
        currentTimeOfDay += Time.deltaTime * currentSettings.timeSpeed / 3600.0f; // Convert to hours
        
        if (currentTimeOfDay >= 24.0f)
        {
            currentTimeOfDay -= 24.0f;
        }
        
        currentSettings.timeOfDay = currentTimeOfDay;
        ApplyTimeOfDay();
        
        if (timeOfDaySlider)
        {
            timeOfDaySlider.value = currentTimeOfDay;
        }
    }

    void UpdateTimeDisplay()
    {
        if (timeDisplayText)
        {
            int hours = Mathf.FloorToInt(currentTimeOfDay);
            int minutes = Mathf.FloorToInt((currentTimeOfDay - hours) * 60);
            timeDisplayText.text = $"{hours:D2}:{minutes:D2}";
        }
    }

    void UpdateUIFromSettings()
    {
        // Update sliders
        if (sunSizeSlider) sunSizeSlider.value = currentSettings.sunSize;
        if (sunIntensitySlider) sunIntensitySlider.value = currentSettings.sunIntensity;
        if (atmosphereThicknessSlider) atmosphereThicknessSlider.value = currentSettings.atmosphereThickness;
        if (exposureSlider) exposureSlider.value = currentSettings.exposure;
        
        if (cloudCoverageSlider) cloudCoverageSlider.value = currentSettings.cloudCoverage;
        if (cloudDensitySlider) cloudDensitySlider.value = currentSettings.cloudDensity;
        if (cloudSpeedSlider) cloudSpeedSlider.value = currentSettings.cloudSpeed;
        
        if (waterTransparencySlider) waterTransparencySlider.value = currentSettings.waterTransparency;
        if (waterReflectionSlider) waterReflectionSlider.value = currentSettings.waterReflection;
        if (waveHeightSlider) waveHeightSlider.value = currentSettings.waveHeight;
        if (waveSpeedSlider) waveSpeedSlider.value = currentSettings.waveSpeed;
        
        if (ambientIntensitySlider) ambientIntensitySlider.value = currentSettings.ambientIntensity;
        if (directionalIntensitySlider) directionalIntensitySlider.value = currentSettings.directionalIntensity;
        if (shadowDistanceSlider) shadowDistanceSlider.value = currentSettings.shadowDistance;
        if (shadowStrengthSlider) shadowStrengthSlider.value = currentSettings.shadowStrength;
        
        if (bloomIntensitySlider) bloomIntensitySlider.value = currentSettings.bloomIntensity;
        if (contrastSlider) contrastSlider.value = currentSettings.contrast;
        if (saturationSlider) saturationSlider.value = currentSettings.saturation;
        if (gammaSlider) gammaSlider.value = currentSettings.gamma;
        
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

    #region Event Handlers

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
        // Apply cloud coverage changes
    }

    void OnCloudDensityChanged(float value)
    {
        currentSettings.cloudDensity = value;
        // Apply cloud density changes
    }

    void OnCloudSpeedChanged(float value)
    {
        currentSettings.cloudSpeed = value;
        // Apply cloud speed changes
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

    // Color picker event handlers
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
        // Apply cloud color changes
    }

    void OnCloudShadowColorChanged(Color color)
    {
        currentSettings.cloudShadowColor = color;
        // Apply cloud shadow color changes
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

    void ResetToDefaults()
    {
        currentSettings = new WindlightSettings();
        ApplySettings(currentSettings);
        UpdateUIFromSettings();
    }

    void RequestEstateWindlight()
    {
        if (client == null) return;
        
        // Request estate windlight settings from the server
        // This would use LibreMetaverse estate management functions
        Debug.Log("Requesting estate windlight settings");
    }

    // Public methods for external control
    public void SetPreset(string presetName)
    {
        var preset = presets.Find(p => p.name == presetName);
        if (preset != null)
        {
            ApplySettings(preset.settings);
            UpdateUIFromSettings();
        }
    }

    public void SetTimeOfDay(float hours)
    {
        currentSettings.timeOfDay = Mathf.Clamp(hours, 0f, 24f);
        ApplyTimeOfDay();
        
        if (timeOfDaySlider)
        {
            timeOfDaySlider.value = currentSettings.timeOfDay;
        }
    }

    public WindlightSettings GetCurrentSettings()
    {
        return currentSettings;
    }
}