using UnityEngine;
using UnityEngine.UI;
using TMPro;
using OpenMetaverse;
using System.Collections.Generic;

public class PreferencesWindow : MonoBehaviour
{
    [Header("Window Management")]
    public GameObject preferencesWindow;
    public Button closeButton;
    
    [Header("Tab System")]
    public Button[] tabButtons;
    public GameObject[] tabPanels;
    public Color activeTabColor = Color.white;
    public Color inactiveTabColor = Color.gray;
    
    [Header("General Tab")]
    public Toggle showNamesOverAvatars;
    public Toggle playTypingSounds;
    public Toggle playUISounds;
    public Slider mouseSensitivity;
    public TMP_Text mouseSensitivityValue;
    
    [Header("Graphics Tab")]
    public Slider viewDistance;
    public TMP_Text viewDistanceValue;
    public Toggle enableShadows;
    public Toggle enableReflections;
    public Toggle enableParticles;
    public TMP_Dropdown qualityDropdown;
    public TMP_Dropdown textureDetailDropdown;
    public Toggle enableWindlight;
    public Slider maxAvatars;
    public TMP_Text maxAvatarsValue;
    
    [Header("Audio Tab")]
    public Slider masterVolume;
    public TMP_Text masterVolumeValue;
    public Slider uiSoundsVolume;
    public TMP_Text uiSoundsVolumeValue;
    public Slider ambientSoundsVolume;  
    public TMP_Text ambientSoundsVolumeValue;
    public Slider voiceChatVolume;
    public TMP_Text voiceChatVolumeValue;
    public Toggle enableVoiceChat;
    public Toggle pushToTalk;
    
    [Header("Network Tab")]
    public TMP_InputField maxBandwidth;
    public TMP_InputField httpProxyAddress;
    public TMP_InputField httpProxyPort;
    public Toggle useHttpProxy;
    public Toggle enableCaching;
    public Slider diskCacheSize;
    public TMP_Text diskCacheSizeValue;
    
    [Header("Privacy Tab")]
    public Toggle hideOnlineStatus;
    public Toggle autoAcceptFriendRequests;
    public Toggle showGroupTitles;
    public Toggle enableIMLogging;
    public Toggle enableChatLogging;
    
    private int currentTab = 0;
    private PreferencesData preferences;

    [System.Serializable]
    public class PreferencesData
    {
        [Header("General")]
        public bool showNamesOverAvatars = true;
        public bool playTypingSounds = true;
        public bool playUISounds = true;
        public float mouseSensitivity = 1.0f;
        
        [Header("Graphics")]
        public float viewDistance = 128f;
        public bool enableShadows = true;
        public bool enableReflections = false;
        public bool enableParticles = true;
        public int qualityLevel = 2; // 0=Low, 1=Medium, 2=High, 3=Ultra
        public int textureDetail = 1; // 0=Low, 1=Medium, 2=High
        public bool enableWindlight = true;
        public int maxAvatars = 50;
        
        [Header("Audio")]
        public float masterVolume = 0.8f;
        public float uiSoundsVolume = 0.7f;
        public float ambientSoundsVolume = 0.6f;
        public float voiceChatVolume = 0.8f;
        public bool enableVoiceChat = false;
        public bool pushToTalk = true;
        
        [Header("Network")]
        public int maxBandwidth = 1000;
        public string httpProxyAddress = "";
        public int httpProxyPort = 8080;
        public bool useHttpProxy = false;
        public bool enableCaching = true;
        public float diskCacheSize = 1024f; // MB
        
        [Header("Privacy")]
        public bool hideOnlineStatus = false;
        public bool autoAcceptFriendRequests = false;
        public bool showGroupTitles = true;
        public bool enableIMLogging = true;
        public bool enableChatLogging = true;
    }

    void Awake()
    {
        preferencesWindow.SetActive(false);
        LoadPreferences();
        SetupUI();
    }

    void SetupUI()
    {
        // Setup close button
        if (closeButton) closeButton.onClick.AddListener(ClosePreferences);
        
        // Setup tab buttons
        for (int i = 0; i < tabButtons.Length; i++)
        {
            int tabIndex = i;
            tabButtons[i].onClick.AddListener(() => SwitchToTab(tabIndex));
        }
        
        // Setup value change listeners
        SetupSliderListeners();
        SetupToggleListeners();
        SetupDropdownListeners();
        
        // Initialize UI with current preferences
        UpdateUIFromPreferences();
        SwitchToTab(0);
    }

    void SetupSliderListeners()
    {
        if (mouseSensitivity) mouseSensitivity.onValueChanged.AddListener(OnMouseSensitivityChanged);
        if (viewDistance) viewDistance.onValueChanged.AddListener(OnViewDistanceChanged);
        if (maxAvatars) maxAvatars.onValueChanged.AddListener(OnMaxAvatarsChanged);
        if (masterVolume) masterVolume.onValueChanged.AddListener(OnMasterVolumeChanged);
        if (uiSoundsVolume) uiSoundsVolume.onValueChanged.AddListener(OnUISoundsVolumeChanged);
        if (ambientSoundsVolume) ambientSoundsVolume.onValueChanged.AddListener(OnAmbientSoundsVolumeChanged);
        if (voiceChatVolume) voiceChatVolume.onValueChanged.AddListener(OnVoiceChatVolumeChanged);
        if (diskCacheSize) diskCacheSize.onValueChanged.AddListener(OnDiskCacheSizeChanged);
    }

    void SetupToggleListeners()
    {
        if (showNamesOverAvatars) showNamesOverAvatars.onValueChanged.AddListener(OnShowNamesChanged);
        if (playTypingSounds) playTypingSounds.onValueChanged.AddListener(OnPlayTypingSoundsChanged);
        if (playUISounds) playUISounds.onValueChanged.AddListener(OnPlayUISoundsChanged);
        if (enableShadows) enableShadows.onValueChanged.AddListener(OnEnableShadowsChanged);
        if (enableReflections) enableReflections.onValueChanged.AddListener(OnEnableReflectionsChanged);
        if (enableParticles) enableParticles.onValueChanged.AddListener(OnEnableParticlesChanged);
        if (enableWindlight) enableWindlight.onValueChanged.AddListener(OnEnableWindlightChanged);
        if (enableVoiceChat) enableVoiceChat.onValueChanged.AddListener(OnEnableVoiceChatChanged);
        if (pushToTalk) pushToTalk.onValueChanged.AddListener(OnPushToTalkChanged);
        if (useHttpProxy) useHttpProxy.onValueChanged.AddListener(OnUseHttpProxyChanged);
        if (enableCaching) enableCaching.onValueChanged.AddListener(OnEnableCachingChanged);
        if (hideOnlineStatus) hideOnlineStatus.onValueChanged.AddListener(OnHideOnlineStatusChanged);
        if (autoAcceptFriendRequests) autoAcceptFriendRequests.onValueChanged.AddListener(OnAutoAcceptFriendsChanged);
        if (showGroupTitles) showGroupTitles.onValueChanged.AddListener(OnShowGroupTitlesChanged);
        if (enableIMLogging) enableIMLogging.onValueChanged.AddListener(OnEnableIMLoggingChanged);
        if (enableChatLogging) enableChatLogging.onValueChanged.AddListener(OnEnableChatLoggingChanged);
    }

    void SetupDropdownListeners()
    {
        if (qualityDropdown) qualityDropdown.onValueChanged.AddListener(OnQualityChanged);
        if (textureDetailDropdown) textureDetailDropdown.onValueChanged.AddListener(OnTextureDetailChanged);
    }

    public void ShowPreferences()
    {
        preferencesWindow.SetActive(true);
        UpdateUIFromPreferences();
    }

    public void ClosePreferences()
    {
        preferencesWindow.SetActive(false);
        SavePreferences();
    }

    public void SwitchToTab(int tabIndex)
    {
        if (tabIndex < 0 || tabIndex >= tabPanels.Length) return;
        
        currentTab = tabIndex;
        
        // Hide all panels
        for (int i = 0; i < tabPanels.Length; i++)
        {
            tabPanels[i].SetActive(i == tabIndex);
        }
        
        // Update tab button colors
        for (int i = 0; i < tabButtons.Length; i++)
        {
            var colors = tabButtons[i].colors;
            colors.normalColor = (i == tabIndex) ? activeTabColor : inactiveTabColor;
            tabButtons[i].colors = colors;
        }
    }

    void UpdateUIFromPreferences()
    {
        // General
        if (showNamesOverAvatars) showNamesOverAvatars.isOn = preferences.showNamesOverAvatars;
        if (playTypingSounds) playTypingSounds.isOn = preferences.playTypingSounds;
        if (playUISounds) playUISounds.isOn = preferences.playUISounds;
        if (mouseSensitivity) mouseSensitivity.value = preferences.mouseSensitivity;
        
        // Graphics
        if (viewDistance) viewDistance.value = preferences.viewDistance;
        if (enableShadows) enableShadows.isOn = preferences.enableShadows;
        if (enableReflections) enableReflections.isOn = preferences.enableReflections;
        if (enableParticles) enableParticles.isOn = preferences.enableParticles;
        if (qualityDropdown) qualityDropdown.value = preferences.qualityLevel;
        if (textureDetailDropdown) textureDetailDropdown.value = preferences.textureDetail;
        if (enableWindlight) enableWindlight.isOn = preferences.enableWindlight;
        if (maxAvatars) maxAvatars.value = preferences.maxAvatars;
        
        // Audio
        if (masterVolume) masterVolume.value = preferences.masterVolume;
        if (uiSoundsVolume) uiSoundsVolume.value = preferences.uiSoundsVolume;
        if (ambientSoundsVolume) ambientSoundsVolume.value = preferences.ambientSoundsVolume;
        if (voiceChatVolume) voiceChatVolume.value = preferences.voiceChatVolume;
        if (enableVoiceChat) enableVoiceChat.isOn = preferences.enableVoiceChat;
        if (pushToTalk) pushToTalk.isOn = preferences.pushToTalk;
        
        // Network
        if (maxBandwidth) maxBandwidth.text = preferences.maxBandwidth.ToString();
        if (httpProxyAddress) httpProxyAddress.text = preferences.httpProxyAddress;
        if (httpProxyPort) httpProxyPort.text = preferences.httpProxyPort.ToString();
        if (useHttpProxy) useHttpProxy.isOn = preferences.useHttpProxy;
        if (enableCaching) enableCaching.isOn = preferences.enableCaching;
        if (diskCacheSize) diskCacheSize.value = preferences.diskCacheSize;
        
        // Privacy
        if (hideOnlineStatus) hideOnlineStatus.isOn = preferences.hideOnlineStatus;
        if (autoAcceptFriendRequests) autoAcceptFriendRequests.isOn = preferences.autoAcceptFriendRequests;
        if (showGroupTitles) showGroupTitles.isOn = preferences.showGroupTitles;
        if (enableIMLogging) enableIMLogging.isOn = preferences.enableIMLogging;
        if (enableChatLogging) enableChatLogging.isOn = preferences.enableChatLogging;
        
        UpdateValueLabels();
    }

    void UpdateValueLabels()
    {
        if (mouseSensitivityValue) mouseSensitivityValue.text = preferences.mouseSensitivity.ToString("F1");
        if (viewDistanceValue) viewDistanceValue.text = preferences.viewDistance.ToString("F0") + "m";
        if (maxAvatarsValue) maxAvatarsValue.text = preferences.maxAvatars.ToString();
        if (masterVolumeValue) masterVolumeValue.text = (preferences.masterVolume * 100).ToString("F0") + "%";
        if (uiSoundsVolumeValue) uiSoundsVolumeValue.text = (preferences.uiSoundsVolume * 100).ToString("F0") + "%";
        if (ambientSoundsVolumeValue) ambientSoundsVolumeValue.text = (preferences.ambientSoundsVolume * 100).ToString("F0") + "%";
        if (voiceChatVolumeValue) voiceChatVolumeValue.text = (preferences.voiceChatVolume * 100).ToString("F0") + "%";
        if (diskCacheSizeValue) diskCacheSizeValue.text = preferences.diskCacheSize.ToString("F0") + " MB";
    }

    #region Event Handlers

    void OnMouseSensitivityChanged(float value)
    {
        preferences.mouseSensitivity = value;
        if (mouseSensitivityValue) mouseSensitivityValue.text = value.ToString("F1");
    }

    void OnViewDistanceChanged(float value)
    {
        preferences.viewDistance = value;
        ClientManager.viewDistance = value;
        if (viewDistanceValue) viewDistanceValue.text = value.ToString("F0") + "m";
    }

    void OnMaxAvatarsChanged(float value)
    {
        preferences.maxAvatars = (int)value;
        if (maxAvatarsValue) maxAvatarsValue.text = ((int)value).ToString();
    }

    void OnMasterVolumeChanged(float value)
    {
        preferences.masterVolume = value;
        AudioListener.volume = value;
        if (masterVolumeValue) masterVolumeValue.text = (value * 100).ToString("F0") + "%";
    }

    void OnUISoundsVolumeChanged(float value)
    {
        preferences.uiSoundsVolume = value;
        if (uiSoundsVolumeValue) uiSoundsVolumeValue.text = (value * 100).ToString("F0") + "%";
    }

    void OnAmbientSoundsVolumeChanged(float value)
    {
        preferences.ambientSoundsVolume = value;
        if (ambientSoundsVolumeValue) ambientSoundsVolumeValue.text = (value * 100).ToString("F0") + "%";
    }

    void OnVoiceChatVolumeChanged(float value)
    {
        preferences.voiceChatVolume = value;
        if (voiceChatVolumeValue) voiceChatVolumeValue.text = (value * 100).ToString("F0") + "%";
    }

    void OnDiskCacheSizeChanged(float value)
    {
        preferences.diskCacheSize = value;
        if (diskCacheSizeValue) diskCacheSizeValue.text = value.ToString("F0") + " MB";
    }

    void OnShowNamesChanged(bool value)
    {
        preferences.showNamesOverAvatars = value;
    }

    void OnPlayTypingSoundsChanged(bool value)
    {
        preferences.playTypingSounds = value;
    }

    void OnPlayUISoundsChanged(bool value)
    {
        preferences.playUISounds = value;
    }

    void OnEnableShadowsChanged(bool value)
    {
        preferences.enableShadows = value;
        // Apply shadow settings
        ApplyGraphicsSettings();
    }

    void OnEnableReflectionsChanged(bool value)
    {
        preferences.enableReflections = value;
        ApplyGraphicsSettings();
    }

    void OnEnableParticlesChanged(bool value)
    {
        preferences.enableParticles = value;
    }

    void OnEnableWindlightChanged(bool value)
    {
        preferences.enableWindlight = value;
    }

    void OnEnableVoiceChatChanged(bool value)
    {
        preferences.enableVoiceChat = value;
    }

    void OnPushToTalkChanged(bool value)
    {
        preferences.pushToTalk = value;
    }

    void OnUseHttpProxyChanged(bool value)
    {
        preferences.useHttpProxy = value;
    }

    void OnEnableCachingChanged(bool value)
    {
        preferences.enableCaching = value;
    }

    void OnHideOnlineStatusChanged(bool value)
    {
        preferences.hideOnlineStatus = value;
    }

    void OnAutoAcceptFriendsChanged(bool value)
    {
        preferences.autoAcceptFriendRequests = value;
    }

    void OnShowGroupTitlesChanged(bool value)
    {
        preferences.showGroupTitles = value;
    }

    void OnEnableIMLoggingChanged(bool value)
    {
        preferences.enableIMLogging = value;
    }

    void OnEnableChatLoggingChanged(bool value)
    {
        preferences.enableChatLogging = value;
    }

    void OnQualityChanged(int value)
    {
        preferences.qualityLevel = value;
        QualitySettings.SetQualityLevel(value);
        ApplyGraphicsSettings();
    }

    void OnTextureDetailChanged(int value)
    {
        preferences.textureDetail = value;
    }

    #endregion

    void ApplyGraphicsSettings()
    {
        // Apply shadow settings
        if (preferences.enableShadows)
        {
            QualitySettings.shadows = ShadowQuality.All;
            QualitySettings.shadowResolution = ShadowResolution.High;
        }
        else
        {
            QualitySettings.shadows = ShadowQuality.Disable;
        }
        
        // Apply reflection settings (this would need more implementation)
        // Apply other graphics settings as needed
    }

    void LoadPreferences()
    {
        string json = PlayerPrefs.GetString("CrystalFrostPreferences", "");
        if (!string.IsNullOrEmpty(json))
        {
            try
            {
                preferences = JsonUtility.FromJson<PreferencesData>(json);
            }
            catch
            {
                preferences = new PreferencesData();
            }
        }
        else
        {
            preferences = new PreferencesData();
        }
    }

    void SavePreferences()
    {
        // Update network settings from input fields
        if (maxBandwidth && int.TryParse(maxBandwidth.text, out int bandwidth))
        {
            preferences.maxBandwidth = bandwidth;
        }
        
        if (httpProxyPort && int.TryParse(httpProxyPort.text, out int port))
        {
            preferences.httpProxyPort = port;
        }
        
        if (httpProxyAddress)
        {
            preferences.httpProxyAddress = httpProxyAddress.text;
        }
        
        string json = JsonUtility.ToJson(preferences, true);
        PlayerPrefs.SetString("CrystalFrostPreferences", json);
        PlayerPrefs.Save();
        
        ApplyPreferences();
    }

    void ApplyPreferences()
    {
        // Apply preferences to the client and game systems
        ClientManager.viewDistance = preferences.viewDistance;
        AudioListener.volume = preferences.masterVolume;
        
        // Apply client settings
        if (ClientManager.client != null)
        {
            ClientManager.client.Self.Movement.Camera.Far = preferences.viewDistance;
        }
        
        ApplyGraphicsSettings();
    }

    void Start()
    {
        ApplyPreferences();
    }
}