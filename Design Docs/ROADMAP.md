# Crystal Frost Viewer Roadmap

This document outlines a roadmap of potential improvements for the Crystal Frost viewer. It is a living document that will be updated as we research and prioritize tasks.

## Prioritized Task List

### P0: Critical Bugs
1.  **Fix Bento Mesh Rendering:** (Completed) The highest priority was to fix the rendering of Bento-enhanced avatars. This was a major functional bug that prevented a large amount of user-created content from being displayed correctly.

### P1: High-Impact Performance & User Experience
2.  **Implement Network & Rezzing Diagnostics:** (Completed) Slow "rezzing" (loading) of objects and textures is a very common problem for users. This feature provides tools to diagnose the cause.
3.  **Implement Progressive Draw Distance:** (In Progress) This will significantly improve the user experience when teleporting to new, complex regions.
    *   **Sub-task:** Create a system that automatically reduces draw distance on teleport and then smoothly increases it as the scene loads.
4.  **Optimize Asset Pipeline:** The efficiency of the asset pipeline is critical for rezzing speed and overall performance.
    *   **Sub-task:** Profile the mesh and texture decoding process to identify bottlenecks.
    *   **Sub-task:** Move asset decoding to background threads to avoid stalling the main rendering thread.
    *   **Sub-task:** Investigate more efficient texture compression formats and loading strategies.

### P2: General Performance & Features
5.  **Optimize UI Performance:** A laggy UI can make the entire viewer feel unresponsive.
    *   **Sub-task:** Profile the UI to identify any performance hotspots.
    *   **Sub-task:** Refactor the UI to follow Unity's best practices (e.g., splitting canvases, limiting raycasters).
6.  **Implement Auto-FPS Feature:** This feature, common in other viewers, helps maintain a smooth experience by dynamically adjusting settings.
    *   **Sub-task:** Design and implement a system to dynamically adjust graphics settings to maintain a target framerate.

### P3: Long-Term Architectural Improvements
7.  **Explore DOTS/ECS:** For a major performance leap, especially with many avatars, a data-oriented approach is worth investigating.
    *   **Sub-task:** Investigate the feasibility of migrating the rendering of avatars and other scene objects to Unity's Data-Oriented Technology Stack (DOTS).
