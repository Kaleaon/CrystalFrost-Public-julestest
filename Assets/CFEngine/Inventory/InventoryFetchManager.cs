using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using OpenMetaverse;
using UnityEngine;
using Microsoft.Extensions.Logging;

namespace CrystalFrost.Inventory
{
    /// <summary>
    /// Background pre-fetch queue manager that manages folder fetch requests with
    /// concurrency limits, priority queueing, frame-rate aware pausing, and local store search indexing.
    /// </summary>
    public class InventoryFetchManager : MonoBehaviour
    {
        public const int MAX_CONCURRENT_FETCHES = 3;
        public const float DEFAULT_MIN_FPS = 30.0f;

        public struct FetchRequest : IComparable<FetchRequest>
        {
            public UUID FolderID;
            public FetchPriority Priority;
            public DateTime QueuedTime;

            public FetchRequest(UUID folderID, FetchPriority priority)
            {
                FolderID = folderID;
                Priority = priority;
                QueuedTime = DateTime.UtcNow;
            }

            public int CompareTo(FetchRequest other)
            {
                // Higher priority first
                int priorityCompare = other.Priority.CompareTo(Priority);
                if (priorityCompare != 0) return priorityCompare;
                // Earlier queued time first
                return QueuedTime.CompareTo(other.QueuedTime);
            }
        }

        private readonly Dictionary<UUID, InventoryFetchStatus> _folderStatuses = new();
        private readonly List<FetchRequest> _fetchQueue = new();
        private readonly HashSet<UUID> _activeFetches = new();
        private readonly object _lock = new();

        private GridClient _client;
        private ILogger<InventoryFetchManager> _logger;

        [Header("Settings")]
        public int maxConcurrentFetches = MAX_CONCURRENT_FETCHES;
        public float minTargetFPS = DEFAULT_MIN_FPS;
        public bool pauseForHeavyNetwork = false;
        public bool isManuallyPaused = false;

        public event Action<UUID, InventoryFetchStatus> OnFolderStatusChanged;
        public event Action OnSearchResultsUpdated;

        public int ActiveFetchCount
        {
            get
            {
                lock (_lock)
                {
                    return _activeFetches.Count;
                }
            }
        }

        public int QueuedFetchCount
        {
            get
            {
                lock (_lock)
                {
                    return _fetchQueue.Count;
                }
            }
        }

        public bool IsPaused
        {
            get
            {
                if (isManuallyPaused || pauseForHeavyNetwork) return true;

                // Frame rate check when running in Unity
                if (Application.isPlaying)
                {
                    float currentFPS = 1.0f / Mathf.Max(Time.unscaledDeltaTime, 0.0001f);
                    if (currentFPS < minTargetFPS)
                    {
                        return true;
                    }
                }

                return false;
            }
        }

        public void Initialize(GridClient client, ILogger<InventoryFetchManager> logger = null)
        {
            _client = client;
            _logger = logger;

            if (_client != null)
            {
                _client.Inventory.FolderUpdated += OnFolderUpdated;
            }
        }

        private void Awake()
        {
            _logger = Services.GetService<ILogger<InventoryFetchManager>>();
        }

        private void Start()
        {
            if (_client == null)
            {
                _client = ClientManager.client;
                if (_client != null)
                {
                    _client.Inventory.FolderUpdated += OnFolderUpdated;
                }
            }
        }

        private void OnDestroy()
        {
            if (_client != null)
            {
                _client.Inventory.FolderUpdated -= OnFolderUpdated;
            }
        }

        private void Update()
        {
            ProcessQueue();
        }

        /// <summary>
        /// Gets current fetch status for a given category/folder node.
        /// </summary>
        public InventoryFetchStatus GetFolderStatus(UUID folderID)
        {
            lock (_lock)
            {
                if (_folderStatuses.TryGetValue(folderID, out var status))
                {
                    return status;
                }
                return InventoryFetchStatus.Unfetched;
            }
        }

        /// <summary>
        /// Sets fetch status for a given category/folder node.
        /// </summary>
        public void SetFolderStatus(UUID folderID, InventoryFetchStatus status)
        {
            lock (_lock)
            {
                _folderStatuses[folderID] = status;
                if (status == InventoryFetchStatus.Fetched)
                {
                    _activeFetches.Remove(folderID);
                }
            }

            OnFolderStatusChanged?.Invoke(folderID, status);
        }

        /// <summary>
        /// Queues a folder request with specified priority.
        /// Returns true if folder was newly queued, false if already fetched or in queue.
        /// </summary>
        public bool QueueFolderFetch(UUID folderID, FetchPriority priority = FetchPriority.Normal)
        {
            if (folderID == UUID.Zero) return false;

            lock (_lock)
            {
                if (_folderStatuses.TryGetValue(folderID, out var status))
                {
                    if (status == InventoryFetchStatus.Fetched || status == InventoryFetchStatus.Fetching)
                    {
                        return false;
                    }
                }

                // Check if already in queue
                int existingIdx = _fetchQueue.FindIndex(r => r.FolderID == folderID);
                if (existingIdx >= 0)
                {
                    // Upgrade priority if higher
                    if (_fetchQueue[existingIdx].Priority < priority)
                    {
                        _fetchQueue[existingIdx] = new FetchRequest(folderID, priority);
                        _fetchQueue.Sort();
                    }
                    return false;
                }

                _fetchQueue.Add(new FetchRequest(folderID, priority));
                _fetchQueue.Sort();
            }

            ProcessQueue();
            return true;
        }

        /// <summary>
        /// Attempts to process pending items in the prioritized fetch queue up to MAX_CONCURRENT_FETCHES.
        /// </summary>
        public void ProcessQueue()
        {
            if (IsPaused) return;

            List<UUID> foldersToFetch = new();

            lock (_lock)
            {
                while (_activeFetches.Count + foldersToFetch.Count < maxConcurrentFetches && _fetchQueue.Count > 0)
                {
                    FetchRequest request = _fetchQueue[0];
                    _fetchQueue.RemoveAt(0);

                    // Re-check status
                    if (_folderStatuses.TryGetValue(request.FolderID, out var status) && status == InventoryFetchStatus.Fetched)
                    {
                        continue;
                    }

                    _folderStatuses[request.FolderID] = InventoryFetchStatus.Fetching;
                    _activeFetches.Add(request.FolderID);
                    foldersToFetch.Add(request.FolderID);

                    OnFolderStatusChanged?.Invoke(request.FolderID, InventoryFetchStatus.Fetching);
                }
            }

            foreach (var folderID in foldersToFetch)
            {
                ExecuteFolderFetch(folderID);
            }
        }

        private void ExecuteFolderFetch(UUID folderID)
        {
            if (_client == null || _client.Inventory == null || _client.Inventory.Store == null)
            {
                // Client not available yet, reset status to Unfetched
                SetFolderStatus(folderID, InventoryFetchStatus.Unfetched);
                return;
            }

            try
            {
                _logger?.LogDebug($"Executing folder fetch for {folderID}");
                _client.Inventory.RequestFolderContents(folderID, _client.Self.AgentID, true, true, InventorySortOrder.ByName);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, $"Failed to request folder contents for {folderID}");
                SetFolderStatus(folderID, InventoryFetchStatus.Unfetched);
            }
        }

        private void OnFolderUpdated(object sender, FolderUpdatedEventArgs e)
        {
            if (e.Success)
            {
                SetFolderStatus(e.FolderID, InventoryFetchStatus.Fetched);

                // Queue child subfolders for background pre-fetching if root or expanded
                if (_client != null && _client.Inventory != null && _client.Inventory.Store != null)
                {
                    var contents = _client.Inventory.Store.GetContents(e.FolderID);
                    foreach (var item in contents)
                    {
                        if (item is InventoryFolder subFolder)
                        {
                            if (GetFolderStatus(subFolder.UUID) == InventoryFetchStatus.Unfetched)
                            {
                                QueueFolderFetch(subFolder.UUID, FetchPriority.Low);
                            }
                        }
                    }
                }

                OnSearchResultsUpdated?.Invoke();
            }
            else
            {
                SetFolderStatus(e.FolderID, InventoryFetchStatus.Unfetched);
            }

            ProcessQueue();
        }

        /// <summary>
        /// Searches local store index across all known folders.
        /// Any unfetched subfolders encountered during the hierarchy scan are automatically queued for background retrieval.
        /// </summary>
        public List<InventoryItem> SearchInventory(string searchTerm, GridClient clientOverride = null)
        {
            var client = clientOverride ?? _client ?? ClientManager.client;
            List<InventoryItem> results = new();

            if (client == null || client.Inventory == null || client.Inventory.Store == null || client.Inventory.Store.RootFolder == null)
            {
                return results;
            }

            if (string.IsNullOrWhiteSpace(searchTerm))
            {
                return results;
            }

            string term = searchTerm.Trim().ToLowerInvariant();
            Queue<UUID> folderQueue = new();
            HashSet<UUID> visitedFolders = new();

            UUID rootID = client.Inventory.Store.RootFolder.UUID;
            folderQueue.Enqueue(rootID);
            visitedFolders.Add(rootID);

            // Fast traversal of local store
            while (folderQueue.Count > 0)
            {
                UUID currentFolderID = folderQueue.Dequeue();

                // Track folder status
                var status = GetFolderStatus(currentFolderID);

                // Get contents from local store index
                List<InventoryBase> contents = client.Inventory.Store.GetContents(currentFolderID);

                if (contents == null || contents.Count == 0)
                {
                    if (status == InventoryFetchStatus.Unfetched)
                    {
                        // Queue missing folder for high-priority background retrieval
                        QueueFolderFetch(currentFolderID, FetchPriority.High);
                    }
                    continue;
                }

                foreach (var item in contents)
                {
                    if (item is InventoryItem invItem)
                    {
                        if ((invItem.Name != null && invItem.Name.ToLowerInvariant().Contains(term)) ||
                            (invItem.Description != null && invItem.Description.ToLowerInvariant().Contains(term)))
                        {
                            results.Add(invItem);
                        }
                    }
                    else if (item is InventoryFolder subFolder)
                    {
                        if (!visitedFolders.Contains(subFolder.UUID))
                        {
                            visitedFolders.Add(subFolder.UUID);
                            folderQueue.Enqueue(subFolder.UUID);

                            if (GetFolderStatus(subFolder.UUID) == InventoryFetchStatus.Unfetched)
                            {
                                QueueFolderFetch(subFolder.UUID, FetchPriority.High);
                            }
                        }
                    }
                }
            }

            return results;
        }

        /// <summary>
        /// Recursively registers all known inventory folders into the tracking store.
        /// </summary>
        public void DiscoverFolders(GridClient clientOverride = null)
        {
            var client = clientOverride ?? _client ?? ClientManager.client;
            if (client == null || client.Inventory == null || client.Inventory.Store == null || client.Inventory.Store.RootFolder == null)
            {
                return;
            }

            Queue<UUID> folderQueue = new();
            folderQueue.Enqueue(client.Inventory.Store.RootFolder.UUID);

            while (folderQueue.Count > 0)
            {
                UUID folderID = folderQueue.Dequeue();
                var contents = client.Inventory.Store.GetContents(folderID);

                if (contents != null && contents.Count > 0)
                {
                    SetFolderStatus(folderID, InventoryFetchStatus.Fetched);
                    foreach (var item in contents)
                    {
                        if (item is InventoryFolder subFolder)
                        {
                            if (GetFolderStatus(subFolder.UUID) == InventoryFetchStatus.Unfetched)
                            {
                                folderQueue.Enqueue(subFolder.UUID);
                            }
                        }
                    }
                }
                else
                {
                    if (GetFolderStatus(folderID) == InventoryFetchStatus.Unfetched)
                    {
                        QueueFolderFetch(folderID, FetchPriority.Normal);
                    }
                }
            }
        }
    }
}
