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

A MonoBehaviour service that provides methods to track UI changes. Components should find this service in the scene using `FindObjectOfType<UIStateTracker>()` rather than through dependency injection.

**Key Methods:**
- `TrackComponentCreated(GameObject, string)` - Track UI component creation
- `TrackComponentDestroyed(string, string)` - Track UI component destruction
- `TrackVisibilityChanged(GameObject, string, bool)` - Track visibility changes
- `TrackComponentMoved(GameObject, string)` - Track position/size changes
- `TrackContentChanged(GameObject, string, object)` - Track content changes
- `TrackInteraction(GameObject, string, string, object)` - Track user interactions

**Note:** All calls should use null-safe operators (`?.`) since the tracker may not be present in the scene during testing or development.

### UIPacketSender

A MonoBehaviour service that handles packet transmission. Like UIStateTracker, this should be found in the scene rather than accessed through dependency injection.

**Key Methods:**
- `SendUIChangePacket(UIChangePacket)` - Send a single packet
- `SendUIChangePacketBatch(List<UIChangePacket>)` - Send multiple packets as a batch
- Automatic batching and sending at configurable intervals
- Multiple transmission methods (chat messages, logging, extensible for custom protocols)

## Integrated UI Components

The following UI components have been integrated with the packet system:

### Inventory System Integration

#### InventoryWindowUI
- **Component Creation**: Tracks when inventory windows are created
- **Tree Population**: Tracks when the inventory tree is populated with items
- **Folder Operations**: Tracks folder expansion/collapse with folder details
- **Item Selection**: Tracks when inventory items are selected with item metadata
- **Context Menu**: Tracks all context menu actions including:
  - Wear/Take Off items
  - Attach/Detach operations with attachment point selection
  - Delete operations
  - Menu opening events

#### InventoryUIController
- **Window Management**: Tracks the entire window creation process
- **Component Creation**: Tracks creation of canvas, panels, and UI elements
- **Creation Status**: Tracks start and completion of window creation

#### InventoryUI (Simple Interface)
- **Panel Visibility**: Tracks when inventory panels are shown/hidden
- **Tree Population**: Tracks inventory tree population events
- **Folder Operations**: Tracks folder expansion/collapse with depth information

#### InventoryManager
- **Outfit Management**: Tracks all outfit-related operations:
  - Replace entire outfits with item counts and IDs
  - Add items to outfits with replace settings
  - Remove items from outfits
- **Attachment Operations**: Tracks attachment/detachment with:
  - Item details (ID, name)
  - Attachment points
  - Replace settings

### Integration Benefits

1. **Complete Inventory Tracking**: All inventory interactions are captured and transmitted
2. **Detailed Context**: Each packet includes relevant metadata (item IDs, names, attachment points, etc.)
3. **User Attribution**: All actions are attributed to the current logged-in user
4. **Real-time Synchronization**: Main viewer receives immediate updates about inventory state
5. **Hierarchical Tracking**: Tree operations include depth and folder hierarchy information

### Example Inventory Packets

**Inventory Window Creation:**
```json
{
  "ComponentType": "InventoryWindow",
  "ChangeType": "Created",
  "UserContext": "John Doe",
  "Position": {"x": 0, "y": 0, "z": 0},
  "Size": {"x": 400, "y": 600}
}
```

**Folder Expansion:**
```json
{
  "ComponentType": "InventoryTreeNode",
  "ChangeType": "InteractionOccurred",
  "ChangeData": "{\"InteractionType\":\"FolderExpanded\",\"Data\":{\"folderId\":\"...\",\"folderName\":\"Clothing\",\"depth\":1}}"
}
```

**Item Attachment:**
```json
{
  "ComponentType": "InventoryManager",
  "ChangeType": "InteractionOccurred",
  "ChangeData": "{\"InteractionType\":\"AttachItem\",\"Data\":{\"itemId\":\"...\",\"itemName\":\"Hat\",\"attachmentPoint\":\"Skull\",\"replace\":false}}"
}
```

## Setup and Usage

### Scene Setup

The UI packet system requires two MonoBehaviour components to be present in the scene:

1. **UIStateTracker** - Add this component to a GameObject in your scene
2. **UIPacketSender** - Add this component to a GameObject in your scene (it will automatically find the UIStateTracker)

These components are not registered in the dependency injection container and must be found in the scene.

### Basic Integration

1. **In UI Controllers**, find the UIStateTracker component:
```csharp
private UIStateTracker _uiStateTracker;

private void Start()
{
    _uiStateTracker = FindObjectOfType<UIStateTracker>();
    if (_uiStateTracker == null)
    {
        Debug.LogWarning("UIStateTracker not found in scene. UI tracking will be disabled.");
    }
}
```

2. **Track UI Changes** (always use null-safe operators):
```csharp
// Track component creation
_uiStateTracker?.TrackComponentCreated(myPanel, "InventoryPanel");

// Track visibility changes
_uiStateTracker?.TrackVisibilityChanged(myPanel, "InventoryPanel", true);

// Track user interactions
_uiStateTracker?.TrackInteraction(myButton, "LoginButton", "Click", 
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

## Testing

### InventoryUIPacketIntegrationTest Component

A comprehensive test component (`InventoryUIPacketIntegrationTest`) has been included to verify the inventory UI integration:

#### Features:
- **Automated Testing**: Can run continuously with configurable intervals
- **Manual Testing**: Context menu options for manual test execution
- **Packet Monitoring**: Logs successful and failed packet transmissions
- **Component Coverage**: Tests all inventory UI components

#### Manual Test Options:
1. **Run Inventory UI Packet Test**: Simulates various inventory interactions
2. **Test All Inventory Components**: Comprehensive test of all integrated components

#### Test Coverage:
- Inventory window creation and management
- Tree node expansion/collapse
- Item selection and context menu actions
- Attachment/detachment operations
- Outfit management operations

### Testing Recommendations:

1. **Enable Test Mode**: Set `enableTestMode = true` in the test component for automated testing
2. **Monitor Logs**: Watch for packet transmission confirmations in Unity Console
3. **Manual Testing**: Use context menu options to trigger specific test scenarios
4. **Integration Testing**: Test with actual inventory data when connected to a grid

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