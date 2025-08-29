# Comparative Analysis: Network & Rezzing Diagnostics

This document analyzes the network and rezzing diagnostics tools in popular Second Life viewers to inform the design of a similar feature in Crystal Frost.

## Common Features in Other Viewers (Firestorm, Official Viewer)

The standard tool for network diagnostics in Second Life viewers is the "Statistics Bar", typically toggled with Ctrl-Shift-1. It displays a wealth of information, but the most important statistics for diagnosing lag and rezzing issues are:

*   **Bandwidth:** The rate of data being received from the simulator, primarily over UDP. This is a key indicator of how much data the viewer is processing.
*   **Packet Loss:** The percentage of UDP packets that are lost in transit. A high packet loss is a strong indicator of network problems.
*   **Ping Sim:** The round-trip latency to the current simulator. High ping values lead to a less responsive experience.
*   **FPS (Frames Per Second):** The client's rendering performance.
*   **Simulator Timings:** A breakdown of how the simulator is spending its time (e.g., Physics, Scripts, Agent Updates). This is crucial for distinguishing between client-side lag (low FPS) and server-side lag (high simulator timings).

## Crystal Frost Implementation Plan

Based on this analysis, the Crystal Frost implementation should aim to provide a similar set of core statistics.

### Version 1 (Current Task)

The initial implementation will focus on the most critical network statistics:
*   **Ping:** The round-trip time to the simulator.
*   **Packet Loss:** The percentage of lost packets.
*   **Bytes In / Out:** The rate of data being sent and received.
*   **Bandwidth:** The user's configured maximum bandwidth setting.

This information will be displayed in a simple text overlay on the screen.

### Future Improvements (from Roadmap)

*   **Graphical Display:** In the future, we can improve the UI to include graphs and color-coding to make the information easier to read at a glance.
*   **Simulator Timings:** We should investigate how to get the detailed simulator timings from `LibreMetaverse` and add them to the display.
*   **Full Statistics Bar:** The long-term goal is to have a comprehensive statistics bar that is on par with Firestorm and the official viewer.

## Conclusion

By implementing a network statistics display with these key metrics, we will provide users with a powerful tool for understanding and diagnosing lag and rezzing issues. The proposed "Version 1" is a solid first step that delivers the most critical information in a simple and clear way.
