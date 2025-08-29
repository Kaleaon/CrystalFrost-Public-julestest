using UnityEngine;
using UnityEngine.UI;
using OpenMetaverse;
using CrystalFrost;

public class NetworkStatsDisplay : MonoBehaviour
{
    private Text statsText;
    private GridClient _client;

    void Start()
    {
        // Get the GridClient from the service locator
        _client = Services.GetService<GridClient>();

        // --- Create the UI ---
        // This is done programmatically because we can't edit scenes directly.

        // Create a Canvas GameObject
        var canvasGo = new GameObject("NetworkStatsCanvas");
        var canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvasGo.AddComponent<CanvasScaler>();
        canvasGo.AddComponent<GraphicRaycaster>();

        // Create the Text GameObject as a child of the Canvas
        var textGo = new GameObject("StatsText");
        textGo.transform.SetParent(canvasGo.transform);

        statsText = textGo.AddComponent<Text>();
        statsText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        statsText.fontSize = 14;
        statsText.color = Color.white;

        // Position the text in the top-left corner
        var rectTransform = statsText.GetComponent<RectTransform>();
        rectTransform.anchorMin = new Vector2(0, 1);
        rectTransform.anchorMax = new Vector2(0, 1);
        rectTransform.pivot = new Vector2(0, 1);
        rectTransform.anchoredPosition = new Vector2(10, -10);
        rectTransform.sizeDelta = new Vector2(400, 100);
    }

    void Update()
    {
        if (_client != null && statsText != null)
        {
            // Access the network stats from the GridClient
            var stats = _client.Network.Stats;

            if (stats != null)
            {
                // Format the string
                string statsString = $"Ping: {stats.Simulator.Ping}ms\n" +
                                     $"Packet Loss: {stats.Simulator.PacketLoss * 100:F2}%\n" +
                                     $"In: {stats.Simulator.BytesIn / 1024f:F2} KB/s\n" +
                                     $"Out: {stats.Simulator.BytesOut / 1024f:F2} KB/s\n" +
                                     $"Bandwidth: {_client.Throttle.Total:F0} bps";

                // Update the UI text
                statsText.text = statsString;
            }
            else
            {
                statsText.text = "Network stats not available.";
            }
        }
    }
}
