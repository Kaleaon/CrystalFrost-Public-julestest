using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using UnityEngine;
using Microsoft.Extensions.Logging;

namespace CrystalFrost.UI
{
    /// <summary>
    /// Interface for UI state tracking service
    /// </summary>
    public interface IUIStateTracker
    {
        /// <summary>
        /// Event fired when a UI change occurs
        /// </summary>
        event Action<UIChangePacket> UIChangeOccurred;

        /// <summary>
        /// Track a UI component creation
        /// </summary>
        void TrackComponentCreated(GameObject uiComponent, string componentType);

        /// <summary>
        /// Track a UI component destruction
        /// </summary>
        void TrackComponentDestroyed(string componentId, string componentType);

        /// <summary>
        /// Track a UI component visibility change
        /// </summary>
        void TrackVisibilityChanged(GameObject uiComponent, string componentType, bool isVisible);

        /// <summary>
        /// Track a UI component position/size change
        /// </summary>
        void TrackComponentMoved(GameObject uiComponent, string componentType);

        /// <summary>
        /// Track a UI component content change
        /// </summary>
        void TrackContentChanged(GameObject uiComponent, string componentType, object changeData);

        /// <summary>
        /// Track a UI interaction event
        /// </summary>
        void TrackInteraction(GameObject uiComponent, string componentType, string interactionType, object data = null);

        /// <summary>
        /// Get all pending UI changes
        /// </summary>
        List<UIChangePacket> GetPendingChanges();

        /// <summary>
        /// Clear all pending changes
        /// </summary>
        void ClearPendingChanges();
    }

    /// <summary>
    /// Service that tracks UI state changes across all UI components in Crystal Frost
    /// </summary>
    public class UIStateTracker : MonoBehaviour, IUIStateTracker
    {
        private ILogger<UIStateTracker> _logger;
        private readonly ConcurrentQueue<UIChangePacket> _pendingChanges = new ConcurrentQueue<UIChangePacket>();
        private readonly Dictionary<GameObject, string> _trackedComponents = new Dictionary<GameObject, string>();
        private string _currentUserContext;

        /// <summary>
        /// Event fired when a UI change occurs
        /// </summary>
        public event Action<UIChangePacket> UIChangeOccurred;

        private void Awake()
        {
            _currentUserContext = GetCurrentUserContext();
        }

        private void Start()
        {
            try
            {
                _logger = Services.GetService<ILogger<UIStateTracker>>();
                _logger.LogInformation("UIStateTracker initialized");
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"Could not get logger service for UIStateTracker: {ex.Message}");
                // Continue without logger - will cause NullReference warnings but functionality will work
            }
        }

        /// <summary>
        /// Track a UI component creation
        /// </summary>
        public void TrackComponentCreated(GameObject uiComponent, string componentType)
        {
            if (uiComponent == null) return;

            string componentId = GetComponentId(uiComponent);
            _trackedComponents[uiComponent] = componentId;

            var rect = uiComponent.GetComponent<RectTransform>();
            Vector3 position = rect ? rect.anchoredPosition3D : uiComponent.transform.position;
            Vector2 size = rect ? rect.sizeDelta : Vector2.zero;
            bool isVisible = uiComponent.activeInHierarchy;

            var packet = UIChangePacket.CreateCreatedPacket(componentId, componentType, position, size, isVisible, _currentUserContext);
            
            _pendingChanges.Enqueue(packet);
            UIChangeOccurred?.Invoke(packet);

            SafeLogDebug($"Tracked component created: {componentType} ({componentId})");
        }

        /// <summary>
        /// Track a UI component destruction
        /// </summary>
        public void TrackComponentDestroyed(string componentId, string componentType)
        {
            var packet = new UIChangePacket
            {
                ComponentId = componentId,
                ComponentType = componentType,
                ChangeType = UIChangeType.Destroyed,
                UserContext = _currentUserContext
            };

            _pendingChanges.Enqueue(packet);
            UIChangeOccurred?.Invoke(packet);

            SafeLogDebug($"Tracked component destroyed: {componentType} ({componentId})");
        }

        /// <summary>
        /// Track a UI component visibility change
        /// </summary>
        public void TrackVisibilityChanged(GameObject uiComponent, string componentType, bool isVisible)
        {
            if (uiComponent == null) return;

            string componentId = GetComponentId(uiComponent);
            var packet = UIChangePacket.CreateVisibilityChangedPacket(componentId, componentType, isVisible, _currentUserContext);

            _pendingChanges.Enqueue(packet);
            UIChangeOccurred?.Invoke(packet);

            SafeLogDebug($"Tracked visibility change: {componentType} ({componentId}) - {(isVisible ? "Visible" : "Hidden")}");
        }

        /// <summary>
        /// Track a UI component position/size change
        /// </summary>
        public void TrackComponentMoved(GameObject uiComponent, string componentType)
        {
            if (uiComponent == null) return;

            string componentId = GetComponentId(uiComponent);
            var rect = uiComponent.GetComponent<RectTransform>();
            Vector3 position = rect ? rect.anchoredPosition3D : uiComponent.transform.position;
            Vector2 size = rect ? rect.sizeDelta : Vector2.zero;

            var packet = UIChangePacket.CreateMovedPacket(componentId, componentType, position, size, _currentUserContext);

            _pendingChanges.Enqueue(packet);
            UIChangeOccurred?.Invoke(packet);

            SafeLogDebug($"Tracked component moved: {componentType} ({componentId})");
        }

        /// <summary>
        /// Track a UI component content change
        /// </summary>
        public void TrackContentChanged(GameObject uiComponent, string componentType, object changeData)
        {
            if (uiComponent == null) return;

            string componentId = GetComponentId(uiComponent);
            var packet = new UIChangePacket
            {
                ComponentId = componentId,
                ComponentType = componentType,
                ChangeType = UIChangeType.ContentChanged,
                ChangeData = JsonUtility.ToJson(changeData ?? new object()),
                UserContext = _currentUserContext
            };

            _pendingChanges.Enqueue(packet);
            UIChangeOccurred?.Invoke(packet);

            SafeLogDebug($"Tracked content change: {componentType} ({componentId})");
        }

        /// <summary>
        /// Track a UI interaction event
        /// </summary>
        public void TrackInteraction(GameObject uiComponent, string componentType, string interactionType, object data = null)
        {
            if (uiComponent == null) return;

            string componentId = GetComponentId(uiComponent);
            var interactionData = new
            {
                InteractionType = interactionType,
                Data = data
            };

            var packet = new UIChangePacket
            {
                ComponentId = componentId,
                ComponentType = componentType,
                ChangeType = UIChangeType.InteractionOccurred,
                ChangeData = JsonUtility.ToJson(interactionData),
                UserContext = _currentUserContext
            };

            _pendingChanges.Enqueue(packet);
            UIChangeOccurred?.Invoke(packet);

            SafeLogDebug($"Tracked interaction: {componentType} ({componentId}) - {interactionType}");
        }

        /// <summary>
        /// Get all pending UI changes
        /// </summary>
        public List<UIChangePacket> GetPendingChanges()
        {
            var changes = new List<UIChangePacket>();
            while (_pendingChanges.TryDequeue(out var change))
            {
                changes.Add(change);
            }
            return changes;
        }

        /// <summary>
        /// Clear all pending changes
        /// </summary>
        public void ClearPendingChanges()
        {
            while (_pendingChanges.TryDequeue(out _)) { }
            SafeLogDebug("Cleared all pending UI changes");
        }

        /// <summary>
        /// Get a unique identifier for a UI component
        /// </summary>
        private string GetComponentId(GameObject uiComponent)
        {
            if (_trackedComponents.TryGetValue(uiComponent, out string existingId))
            {
                return existingId;
            }

            // Generate a new ID based on hierarchy path and instance ID
            string hierarchyPath = GetHierarchyPath(uiComponent);
            string componentId = $"{hierarchyPath}_{uiComponent.GetInstanceID()}";
            return componentId;
        }

        /// <summary>
        /// Get the hierarchy path of a GameObject
        /// </summary>
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

        /// <summary>
        /// Get current user context for changes
        /// </summary>
        private string GetCurrentUserContext()
        {
            try
            {
                if (ClientManager.client != null && ClientManager.client.Self != null)
                {
                    return $"{ClientManager.client.Self.FirstName} {ClientManager.client.Self.LastName}";
                }
            }
            catch (Exception ex)
            {
                SafeLogWarning(ex, "Could not get current user context");
            }

            return "Unknown User";
        }

        /// <summary>
        /// Update user context when login state changes
        /// </summary>
        public void UpdateUserContext()
        {
            _currentUserContext = GetCurrentUserContext();
            SafeLogDebug($"Updated user context to: {_currentUserContext}");
        }

        /// <summary>
        /// Safe logging methods that handle null logger
        /// </summary>
        private void SafeLogDebug(string message)
        {
            _logger?.LogDebug(message);
        }

        private void SafeLogWarning(string message)
        {
            _logger?.LogWarning(message);
        }

        private void SafeLogWarning(Exception ex, string message)
        {
            _logger?.LogWarning(ex, message);
        }
    }
}