using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using Microsoft.Extensions.Logging;
using OpenMetaverse;
using System.Text;

namespace CrystalFrost.UI
{
    /// <summary>
    /// Interface for sending UI change packets to the Crystal Frost main viewer
    /// </summary>
    public interface IUIPacketSender
    {
        /// <summary>
        /// Send a single UI change packet to the main viewer
        /// </summary>
        Task<bool> SendUIChangePacket(UIChangePacket packet);

        /// <summary>
        /// Send multiple UI change packets as a batch to the main viewer
        /// </summary>
        Task<bool> SendUIChangePacketBatch(List<UIChangePacket> packets);

        /// <summary>
        /// Event fired when a packet is successfully sent
        /// </summary>
        event Action<UIChangePacket> PacketSent;

        /// <summary>
        /// Event fired when a packet fails to send
        /// </summary>
        event Action<UIChangePacket, Exception> PacketSendFailed;
    }

    /// <summary>
    /// Service responsible for packaging and sending UI changes to the Crystal Frost main viewer
    /// </summary>
    public class UIPacketSender : MonoBehaviour, IUIPacketSender
    {
        private ILogger<UIPacketSender> _logger;
        private IUIStateTracker _uiStateTracker;

        [Header("Configuration")]
        [SerializeField] private bool enableAutomaticSending = true;
        [SerializeField] private float batchSendInterval = 1.0f; // Send batches every second
        [SerializeField] private int maxBatchSize = 50;

        /// <summary>
        /// Event fired when a packet is successfully sent
        /// </summary>
        public event Action<UIChangePacket> PacketSent;

        /// <summary>
        /// Event fired when a packet fails to send
        /// </summary>
        public event Action<UIChangePacket, Exception> PacketSendFailed;

        private void Awake()
        {
            try
            {
                _logger = Services.GetService<ILogger<UIPacketSender>>();
                _logger.LogInformation("UIPacketSender initialized");
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"Could not get logger service for UIPacketSender: {ex.Message}");
                // Continue without logger - will cause NullReference warnings but functionality will work
            }
        }

        private void Start()
        {
            // Find the UI state tracker
            _uiStateTracker = FindObjectOfType<UIStateTracker>();
            if (_uiStateTracker == null)
            {
                SafeLogWarning("UIStateTracker not found. Creating one.");
                var trackerGO = new GameObject("UIStateTracker");
                _uiStateTracker = trackerGO.AddComponent<UIStateTracker>();
            }

            // Subscribe to UI changes if automatic sending is enabled
            if (enableAutomaticSending)
            {
                StartAutomaticSending();
            }
        }

        /// <summary>
        /// Safe logging methods that handle null logger
        /// </summary>
        private void SafeLogDebug(string message)
        {
            _logger?.LogDebug(message);
        }

        private void SafeLogInformation(string message)
        {
            _logger?.LogInformation(message);
        }

        private void SafeLogWarning(string message)
        {
            _logger?.LogWarning(message);
        }

        private void SafeLogError(Exception ex, string message)
        {
            _logger?.LogError(ex, message);
        }

        /// <summary>
        /// Start automatic sending of UI change packets
        /// </summary>
        private void StartAutomaticSending()
        {
            StartCoroutine(SendPendingChangesBatchCoroutine());
            SafeLogInformation($"Started automatic UI packet sending with interval: {batchSendInterval}s");
        }

        /// <summary>
        /// Send pending changes as a batch (called automatically if enabled)
        /// </summary>
        private IEnumerator SendPendingChangesBatchCoroutine()
        {
            while (true)
            {
                try
                {
                    var pendingChanges = _uiStateTracker.GetPendingChanges();
                    if (pendingChanges.Count > 0)
                    {
                        // Limit batch size
                        if (pendingChanges.Count > maxBatchSize)
                        {
                            pendingChanges = pendingChanges.GetRange(0, maxBatchSize);
                            SafeLogWarning($"UI change batch size limited to {maxBatchSize}. Some changes will be sent in the next batch.");
                        }

                        var sendTask = SendUIChangePacketBatch(pendingChanges);
                        while (!sendTask.IsCompleted)
                        {
                            yield return null;
                        }
                        // Optionally handle exceptions from the task
                        if (sendTask.IsFaulted)
                        {
                            SafeLogError(sendTask.Exception, "Error sending pending UI changes batch");
                        }
                    }
                }
                catch (Exception ex)
                {
                    SafeLogError(ex, "Error sending pending UI changes batch");
                }
                yield return new WaitForSeconds(batchSendInterval);
            }
        }

        /// <summary>
        /// Send a single UI change packet to the main viewer
        /// </summary>
        public async Task<bool> SendUIChangePacket(UIChangePacket packet)
        {
            try
            {
                SafeLogDebug($"Sending UI change packet: {packet.ComponentType} - {packet.ChangeType}");

                // Serialize the packet to JSON
                string jsonData = packet.ToJson();
                
                // Send via the appropriate channel (this is where you'd integrate with the actual communication system)
                bool success = await SendToMainViewer(jsonData, packet);

                if (success)
                {
                    PacketSent?.Invoke(packet);
                    SafeLogDebug($"Successfully sent UI change packet: {packet.PacketId}");
                }
                else
                {
                    var exception = new Exception("Failed to send packet to main viewer");
                    PacketSendFailed?.Invoke(packet, exception);
                    SafeLogWarning($"Failed to send UI change packet: {packet.PacketId}");
                }

                return success;
            }
            catch (Exception ex)
            {
                PacketSendFailed?.Invoke(packet, ex);
                SafeLogError(ex, $"Exception sending UI change packet: {packet.PacketId}");
                return false;
            }
        }

        /// <summary>
        /// Send multiple UI change packets as a batch to the main viewer
        /// </summary>
        public async Task<bool> SendUIChangePacketBatch(List<UIChangePacket> packets)
        {
            if (packets == null || packets.Count == 0)
            {
                return true; // Nothing to send
            }

            try
            {
                SafeLogDebug($"Sending UI change packet batch with {packets.Count} packets");

                // Create a batch packet container
                var batchPacket = new UIBatchPacket
                {
                    BatchId = System.Guid.NewGuid().ToString(),
                    TimestampTicks = DateTime.UtcNow.Ticks,
                    PacketCount = packets.Count,
                    Packets = packets.ToArray()
                };

                string jsonData = JsonUtility.ToJson(batchPacket, true);
                
                // Send the batch
                bool success = await SendBatchToMainViewer(jsonData, packets);

                if (success)
                {
                    // Fire events for each packet in the batch
                    foreach (var packet in packets)
                    {
                        PacketSent?.Invoke(packet);
                    }
                    SafeLogInformation($"Successfully sent UI change packet batch with {packets.Count} packets");
                }
                else
                {
                    var exception = new Exception("Failed to send batch to main viewer");
                    foreach (var packet in packets)
                    {
                        PacketSendFailed?.Invoke(packet, exception);
                    }
                    SafeLogWarning($"Failed to send UI change packet batch with {packets.Count} packets");
                }

                return success;
            }
            catch (Exception ex)
            {
                foreach (var packet in packets)
                {
                    PacketSendFailed?.Invoke(packet, ex);
                }
                SafeLogError(ex, $"Exception sending UI change packet batch with {packets.Count} packets");
                return false;
            }
        }

        /// <summary>
        /// Send data to the main viewer (implement actual communication here)
        /// </summary>
        private async Task<bool> SendToMainViewer(string jsonData, UIChangePacket packet)
        {
            try
            {
                // IMPLEMENTATION NOTE: This is where you would integrate with the actual
                // Crystal Frost main viewer communication system. Options include:
                // 1. HTTP/WebSocket to a main viewer service
                // 2. Grid client instant messages with special formatting
                // 3. Custom network protocol
                // 4. File-based communication for local testing

                // For now, we'll simulate sending via grid client chat or instant message
                if (ClientManager.client != null && ClientManager.client.Network.Connected)
                {
                    // Option 1: Send as a special chat message that the main viewer can parse
                    await SendViaChat(jsonData, packet);
                    return true;
                }
                else
                {
                    // Option 2: Log for file-based testing/debugging
                    await SendViaLogging(jsonData, packet);
                    return true;
                }
            }
            catch (Exception ex)
            {
                SafeLogError(ex, "Error in SendToMainViewer");
                return false;
            }
        }

        /// <summary>
        /// Send batch data to the main viewer
        /// </summary>
        private async Task<bool> SendBatchToMainViewer(string jsonData, List<UIChangePacket> packets)
        {
            try
            {
                if (ClientManager.client != null && ClientManager.client.Network.Connected)
                {
                    await SendBatchViaChat(jsonData, packets);
                    return true;
                }
                else
                {
                    await SendBatchViaLogging(jsonData, packets);
                    return true;
                }
            }
            catch (Exception ex)
            {
                SafeLogError(ex, "Error in SendBatchToMainViewer");
                return false;
            }
        }

        /// <summary>
        /// Send UI packet via chat channel (for connected grid clients)
        /// </summary>
        private async Task SendViaChat(string jsonData, UIChangePacket packet)
        {
            await Task.Run(() =>
            {
                try
                {
                    // Send on a special channel that the main viewer monitors
                    string message = $"[CF_UI_PACKET] {jsonData}";
                    ClientManager.client.Self.Chat(message, 0, ChatType.Whisper);
                    SafeLogDebug($"Sent UI packet via chat: {packet.PacketId}");
                }
                catch (Exception ex)
                {
                    SafeLogError(ex, "Error sending UI packet via chat");
                    throw;
                }
            });
        }

        /// <summary>
        /// Send UI packet batch via chat channel
        /// </summary>
        private async Task SendBatchViaChat(string jsonData, List<UIChangePacket> packets)
        {
            await Task.Run(() =>
            {
                try
                {
                    // For large batches, split into smaller messages if needed
                    byte[] dataBytes = Encoding.UTF8.GetBytes(jsonData);
                    if (dataBytes.Length > 1000) // Chat message size limit consideration
                    {
                        // Send batch header
                        string batchId = Guid.NewGuid().ToString();
                        ClientManager.client.Self.Chat($"[CF_UI_BATCH_START] {batchId} {packets.Count}", 0, ChatType.Whisper);
                        
                        // Send individual packets
                        foreach (var packet in packets)
                        {
                            string singlePacketJson = packet.ToJson();
                            ClientManager.client.Self.Chat($"[CF_UI_BATCH_ITEM] {batchId} {singlePacketJson}", 0, ChatType.Whisper);
                        }
                        
                        // Send batch end
                        ClientManager.client.Self.Chat($"[CF_UI_BATCH_END] {batchId}", 0, ChatType.Whisper);
                    }
                    else
                    {
                        string message = $"[CF_UI_BATCH] {jsonData}";
                        ClientManager.client.Self.Chat(message, 0, ChatType.Whisper);
                    }
                    
                    SafeLogDebug($"Sent UI packet batch via chat with {packets.Count} packets");
                }
                catch (Exception ex)
                {
                    SafeLogError(ex, "Error sending UI packet batch via chat");
                    throw;
                }
            });
        }

        /// <summary>
        /// Send UI packet via logging (for disconnected or testing scenarios)
        /// </summary>
        private async Task SendViaLogging(string jsonData, UIChangePacket packet)
        {
            await Task.Run(() =>
            {
                SafeLogInformation($"[UI_PACKET_TO_MAIN_VIEWER] {jsonData}");
            });
        }

        /// <summary>
        /// Send UI packet batch via logging
        /// </summary>
        private async Task SendBatchViaLogging(string jsonData, List<UIChangePacket> packets)
        {
            await Task.Run(() =>
            {
                SafeLogInformation($"[UI_BATCH_TO_MAIN_VIEWER] {jsonData}");
            });
        }

        private void OnDestroy()
        {
            CancelInvoke();
        }
    }
}