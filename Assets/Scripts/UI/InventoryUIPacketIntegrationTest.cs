using UnityEngine;
using CrystalFrost.UI;
using Microsoft.Extensions.Logging;

namespace CrystalFrost.UI
{
    /// <summary>
    /// Test component that demonstrates the integration of inventory UI with the packet system
    /// </summary>
    public class InventoryUIPacketIntegrationTest : MonoBehaviour
    {
        private ILogger<InventoryUIPacketIntegrationTest> _logger;
        private IUIStateTracker _uiStateTracker;
        private IUIPacketSender _uiPacketSender;
        
        [Header("Test Configuration")]
        [SerializeField] private bool enableTestMode = false;
        [SerializeField] private float testInterval = 5.0f;
        
        private void Awake()
        {
            _logger = Services.GetService<ILogger<InventoryUIPacketIntegrationTest>>();
            _uiStateTracker = FindObjectOfType<UIStateTracker>();
            _uiPacketSender = Services.GetService<IUIPacketSender>();
        }
        
        private void Start()
        {
            if (enableTestMode)
            {
                _logger.LogInformation("Inventory UI Packet Integration Test started");
                
                // Subscribe to packet events for logging
                _uiPacketSender.PacketSent += OnPacketSent;
                _uiPacketSender.PacketSendFailed += OnPacketSendFailed;
                
                // Start automated testing
                InvokeRepeating(nameof(SimulateInventoryInteractions), testInterval, testInterval);
            }
        }
        
        private void OnPacketSent(UIChangePacket packet)
        {
            _logger.LogInformation($"✓ Inventory UI packet sent: {packet.ComponentType} - {packet.ChangeType} ({packet.PacketId})");
        }
        
        private void OnPacketSendFailed(UIChangePacket packet, System.Exception ex)
        {
            _logger.LogWarning($"✗ Inventory UI packet failed: {packet.ComponentType} - {packet.ChangeType} ({packet.PacketId}) - {ex.Message}");
        }
        
        /// <summary>
        /// Simulate various inventory UI interactions for testing
        /// </summary>
        private void SimulateInventoryInteractions()
        {
            _logger.LogDebug("Simulating inventory UI interactions...");
            
            // Test 1: Simulate inventory window creation
            _uiStateTracker?.TrackInteraction(gameObject, "InventoryUIController", "TestWindowCreation", new { testId = System.Guid.NewGuid().ToString() });
            
            // Test 2: Simulate inventory tree expansion
            _uiStateTracker?.TrackInteraction(gameObject, "InventoryTreeNode", "TestFolderExpanded", new { 
                folderId = System.Guid.NewGuid().ToString(),
                folderName = "Test Folder",
                depth = 1
            });
            
            // Test 3: Simulate inventory item selection
            _uiStateTracker?.TrackInteraction(gameObject, "InventoryTreeNode", "TestItemSelected", new {
                itemId = System.Guid.NewGuid().ToString(),
                itemName = "Test Item",
                itemType = "InventoryWearable"
            });
            
            // Test 4: Simulate context menu action
            _uiStateTracker?.TrackInteraction(gameObject, "InventoryContextMenu", "TestWearItem", new {
                itemId = System.Guid.NewGuid().ToString(),
                itemName = "Test Wearable"
            });
            
            // Test 5: Simulate attachment action
            _uiStateTracker?.TrackInteraction(gameObject, "InventoryManager", "TestAttachItem", new {
                itemId = System.Guid.NewGuid().ToString(),
                itemName = "Test Attachment",
                attachmentPoint = "RightHand",
                replace = false
            });
        }
        
        /// <summary>
        /// Manual test trigger (can be called from Unity context menu)
        /// </summary>
        [ContextMenu("Run Inventory UI Packet Test")]
        public void RunManualTest()
        {
            _logger.LogInformation("Running manual inventory UI packet test...");
            SimulateInventoryInteractions();
        }
        
        /// <summary>
        /// Test all inventory UI components integration
        /// </summary>
        [ContextMenu("Test All Inventory Components")]
        public void TestAllInventoryComponents()
        {
            _logger.LogInformation("Testing all inventory UI components integration...");
            
            // Test InventoryWindowUI integration
            var testWindow = new GameObject("TestInventoryWindow");
            _uiStateTracker?.TrackComponentCreated(testWindow, "InventoryWindow");
            _uiStateTracker?.TrackVisibilityChanged(testWindow, "InventoryWindow", true);
            
            // Test InventoryUIController integration
            _uiStateTracker?.TrackInteraction(gameObject, "InventoryUIController", "CreateWindowStarted", null);
            _uiStateTracker?.TrackInteraction(gameObject, "InventoryUIController", "CreateWindowCompleted", new { windowId = testWindow.GetInstanceID() });
            
            // Test InventoryUI integration
            var testPanel = new GameObject("TestInventoryPanel");
            _uiStateTracker?.TrackVisibilityChanged(testPanel, "InventoryPanel", true);
            _uiStateTracker?.TrackInteraction(testPanel, "InventoryUI", "PopulateStarted", null);
            _uiStateTracker?.TrackContentChanged(testPanel, "InventoryUI", new { action = "PopulateCompleted", entryCount = 10 });
            
            // Test InventoryManager integration
            _uiStateTracker?.TrackInteraction(gameObject, "InventoryManager", "AttachItem", new {
                itemId = System.Guid.NewGuid().ToString(),
                itemName = "Test Manager Item",
                attachmentPoint = "LeftHand",
                replace = true
            });
            
            // Clean up test objects
            DestroyImmediate(testWindow);
            DestroyImmediate(testPanel);
            
            _logger.LogInformation("All inventory UI components integration test completed");
        }
        
        private void OnDestroy()
        {
            if (_uiPacketSender != null)
            {
                _uiPacketSender.PacketSent -= OnPacketSent;
                _uiPacketSender.PacketSendFailed -= OnPacketSendFailed;
            }
            
            CancelInvoke();
        }
    }
}