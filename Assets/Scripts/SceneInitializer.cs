using UnityEngine;

public class SceneInitializer : MonoBehaviour
{
    void Start()
    {
        // This script's purpose is to initialize scene-specific components
        // that we can't add directly to the scene file.

        // Create a new GameObject to hold the NetworkStatsDisplay script
        var statsDisplayGo = new GameObject("NetworkStatsDisplayObject");

        // Add the NetworkStatsDisplay component to the GameObject.
        // This will trigger its Start() method, which creates the UI.
        statsDisplayGo.AddComponent<NetworkStatsDisplay>();

        // Add the ProgressiveDrawDistance component to the GameObject.
        statsDisplayGo.AddComponent<ProgressiveDrawDistance>();
    }
}
