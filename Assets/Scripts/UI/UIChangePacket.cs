using System;
using UnityEngine;

namespace CrystalFrost.UI
{
    /// <summary>
    /// Represents the type of UI change that occurred
    /// </summary>
    public enum UIChangeType
    {
        Created,
        Updated,
        Destroyed,
        Moved,
        Resized,
        VisibilityChanged,
        ContentChanged,
        InteractionOccurred
    }

    /// <summary>
    /// Represents a UI state change that can be packaged and sent to the Crystal Frost main viewer
    /// </summary>
    [Serializable]
    public class UIChangePacket
    {
        /// <summary>
        /// Unique identifier for this change packet
        /// </summary>
        public string PacketId;

        /// <summary>
        /// Timestamp when the change occurred (as ticks)
        /// </summary>
        public long TimestampTicks;

        /// <summary>
        /// Unique identifier of the UI component that changed
        /// </summary>
        public string ComponentId;

        /// <summary>
        /// Name/type of the UI component (e.g., "LoginWindow", "InventoryPanel")
        /// </summary>
        public string ComponentType;

        /// <summary>
        /// Type of change that occurred
        /// </summary>
        public UIChangeType ChangeType;

        /// <summary>
        /// Position data for the UI component
        /// </summary>
        public Vector3 Position;

        /// <summary>
        /// Size data for the UI component
        /// </summary>
        public Vector2 Size;

        /// <summary>
        /// Whether the component is currently visible
        /// </summary>
        public bool IsVisible;

        /// <summary>
        /// Additional metadata about the change as JSON
        /// </summary>
        public string ChangeData;

        /// <summary>
        /// User/session context for this change
        /// </summary>
        public string UserContext;

        /// <summary>
        /// Timestamp as DateTime property
        /// </summary>
        public DateTime Timestamp
        {
            get => new DateTime(TimestampTicks);
            set => TimestampTicks = value.Ticks;
        }

        public UIChangePacket()
        {
            PacketId = System.Guid.NewGuid().ToString();
            Timestamp = DateTime.UtcNow;
            ChangeData = "{}";
        }

        /// <summary>
        /// Serializes the packet to JSON string
        /// </summary>
        public string ToJson()
        {
            return JsonUtility.ToJson(this, true);
        }

        /// <summary>
        /// Deserializes a UIChangePacket from JSON string
        /// </summary>
        public static UIChangePacket FromJson(string json)
        {
            return JsonUtility.FromJson<UIChangePacket>(json);
        }

        /// <summary>
        /// Creates a new packet for component creation
        /// </summary>
        public static UIChangePacket CreateCreatedPacket(string componentId, string componentType, Vector3 position, Vector2 size, bool isVisible, string userContext = null)
        {
            return new UIChangePacket
            {
                ComponentId = componentId,
                ComponentType = componentType,
                ChangeType = UIChangeType.Created,
                Position = position,
                Size = size,
                IsVisible = isVisible,
                UserContext = userContext ?? "Unknown"
            };
        }

        /// <summary>
        /// Creates a new packet for visibility changes
        /// </summary>
        public static UIChangePacket CreateVisibilityChangedPacket(string componentId, string componentType, bool isVisible, string userContext = null)
        {
            return new UIChangePacket
            {
                ComponentId = componentId,
                ComponentType = componentType,
                ChangeType = UIChangeType.VisibilityChanged,
                IsVisible = isVisible,
                UserContext = userContext ?? "Unknown"
            };
        }

        /// <summary>
        /// Creates a new packet for position/size changes
        /// </summary>
        public static UIChangePacket CreateMovedPacket(string componentId, string componentType, Vector3 position, Vector2 size, string userContext = null)
        {
            return new UIChangePacket
            {
                ComponentId = componentId,
                ComponentType = componentType,
                ChangeType = UIChangeType.Moved,
                Position = position,
                Size = size,
                UserContext = userContext ?? "Unknown"
            };
        }
    }

    /// <summary>
    /// Represents a batch of UI change packets
    /// </summary>
    [Serializable]
    public class UIBatchPacket
    {
        public string BatchId;
        public long TimestampTicks;
        public int PacketCount;
        public UIChangePacket[] Packets;

        public DateTime Timestamp
        {
            get => new DateTime(TimestampTicks);
            set => TimestampTicks = value.Ticks;
        }
    }
}