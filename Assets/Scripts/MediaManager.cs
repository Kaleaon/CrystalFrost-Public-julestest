using OpenMetaverse;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Video;
using TMPro;
using UnityEngine.UI;
using System.Collections;
using System;

public class MediaManager : MonoBehaviour
{
    [Header("Media Player UI")]
    public GameObject mediaPlayerWindow;
    public Button closeButton;
    public Button playButton;
    public Button pauseButton;
    public Button stopButton;
    public Button muteButton;
    public Slider volumeSlider;
    public Slider progressSlider;
    public TMP_Text mediaTitle;
    public TMP_Text currentTime;
    public TMP_Text totalTime;
    public RawImage videoDisplay;
    
    [Header("Media Controls")]
    public Button mediaOnPrimButton;
    public TMP_InputField mediaUrlField;
    public Button setMediaButton;
    public Toggle loopMediaToggle;
    public Toggle autoPlayToggle;
    
    [Header("Parcel Media")]
    public GameObject parcelMediaPanel;
    public TMP_Text parcelMediaTitle;
    public TMP_Text parcelMediaUrl;
    public Button playParcelMediaButton;
    public Button stopParcelMediaButton;
    public Slider parcelVolumeSlider;
    
    [Header("Music Stream")]
    public GameObject musicStreamPanel;
    public TMP_Text streamTitle;
    public TMP_Text streamUrl;
    public Button playStreamButton;
    public Button stopStreamButton;
    public Slider musicVolumeSlider;
    
    private GridClient client;
    private Dictionary<UUID, MediaObject> mediaObjects = new();
    private VideoPlayer videoPlayer;
    private AudioSource audioSource;
    private AudioSource musicAudioSource;
    private string currentParcelMediaUrl = "";
    private string currentMusicStreamUrl = "";
    private bool isPlayingParcelMedia = false;
    private bool isPlayingMusicStream = false;
    
    public class MediaObject
    {
        public GameObject primObject;
        public Primitive primitive;
        public string mediaUrl;
        public VideoPlayer videoPlayer;
        public AudioSource audioSource;
        public MediaType mediaType;
        public bool isPlaying;
        public bool isLooping;
        public float volume = 1.0f;
    }
    
    public enum MediaType
    {
        Video,
        Audio,
        Image,
        Web
    }

    void Awake()
    {
        mediaPlayerWindow.SetActive(false);
        SetupUI();
        SetupMediaPlayers();
    }

    void SetupUI()
    {
        if (closeButton) closeButton.onClick.AddListener(() => mediaPlayerWindow.SetActive(false));
        if (playButton) playButton.onClick.AddListener(PlayMedia);
        if (pauseButton) pauseButton.onClick.AddListener(PauseMedia);
        if (stopButton) stopButton.onClick.AddListener(StopMedia);
        if (muteButton) muteButton.onClick.AddListener(ToggleMute);
        
        if (volumeSlider) volumeSlider.onValueChanged.AddListener(OnVolumeChanged);
        if (progressSlider) progressSlider.onValueChanged.AddListener(OnProgressChanged);
        
        // Media on prim controls
        if (setMediaButton) setMediaButton.onClick.AddListener(SetMediaOnPrim);
        
        // Parcel media controls
        if (playParcelMediaButton) playParcelMediaButton.onClick.AddListener(PlayParcelMedia);
        if (stopParcelMediaButton) stopParcelMediaButton.onClick.AddListener(StopParcelMedia);
        if (parcelVolumeSlider) parcelVolumeSlider.onValueChanged.AddListener(OnParcelVolumeChanged);
        
        // Music stream controls
        if (playStreamButton) playStreamButton.onClick.AddListener(PlayMusicStream);
        if (stopStreamButton) stopStreamButton.onClick.AddListener(StopMusicStream);
        if (musicVolumeSlider) musicVolumeSlider.onValueChanged.AddListener(OnMusicVolumeChanged);
    }

    void SetupMediaPlayers()
    {
        // Create main video player
        var videoPlayerObj = new GameObject("MainVideoPlayer");
        videoPlayerObj.transform.parent = transform;
        videoPlayer = videoPlayerObj.AddComponent<VideoPlayer>();
        videoPlayer.playOnAwake = false;
        videoPlayer.isLooping = false;
        videoPlayer.renderMode = VideoRenderMode.RenderTexture;
        
        // Create audio source for media
        var audioObj = new GameObject("MediaAudio");
        audioObj.transform.parent = transform;
        audioSource = audioObj.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.volume = 0.8f;
        
        // Create audio source for music streams
        var musicObj = new GameObject("MusicAudio");
        musicObj.transform.parent = transform;
        musicAudioSource = musicObj.AddComponent<AudioSource>();
        musicAudioSource.playOnAwake = false;
        musicAudioSource.volume = 0.6f;
        
        // Setup video display
        if (videoDisplay)
        {
            var renderTexture = new RenderTexture(1024, 768, 16);
            videoPlayer.targetTexture = renderTexture;
            videoDisplay.texture = renderTexture;
        }
    }

    void Start()
    {
        client = ClientManager.client;
        
        if (client != null)
        {
            client.Parcels.ParcelMediaUpdate += OnParcelMediaUpdate;
            client.Parcels.ParcelProperties += OnParcelProperties;
            client.Objects.ObjectProperties += OnObjectProperties;
        }
        
        StartCoroutine(UpdateMediaUI());
    }

    void OnDestroy()
    {
        if (client != null)
        {
            client.Parcels.ParcelMediaUpdate -= OnParcelMediaUpdate;
            client.Parcels.ParcelProperties -= OnParcelProperties;
            client.Objects.ObjectProperties -= OnObjectProperties;
        }
    }

    public void ShowMediaPlayer()
    {
        mediaPlayerWindow.SetActive(true);
        RefreshParcelMedia();
    }

    IEnumerator UpdateMediaUI()
    {
        while (true)
        {
            if (videoPlayer && videoPlayer.isPlaying)
            {
                UpdateVideoProgress();
            }
            
            UpdateMediaObjects();
            yield return new WaitForSeconds(0.1f);
        }
    }

    void UpdateVideoProgress()
    {
        if (videoPlayer.frameCount > 0)
        {
            float progress = (float)videoPlayer.frame / videoPlayer.frameCount;
            if (progressSlider && !progressSlider.interactable)
            {
                progressSlider.value = progress;
            }
            
            if (currentTime)
            {
                var current = TimeSpan.FromSeconds(videoPlayer.time);
                currentTime.text = $"{current.Minutes:D2}:{current.Seconds:D2}";
            }
            
            if (totalTime && videoPlayer.length > 0)
            {
                var total = TimeSpan.FromSeconds(videoPlayer.length);
                totalTime.text = $"{total.Minutes:D2}:{total.Seconds:D2}";
            }
        }
    }

    void UpdateMediaObjects()
    {
        // Update all media objects in the scene
        var objectsToRemove = new List<UUID>();
        
        foreach (var kvp in mediaObjects)
        {
            var mediaObj = kvp.Value;
            
            if (mediaObj.primObject == null)
            {
                objectsToRemove.Add(kvp.Key);
                continue;
            }
            
            // Update media object state
            if (mediaObj.videoPlayer && mediaObj.videoPlayer.isPlaying)
            {
                // Apply video texture to object
                ApplyMediaTexture(mediaObj);
            }
        }
        
        // Clean up destroyed objects
        foreach (var id in objectsToRemove)
        {
            mediaObjects.Remove(id);
        }
    }

    void ApplyMediaTexture(MediaObject mediaObj)
    {
        if (mediaObj.videoPlayer == null || mediaObj.primObject == null) return;
        
        var renderer = mediaObj.primObject.GetComponent<MeshRenderer>();
        if (renderer && mediaObj.videoPlayer.targetTexture)
        {
            renderer.material.mainTexture = mediaObj.videoPlayer.targetTexture;
        }
    }

    #region Event Handlers

    void OnParcelMediaUpdate(object sender, ParcelMediaUpdateEventArgs e)
    {
        UnityMainThreadDispatcher.Instance().Enqueue(() =>
        {
            currentParcelMediaUrl = e.MediaURL;
            if (parcelMediaUrl) parcelMediaUrl.text = e.MediaURL;
            if (parcelMediaTitle) parcelMediaTitle.text = "Parcel Media";
            
            // Auto-play if enabled
            if (autoPlayToggle && autoPlayToggle.isOn && !string.IsNullOrEmpty(e.MediaURL))
            {
                PlayParcelMedia();
            }
        });
    }

    void OnParcelProperties(object sender, ParcelPropertiesEventArgs e)
    {
        UnityMainThreadDispatcher.Instance().Enqueue(() =>
        {
            if (!string.IsNullOrEmpty(e.Result.MusicURL))
            {
                currentMusicStreamUrl = e.Result.MusicURL;
                if (streamUrl) streamUrl.text = e.Result.MusicURL;
                if (streamTitle) streamTitle.text = "Parcel Music";
                
                // Auto-play music if enabled
                if (autoPlayToggle && autoPlayToggle.isOn)
                {
                    PlayMusicStream();
                }
            }
            
            if (!string.IsNullOrEmpty(e.Result.MediaURL))
            {
                currentParcelMediaUrl = e.Result.MediaURL;
                if (parcelMediaUrl) parcelMediaUrl.text = e.Result.MediaURL;
            }
        });
    }

    void OnObjectProperties(object sender, ObjectPropertiesEventArgs e)
    {
        // Handle object media properties
        UnityMainThreadDispatcher.Instance().Enqueue(() =>
        {
            if (!string.IsNullOrEmpty(e.Properties.MediaURL))
            {
                CreateMediaObject(e.Properties);
            }
        });
    }

    #endregion

    void CreateMediaObject(Primitive.ObjectProperties properties)
    {
        if (properties.ObjectID == UUID.Zero) return;
        
        // Find the game object for this primitive
        GameObject primObj = FindPrimitiveObject(properties.ObjectID);
        if (primObj == null) return;
        
        var mediaObj = new MediaObject
        {
            primObject = primObj,
            mediaUrl = properties.MediaURL,
            mediaType = GetMediaType(properties.MediaURL),
            volume = 1.0f,
            isLooping = false
        };
        
        // Create video player for this object
        var videoPlayerObj = new GameObject($"MediaPlayer_{properties.ObjectID}");
        videoPlayerObj.transform.parent = primObj.transform;
        
        mediaObj.videoPlayer = videoPlayerObj.AddComponent<VideoPlayer>();
        mediaObj.videoPlayer.playOnAwake = false;
        mediaObj.videoPlayer.renderMode = VideoRenderMode.RenderTexture;
        
        // Create render texture for the video
        var renderTexture = new RenderTexture(512, 512, 16);
        mediaObj.videoPlayer.targetTexture = renderTexture;
        
        mediaObjects[properties.ObjectID] = mediaObj;
        
        // Auto-play if media should start
        if (ShouldAutoPlayMedia(properties.MediaURL))
        {
            PlayMediaObject(mediaObj);
        }
    }

    GameObject FindPrimitiveObject(UUID objectID)
    {
        // Search through scene prims to find the matching object
        if (ClientManager.simManager != null)
        {
            foreach (var kvp in ClientManager.simManager.scenePrims)
            {
                var scenePrim = kvp.Value;
                if (scenePrim.uuid == objectID)
                {
                    return scenePrim.obj;
                }
            }
        }
        return null;
    }

    MediaType GetMediaType(string url)
    {
        if (string.IsNullOrEmpty(url)) return MediaType.Web;
        
        var lowerUrl = url.ToLower();
        
        if (lowerUrl.Contains(".mp4") || lowerUrl.Contains(".avi") || lowerUrl.Contains(".mov") || 
            lowerUrl.Contains(".webm") || lowerUrl.Contains("youtube.com") || lowerUrl.Contains("vimeo.com"))
        {
            return MediaType.Video;
        }
        
        if (lowerUrl.Contains(".mp3") || lowerUrl.Contains(".wav") || lowerUrl.Contains(".ogg") ||
            lowerUrl.Contains(".aac") || lowerUrl.Contains("stream") || lowerUrl.Contains("radio"))
        {
            return MediaType.Audio;
        }
        
        if (lowerUrl.Contains(".jpg") || lowerUrl.Contains(".png") || lowerUrl.Contains(".gif") ||
            lowerUrl.Contains(".bmp") || lowerUrl.Contains(".tga"))
        {
            return MediaType.Image;
        }
        
        return MediaType.Web;
    }

    bool ShouldAutoPlayMedia(string url)
    {
        // Check if this media should auto-play based on settings
        return autoPlayToggle && autoPlayToggle.isOn && !string.IsNullOrEmpty(url);
    }

    void PlayMediaObject(MediaObject mediaObj)
    {
        if (mediaObj == null || string.IsNullOrEmpty(mediaObj.mediaUrl)) return;
        
        try
        {
            switch (mediaObj.mediaType)
            {
                case MediaType.Video:
                    mediaObj.videoPlayer.url = mediaObj.mediaUrl;
                    mediaObj.videoPlayer.isLooping = mediaObj.isLooping;
                    mediaObj.videoPlayer.Play();
                    mediaObj.isPlaying = true;
                    break;
                    
                case MediaType.Audio:
                    if (mediaObj.audioSource == null)
                    {
                        mediaObj.audioSource = mediaObj.primObject.AddComponent<AudioSource>();
                    }
                    StartCoroutine(PlayAudioFromUrl(mediaObj.audioSource, mediaObj.mediaUrl));
                    mediaObj.isPlaying = true;
                    break;
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"Failed to play media: {ex.Message}");
        }
    }

    IEnumerator PlayAudioFromUrl(AudioSource audioSource, string url)
    {
        using (var www = UnityEngine.Networking.UnityWebRequestMultimedia.GetAudioClip(url, AudioType.MPEG))
        {
            yield return www.SendWebRequest();
            
            if (www.result == UnityEngine.Networking.UnityWebRequest.Result.Success)
            {
                var audioClip = UnityEngine.Networking.DownloadHandlerAudioClip.GetContent(www);
                audioSource.clip = audioClip;
                audioSource.Play();
            }
            else
            {
                Debug.LogError($"Failed to load audio from {url}: {www.error}");
            }
        }
    }

    #region UI Controls

    void PlayMedia()
    {
        if (videoPlayer && !videoPlayer.isPlaying)
        {
            videoPlayer.Play();
        }
    }

    void PauseMedia()
    {
        if (videoPlayer && videoPlayer.isPlaying)
        {
            videoPlayer.Pause();
        }
    }

    void StopMedia()
    {
        if (videoPlayer)
        {
            videoPlayer.Stop();
        }
    }

    void ToggleMute()
    {
        if (audioSource)
        {
            audioSource.mute = !audioSource.mute;
        }
        
        if (musicAudioSource)
        {
            musicAudioSource.mute = !musicAudioSource.mute;
        }
        
        // Update button appearance
        if (muteButton)
        {
            var buttonText = muteButton.GetComponentInChildren<TMP_Text>();
            if (buttonText)
            {
                buttonText.text = audioSource.mute ? "Unmute" : "Mute";
            }
        }
    }

    void OnVolumeChanged(float value)
    {
        if (audioSource) audioSource.volume = value;
        if (videoPlayer) videoPlayer.SetDirectAudioVolume(0, value);
    }

    void OnProgressChanged(float value)
    {
        if (videoPlayer && videoPlayer.frameCount > 0)
        {
            videoPlayer.frame = (long)(value * videoPlayer.frameCount);
        }
    }

    void SetMediaOnPrim()
    {
        if (mediaUrlField == null || string.IsNullOrEmpty(mediaUrlField.text)) return;
        
        // This would set media on the selected object
        Debug.Log($"Setting media URL: {mediaUrlField.text}");
        
        // Implementation would depend on object selection system
        // and LibreMetaverse media setting methods
    }

    void PlayParcelMedia()
    {
        if (string.IsNullOrEmpty(currentParcelMediaUrl)) return;
        
        try
        {
            videoPlayer.url = currentParcelMediaUrl;
            videoPlayer.isLooping = loopMediaToggle ? loopMediaToggle.isOn : false;
            videoPlayer.Play();
            isPlayingParcelMedia = true;
            
            if (mediaTitle) mediaTitle.text = "Parcel Media";
        }
        catch (Exception ex)
        {
            Debug.LogError($"Failed to play parcel media: {ex.Message}");
        }
    }

    void StopParcelMedia()
    {
        if (videoPlayer && isPlayingParcelMedia)
        {
            videoPlayer.Stop();
            isPlayingParcelMedia = false;
        }
    }

    void OnParcelVolumeChanged(float value)
    {
        if (videoPlayer && isPlayingParcelMedia)
        {
            videoPlayer.SetDirectAudioVolume(0, value);
        }
    }

    void PlayMusicStream()
    {
        if (string.IsNullOrEmpty(currentMusicStreamUrl)) return;
        
        StartCoroutine(PlayMusicStreamCoroutine());
    }

    IEnumerator PlayMusicStreamCoroutine()
    {
        using (var www = UnityEngine.Networking.UnityWebRequestMultimedia.GetAudioClip(currentMusicStreamUrl, AudioType.MPEG))
        {
            yield return www.SendWebRequest();
            
            if (www.result == UnityEngine.Networking.UnityWebRequest.Result.Success)
            {
                var audioClip = UnityEngine.Networking.DownloadHandlerAudioClip.GetContent(www);
                musicAudioSource.clip = audioClip;
                musicAudioSource.Play();
                isPlayingMusicStream = true;
                
                if (streamTitle) streamTitle.text = "Playing Music Stream";
            }
            else
            {
                Debug.LogError($"Failed to load music stream: {www.error}");
            }
        }
    }

    void StopMusicStream()
    {
        if (musicAudioSource && isPlayingMusicStream)
        {
            musicAudioSource.Stop();
            isPlayingMusicStream = false;
            
            if (streamTitle) streamTitle.text = "Music Stream Stopped";
        }
    }

    void OnMusicVolumeChanged(float value)
    {
        if (musicAudioSource)
        {
            musicAudioSource.volume = value;
        }
    }

    #endregion

    void RefreshParcelMedia()
    {
        if (client != null && client.Network.CurrentSim != null)
        {
            // Request current parcel information
            client.Parcels.RequestParcelProperties(client.Network.CurrentSim, 
                client.Self.SimPosition.X, client.Self.SimPosition.Y, 0);
        }
    }

    // Public methods for external control
    public void SetMediaUrl(string url)
    {
        if (mediaUrlField) mediaUrlField.text = url;
    }

    public void PlayMediaUrl(string url)
    {
        if (string.IsNullOrEmpty(url)) return;
        
        try
        {
            videoPlayer.url = url;
            videoPlayer.Play();
            
            if (mediaTitle) mediaTitle.text = "Custom Media";
        }
        catch (Exception ex)
        {
            Debug.LogError($"Failed to play media URL: {ex.Message}");
        }
    }

    public void StopAllMedia()
    {
        StopMedia();
        StopParcelMedia();
        StopMusicStream();
        
        // Stop all media objects
        foreach (var mediaObj in mediaObjects.Values)
        {
            if (mediaObj.videoPlayer && mediaObj.videoPlayer.isPlaying)
            {
                mediaObj.videoPlayer.Stop();
            }
            
            if (mediaObj.audioSource && mediaObj.audioSource.isPlaying)
            {
                mediaObj.audioSource.Stop();
            }
            
            mediaObj.isPlaying = false;
        }
    }
}