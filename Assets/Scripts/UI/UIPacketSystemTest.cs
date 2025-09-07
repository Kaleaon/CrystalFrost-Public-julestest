using UnityEngine;
using UnityEngine.UI;
using CrystalFrost.UI;
using Microsoft.Extensions.Logging;
using System.Collections;

namespace CrystalFrost.UI
{
    /// <summary>
    /// Test script to demonstrate the UI packet system functionality
    /// </summary>
    public class UIPacketSystemTest : MonoBehaviour
    {
        [Header("Test Configuration")]
        [SerializeField] private bool enableTestMode = true;
        [SerializeField] private float testInterval = 5.0f;
        
        [Header("Test UI Elements")]
        [SerializeField] private GameObject testPanel;
        [SerializeField] private Button testButton;
        [SerializeField] private Text testLabel;

        private IUIStateTracker _uiStateTracker;
        private IUIPacketSender _uiPacketSender;
        private ILogger<UIPacketSystemTest> _logger;

        private void Start()
        {
            if (!enableTestMode) return;

            // Get services
            try
            {
                _uiStateTracker = FindObjectOfType<UIStateTracker>();
                _uiPacketSender = FindObjectOfType<UIPacketSender>();
                _logger = Services.GetService<ILogger<UIPacketSystemTest>>();

                _logger.LogInformation("UIPacketSystemTest initialized");

                // Subscribe to packet events
                _uiPacketSender.PacketSent += OnPacketSent;
                _uiPacketSender.PacketSendFailed += OnPacketSendFailed;

                // Start test sequence
                StartCoroutine(RunTestSequence());
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Failed to initialize UIPacketSystemTest: {ex.Message}");
            }
        }

        private IEnumerator RunTestSequence()
        {
            _logger.LogInformation("Starting UI packet system test sequence");

            // Test 1: Create a test panel
            yield return new WaitForSeconds(1f);
            CreateTestPanel();

            // Test 2: Show/hide panel
            yield return new WaitForSeconds(testInterval);
            ToggleTestPanel();

            // Test 3: Move panel
            yield return new WaitForSeconds(testInterval);
            MoveTestPanel();

            // Test 4: Change content
            yield return new WaitForSeconds(testInterval);
            ChangeTestContent();

            // Test 5: Simulate button interaction
            yield return new WaitForSeconds(testInterval);
            SimulateButtonClick();

            // Test 6: Destroy panel
            yield return new WaitForSeconds(testInterval);
            DestroyTestPanel();

            _logger.LogInformation("UI packet system test sequence completed");
        }

        private void CreateTestPanel()
        {
            if (testPanel == null)
            {
                // Create a simple test panel
                GameObject canvas = GameObject.Find("Canvas");
                if (canvas == null)
                {
                    canvas = new GameObject("TestCanvas");
                    canvas.AddComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
                    canvas.AddComponent<CanvasScaler>();
                    canvas.AddComponent<GraphicRaycaster>();
                }

                testPanel = new GameObject("TestPanel");
                testPanel.transform.SetParent(canvas.transform, false);
                
                // Add RectTransform and Image
                RectTransform rect = testPanel.AddComponent<RectTransform>();
                rect.sizeDelta = new Vector2(200, 100);
                rect.anchoredPosition = Vector2.zero;
                
                Image panelImage = testPanel.AddComponent<Image>();
                panelImage.color = new Color(0.2f, 0.5f, 0.8f, 0.8f);

                // Create test button
                GameObject buttonGO = new GameObject("TestButton");
                buttonGO.transform.SetParent(testPanel.transform, false);
                
                RectTransform buttonRect = buttonGO.AddComponent<RectTransform>();
                buttonRect.sizeDelta = new Vector2(100, 30);
                buttonRect.anchoredPosition = new Vector2(0, 20);
                
                testButton = buttonGO.AddComponent<Button>();
                buttonGO.AddComponent<Image>().color = Color.white;

                // Create test label
                GameObject labelGO = new GameObject("TestLabel");
                labelGO.transform.SetParent(testPanel.transform, false);
                
                RectTransform labelRect = labelGO.AddComponent<RectTransform>();
                labelRect.sizeDelta = new Vector2(150, 20);
                labelRect.anchoredPosition = new Vector2(0, -20);
                
                testLabel = labelGO.AddComponent<Text>();
                testLabel.text = "Test Label";
                testLabel.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                testLabel.alignment = TextAnchor.MiddleCenter;
            }

            // Track creation
            if (_uiStateTracker != null)
            {
                _uiStateTracker?.TrackComponentCreated(testPanel, "TestPanel");
                _logger.LogInformation("Created test panel and tracked creation");
            }
        }

        private void ToggleTestPanel()
        {
            if (testPanel != null && _uiStateTracker != null)
            {
                bool newVisibility = !testPanel.activeSelf;
                testPanel.SetActive(newVisibility);
                _uiStateTracker?.TrackVisibilityChanged(testPanel, "TestPanel", newVisibility);
                _logger.LogInformation($"Toggled test panel visibility to: {newVisibility}");
            }
        }

        private void MoveTestPanel()
        {
            if (testPanel != null && _uiStateTracker != null)
            {
                RectTransform rect = testPanel.GetComponent<RectTransform>();
                Vector2 newPosition = new Vector2(Random.Range(-100, 100), Random.Range(-50, 50));
                rect.anchoredPosition = newPosition;
                
                _uiStateTracker?.TrackComponentMoved(testPanel, "TestPanel");
                _logger.LogInformation($"Moved test panel to position: {newPosition}");
            }
        }

        private void ChangeTestContent()
        {
            if (testLabel != null && _uiStateTracker != null)
            {
                string newText = $"Updated at {System.DateTime.Now:HH:mm:ss}";
                testLabel.text = newText;
                
                _uiStateTracker?.TrackContentChanged(testLabel.gameObject, "TestLabel", 
                    new { NewText = newText, ChangeTime = System.DateTime.UtcNow });
                _logger.LogInformation($"Changed test label content to: {newText}");
            }
        }

        private void SimulateButtonClick()
        {
            if (testButton != null && _uiStateTracker != null)
            {
                _uiStateTracker?.TrackInteraction(testButton.gameObject, "TestButton", "Click", 
                    new { ClickTime = System.DateTime.UtcNow, TestData = "Simulated click" });
                _logger.LogInformation("Simulated button click interaction");
            }
        }

        private void DestroyTestPanel()
        {
            if (testPanel != null && _uiStateTracker != null)
            {
                string componentId = $"{GetHierarchyPath(testPanel)}_{testPanel.GetInstanceID()}";
                _uiStateTracker?.TrackComponentDestroyed(componentId, "TestPanel");
                
                Destroy(testPanel);
                testPanel = null;
                testButton = null;
                testLabel = null;
                
                _logger.LogInformation("Destroyed test panel and tracked destruction");
            }
        }

        private void OnPacketSent(UIChangePacket packet)
        {
            _logger.LogInformation($"Packet sent successfully: {packet.ComponentType} - {packet.ChangeType} ({packet.PacketId})");
        }

        private void OnPacketSendFailed(UIChangePacket packet, System.Exception exception)
        {
            _logger.LogError(exception, $"Packet send failed: {packet.ComponentType} - {packet.ChangeType} ({packet.PacketId})");
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

        private void OnDestroy()
        {
            // Unsubscribe from events
            if (_uiPacketSender != null)
            {
                _uiPacketSender.PacketSent -= OnPacketSent;
                _uiPacketSender.PacketSendFailed -= OnPacketSendFailed;
            }
        }

        // Public methods for manual testing
        [ContextMenu("Test Create Panel")]
        public void TestCreatePanel() => CreateTestPanel();

        [ContextMenu("Test Toggle Panel")]
        public void TestTogglePanel() => ToggleTestPanel();

        [ContextMenu("Test Move Panel")]
        public void TestMovePanel() => MoveTestPanel();

        [ContextMenu("Test Change Content")]
        public void TestChangeContent() => ChangeTestContent();

        [ContextMenu("Test Button Click")]
        public void TestButtonClick() => SimulateButtonClick();

        [ContextMenu("Test Destroy Panel")]
        public void TestDestroyPanel() => DestroyTestPanel();
    }
}