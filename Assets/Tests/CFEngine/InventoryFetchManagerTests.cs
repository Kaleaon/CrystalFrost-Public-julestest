using System;
using System.Collections.Generic;
using System.Diagnostics;
using NUnit.Framework;
using OpenMetaverse;
using UnityEngine;
using CrystalFrost.Inventory;

namespace CrystalFrost.Tests
{
    [TestFixture]
    public class InventoryFetchManagerTests
    {
        private GameObject _managerGO;
        private InventoryFetchManager _fetchManager;

        [SetUp]
        public void Setup()
        {
            _managerGO = new GameObject("TestFetchManager");
            _fetchManager = _managerGO.AddComponent<InventoryFetchManager>();
            _fetchManager.isManuallyPaused = true; // Pause actual network dispatches during unit test setup
        }

        [TearDown]
        public void TearDown()
        {
            if (_managerGO != null)
            {
                UnityEngine.Object.DestroyImmediate(_managerGO);
            }
        }

        [Test]
        public void FolderStatusTracking_TransitionsCorrectly()
        {
            UUID folderID = UUID.Random();

            // Default status should be Unfetched
            Assert.AreEqual(InventoryFetchStatus.Unfetched, _fetchManager.GetFolderStatus(folderID));

            // Set to Fetching
            _fetchManager.SetFolderStatus(folderID, InventoryFetchStatus.Fetching);
            Assert.AreEqual(InventoryFetchStatus.Fetching, _fetchManager.GetFolderStatus(folderID));

            // Set to Fetched
            _fetchManager.SetFolderStatus(folderID, InventoryFetchStatus.Fetched);
            Assert.AreEqual(InventoryFetchStatus.Fetched, _fetchManager.GetFolderStatus(folderID));
        }

        [Test]
        public void PrioritizedQueue_OrdersByPriorityAndEnforcesConcurrencyLimit()
        {
            _fetchManager.isManuallyPaused = false;
            _fetchManager.maxConcurrentFetches = 3;

            UUID folder1 = UUID.Random();
            UUID folder2 = UUID.Random();
            UUID folder3 = UUID.Random();
            UUID folder4 = UUID.Random();

            // Queue requests with different priorities
            _fetchManager.QueueFolderFetch(folder1, FetchPriority.Low);
            _fetchManager.QueueFolderFetch(folder2, FetchPriority.High);
            _fetchManager.QueueFolderFetch(folder3, FetchPriority.Normal);
            _fetchManager.QueueFolderFetch(folder4, FetchPriority.High);

            _fetchManager.ProcessQueue();

            // Active fetch count must be capped at maxConcurrentFetches (3)
            Assert.LessOrEqual(_fetchManager.ActiveFetchCount, 3);
            Assert.AreEqual(1, _fetchManager.QueuedFetchCount);
        }

        [Test]
        public void PausePreFetch_WhenPaused_PreventsQueueProcessing()
        {
            _fetchManager.isManuallyPaused = true;

            UUID folder1 = UUID.Random();
            UUID folder2 = UUID.Random();

            _fetchManager.QueueFolderFetch(folder1, FetchPriority.High);
            _fetchManager.QueueFolderFetch(folder2, FetchPriority.Normal);

            _fetchManager.ProcessQueue();

            Assert.AreEqual(0, _fetchManager.ActiveFetchCount);
            Assert.AreEqual(2, _fetchManager.QueuedFetchCount);
        }

        [Test]
        public void SearchPerformance_ExecutesUnder50MillisecondsForLargeDataset()
        {
            // Set up mock client with root folder
            GridClient client = new GridClient();
            OpenMetaverse.Inventory store = (OpenMetaverse.Inventory)System.Runtime.CompilerServices.RuntimeHelpers.GetUninitializedObject(typeof(OpenMetaverse.Inventory));
            store.Items = new Dictionary<UUID, InventoryNode>();
            var storeField = typeof(InventoryManager).GetField("_Store", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            if (storeField != null) storeField.SetValue(client.Inventory, store);

            UUID rootID = UUID.Random();




            InventoryFolder rootFolder = new InventoryFolder(rootID)
            {
                Name = "My Inventory",
                OwnerID = client.Self.AgentID
            };
            client.Inventory.Store.RootFolder = rootFolder;
            client.Inventory.Store[rootID] = rootFolder;

            // Populate 10,000 items into store across 10 subfolders
            const int totalItems = 10000;
            const int numFolders = 10;
            int itemsPerFolder = totalItems / numFolders;

            for (int f = 0; f < numFolders; f++)
            {
                UUID folderID = UUID.Random();
                InventoryFolder folder = new InventoryFolder(folderID)
                {
                    Name = $"Folder_{f}",
                    ParentUUID = rootID,
                    OwnerID = client.Self.AgentID
                };
                client.Inventory.Store[folderID] = folder;
                _fetchManager.SetFolderStatus(folderID, InventoryFetchStatus.Fetched);

                for (int i = 0; i < itemsPerFolder; i++)
                {
                    UUID itemID = UUID.Random();
                    InventoryItem item = new InventoryItem(itemID)
                    {
                        Name = $"Item_{f}_{i}_test_search",
                        Description = $"Description for item {i}",
                        ParentUUID = folderID,
                        OwnerID = client.Self.AgentID
                    };
                    client.Inventory.Store[itemID] = item;
                }
            }

            _fetchManager.Initialize(client);

            Stopwatch stopwatch = Stopwatch.StartNew();
            List<InventoryItem> results = _fetchManager.SearchInventory("test_search", client);
            stopwatch.Stop();

            Assert.AreEqual(totalItems, results.Count);
            Assert.Less(stopwatch.ElapsedMilliseconds, 50, $"Search for 10,000 items took {stopwatch.ElapsedMilliseconds}ms, expected under 50ms");
        }
    }
}
