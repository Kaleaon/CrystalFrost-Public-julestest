# Crystal Frost UI Packet System

This document describes the new UI packet system implemented to track and transmit UI state changes to the Crystal Frost main viewer.

## Overview

The UI packet system consists of three main components:

1. **UIChangePacket** - Data structure representing UI state changes
2. **UIStateTracker** - Service that tracks UI changes across all components
3. **UIPacketSender** - Service that packages and sends UI changes to the main viewer

## Architecture

```
UI Components → UIStateTracker → UIPacketSender → Main Viewer
                     ↓
               UIChangePacket
```

## Components

### UIChangePacket

A serializable data structure that contains:
- **PacketId**: Unique identifier for the packet
- **Timestamp**: When the change occurred
- **ComponentId**: Unique identifier of the UI component
- **ComponentType**: Type/name of the component (e.g., "LoginWindow", "InventoryPanel")
- **ChangeType**: Type of change (Created, Updated, Destroyed, Moved, Resized, VisibilityChanged, ContentChanged, InteractionOccurred)
- **Position**: 3D position data
- **Size**: 2D size data
- **IsVisible**: Current visibility state
- **ChangeData**: Additional metadata as JSON
- **UserContext**: User/session information

### UIStateTracker

A MonoBehaviour service that provides methods to track UI changes:
- `TrackComponentCreated(GameObject, string)` - Track UI component creation
- `TrackComponentDestroyed(string, string)` - Track UI component destruction
- `TrackVisibilityChanged(GameObject, string, bool)` - Track visibility changes
- `TrackComponentMoved(GameObject, string)` - Track position/size changes
- `TrackContentChanged(GameObject, string, object)` - Track content changes
- `TrackInteraction(GameObject, string, string, object)` - Track user interactions

### UIPacketSender

A MonoBehaviour service that handles packet transmission:
- `SendUIChangePacket(UIChangePacket)` - Send a single packet
- `SendUIChangePacketBatch(List<UIChangePacket>)` - Send multiple packets as a batch
- Automatic batching and sending at configurable intervals
- Multiple transmission methods (chat messages, logging, extensible for custom protocols)

## Usage

### Basic Integration

1. **In UI Controllers**, get the UIStateTracker service:
```csharp
private IUIStateTracker _uiStateTracker;

private void Start()
{
    _uiStateTracker = Services.GetService<IUIStateTracker>();
}
```

2. **Track UI Changes**:
```csharp
// Track component creation
_uiStateTracker.TrackComponentCreated(myPanel, "InventoryPanel");

// Track visibility changes
_uiStateTracker.TrackVisibilityChanged(myPanel, "InventoryPanel", true);

// Track user interactions
_uiStateTracker.TrackInteraction(myButton, "LoginButton", "Click", 
    new { Username = "John.Doe" });
```

### Transmission Methods

The system supports multiple transmission methods:

1. **Chat Channel** (when connected to grid):
   - Sends packets as special chat messages with `[CF_UI_PACKET]` prefix
   - Batches use `[CF_UI_BATCH]` prefix
   - Large batches are split into multiple messages

2. **Logging** (when disconnected or for testing):
   - Logs packets with `[UI_PACKET_TO_MAIN_VIEWER]` prefix
   - Can be captured by log parsers or external tools

3. **Extensible** (future):
   - HTTP/WebSocket endpoints
   - Custom network protocols
   - File-based communication

## Configuration

The UIPacketSender has configurable settings:
- `enableAutomaticSending`: Enable/disable automatic packet transmission
- `batchSendInterval`: Interval between batch sends (default: 1 second)
- `maxBatchSize`: Maximum packets per batch (default: 50)

## Testing

Use the `UIPacketSystemTest` component to test the system:
1. Add the component to a GameObject in your scene
2. Enable `enableTestMode` in the inspector
3. The test will automatically create UI elements and track various changes
4. Check the console logs to see packet transmission

Alternatively, use the context menu options for manual testing:
- Test Create Panel
- Test Toggle Panel
- Test Move Panel
- Test Change Content
- Test Button Click
- Test Destroy Panel

## Integration with Existing UI

The system has been integrated with:
- **UIManager**: Enhanced to track panel visibility changes
- **LoginUIController**: Tracks login window state, form interactions, and content changes

To integrate with additional UI components:
1. Get the UIStateTracker service in your component
2. Call appropriate tracking methods when UI state changes
3. Use the component type strings consistently (e.g., "InventoryWindow", "ChatPanel")

## Packet Format Example

```json
{
  "PacketId": "550e8400-e29b-41d4-a716-446655440000",
  "Timestamp": "2024-01-01T12:00:00.000Z",
  "ComponentId": "Canvas/LoginWindow_12345",
  "ComponentType": "LoginWindow",
  "ChangeType": "VisibilityChanged",
  "Position": { "x": 0, "y": 0, "z": 0 },
  "Size": { "x": 400, "y": 300 },
  "IsVisible": true,
  "ChangeData": "{}",
  "UserContext": "John Doe"
}
```

## Future Enhancements

- **Filtering**: Configure which UI changes to track/send
- **Compression**: Compress packet data for large batches
- **Reliability**: Add acknowledgments and retry logic
- **Security**: Encrypt sensitive UI data
- **Analytics**: Aggregate UI usage statistics
- **Real-time**: WebSocket connections for instant transmission

## Dependencies

- Newtonsoft.Json (for JSON serialization)
- Microsoft.Extensions.Logging (for logging)
- Crystal Frost Services architecture (dependency injection)
- OpenMetaverse/LibreMetaverse (for grid communication)