using UnityEngine;

public class SceneInitializer : MonoBehaviour
{
    void Start()
    {
        // This script's purpose is to initialize scene-specific components
        // that we can't add directly to the scene file.

        // Create a new GameObject to hold our runtime-initialized components
        var servicesGo = new GameObject("SceneServices");

        // Add the NetworkStatsDisplay component to the GameObject.
        // This will trigger its Start() method, which creates the UI.
        servicesGo.AddComponent<NetworkStatsDisplay>();
    }
}
