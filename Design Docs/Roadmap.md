# Crystal Frost Development Roadmap

This document outlines the planned features for Crystal Frost, prioritized based on core functionality and user experience. The features are largely inspired by the capabilities of the Firestorm viewer, adapted for the Unity engine and the `libremetaverse` backend.

## High Priority

These features are essential for a basic, usable virtual world experience.

- **[To Be Implemented] 3D World Navigation & Camera**
  - **Description**: Basic avatar movement (walking, running, flying, jumping) and camera controls (mouselook, orbit, follow-cam).
  - **Status**: Not Started.
  - **Notes**: This will be the first major implementation task.

- **Communication UI**
  - **Description**: UI for local chat, instant messages (IMs), and group chat. Includes tabbed conversations and chat history.
  - **Status**: Not Started.
  - **Notes**: `libremetaverse` handles the underlying chat and IM events. The focus is on the Unity UI implementation.

- **Inventory Management**
  - **Description**: Basic UI for browsing the inventory folder structure. Allow for wearing/removing clothing and attachments.
  - **Status**: Not Started.
  - **Notes**: `libremetaverse` provides inventory data. The main work is creating a recursive UI to display the folder hierarchy and handling "attach" and "detach" commands.

- **Friends List & Presence**
  - **Description**: A window to display the user's friends list, their online status, and their location.
  - **Status**: Not Started.
  - **Notes**: `libremetaverse` handles friend list fetching and presence updates. The UI needs to display this information and provide options for interaction (e.g., "Start IM").

## Medium Priority

These features enhance the user experience and add more complex interactions.

- **Avatar Customization (WIP)**
  - **Description**: A simple interface for changing the avatar's worn items. This is a more advanced version of the basic inventory "wear" functionality.
  - **Status**: Not Started.
  - **Notes**: Will build upon the inventory system. A simple "outfit" or "worn items" panel is the initial goal. Full appearance editing is a long-term goal.

- **Teleportation**
  - **Description**: Ability to teleport to other locations via landmarks in inventory or by using world map coordinates.
  - **Status**: Not Started.
  - **Notes**: `libremetaverse` provides the `client.Self.Teleport()` function. The UI needs to expose this for landmarks and map clicks.

- **World Map & Minimap**
  - **Description**: A basic world map showing parcel boundaries and avatar locations, and a minimap for local navigation.
  - **Status**: Not Started.
  - **Notes**: This is a significant undertaking. `libremetaverse` provides map tile data. The Unity side will need to render this data and handle user interaction.

- **RLV (Restrained Love Viewer) API Basics**
  - **Description**: Implement basic RLV command handling, such as `@version`, `@getstatus`, and simple restrictions like `@chat` or `@detach`.
  - **Status**: Not Started.
  - **Notes**: `libremetaverse` has an RLV module. The work involves routing RLV commands from the chat window to the library and enforcing the restrictions on the Unity client side.

## Low Priority

These are "nice-to-have" features that can be implemented after the core experience is solid.

- **LSL Preprocessor & Scripting Window**
  - **Description**: An in-viewer editor for LSL scripts with support for Firestorm's preprocessor extensions (e.g., `switch` statements).
  - **Status**: Not Started.
  - **Notes**: The C# implementation of the LSL preprocessor's `switch` statement is already complete from a previous task, but it needs to be integrated into a proper UI window.

- **Search Functionality**
  - **Description**: UI for searching people, places, and groups.
  - **Status**: Not Started.
  - **Notes**: `libremetaverse` provides the necessary search event handlers. This is primarily a UI task.

- **Advanced Building & Editing Tools**
  - **Description**: Tools for creating and manipulating prims in-world.
  - **Status**: Not Started.
  - **Notes**: This is a very large feature set. `libremetaverse` handles the object manipulation commands. The UI and in-world gizmos are the main challenge.

## Frost Light (Advanced Rendering)

This section is dedicated to features that leverage Unity's advanced rendering capabilities, going beyond the standard feature set.

- **[Not Started] Screen Space Global Illumination (SSGI)**
  - **Description**: Implement SSGI for more realistic lighting and contact shadows.
  - **Notes**: Requires configuration of Unity's rendering pipeline.

- **[Not Started] Advanced Water Shaders**
  - **Description**: Integrate a high-quality water system for realistic waves, reflections, and refractions.
  - **Notes**: May involve third-party assets or a custom shader implementation.

- **[Not Started] Volumetric Lighting & Fog**
  - **Description**: Use volumetric effects to create more atmospheric and immersive environments.
  - **Notes**: Requires configuration of Unity's rendering pipeline.
