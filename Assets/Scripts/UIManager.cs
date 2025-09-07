using UnityEngine;
using CrystalFrost.UI;
using Microsoft.Extensions.Logging;

public class UIManager : MonoBehaviour
{
    private IUIStateTracker _uiStateTracker;
    private ILogger<UIManager> _logger;

    private void Start()
    {
        // Find the UI state tracker in the scene
        _uiStateTracker = FindObjectOfType<UIStateTracker>();
        
        try
        {
            _logger = Services.GetService<ILogger<UIManager>>();
            _logger.LogInformation("UIManager initialized with UI state tracking");
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Failed to get logger for UIManager: {ex.Message}");
        }

        if (_uiStateTracker == null)
        {
            Debug.LogWarning("UIStateTracker not found in scene. UI tracking will be disabled.");
        }
    }

    public void TogglePanel(GameObject panel)
    {
        if (panel != null)
        {
            bool wasActive = panel.activeSelf;
            panel.SetActive(!wasActive);
            
            // Track the visibility change
            if (_uiStateTracker != null)
            {
                _uiStateTracker.TrackVisibilityChanged(panel, "Panel", !wasActive);
            }
        }
    }

    /// <summary>
    /// Show a panel and track the change
    /// </summary>
    public void ShowPanel(GameObject panel, string panelType = "Panel")
    {
        if (panel != null && !panel.activeSelf)
        {
            panel.SetActive(true);
            
            if (_uiStateTracker != null)
            {
                _uiStateTracker.TrackVisibilityChanged(panel, panelType, true);
            }
        }
    }

    /// <summary>
    /// Hide a panel and track the change
    /// </summary>
    public void HidePanel(GameObject panel, string panelType = "Panel")
    {
        if (panel != null && panel.activeSelf)
        {
            panel.SetActive(false);
            
            if (_uiStateTracker != null)
            {
                _uiStateTracker.TrackVisibilityChanged(panel, panelType, false);
            }
        }
    }

    /// <summary>
    /// Create a new UI component and track it
    /// </summary>
    public GameObject CreateUIComponent(GameObject prefab, Transform parent, string componentType)
    {
        if (prefab == null) return null;

        GameObject newComponent = Instantiate(prefab, parent);
        
        if (_uiStateTracker != null)
        {
            _uiStateTracker.TrackComponentCreated(newComponent, componentType);
        }

        return newComponent;
    }

    /// <summary>
    /// Destroy a UI component and track it
    /// </summary>
    public void DestroyUIComponent(GameObject component, string componentType)
    {
        if (component == null) return;

        string componentId = $"{GetHierarchyPath(component)}_{component.GetInstanceID()}";
        
        if (_uiStateTracker != null)
        {
            _uiStateTracker.TrackComponentDestroyed(componentId, componentType);
        }

        Destroy(component);
    }

    private string GetHierarchyPath(GameObject gameObject)
    {
        string path = gameObject.name;
        Transform current = gameObject.transform.parent;

        while (current != null)
        {
            path = current.name + "/" + path;
            current = current.parent;
        }

        return path;
    }
}
