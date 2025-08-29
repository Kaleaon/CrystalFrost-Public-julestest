using UnityEngine;
using OpenMetaverse;
using CrystalFrost;
using System.Collections;

public class ProgressiveDrawDistance : MonoBehaviour
{
    private GridClient _client;

    // Configuration
    public float TargetDrawDistance = 128.0f;
    public float MinDrawDistance = 64.0f;
    public float RampUpTime = 10.0f;

    private Coroutine _rampUpCoroutine;

    void Start()
    {
        _client = Services.GetService<GridClient>();
        if (_client != null)
        {
            _client.Self.TeleportProgress += Self_TeleportProgress;
            Debug.Log("[ProgressiveDrawDistance] Initialized and subscribed to teleport events.");
        }
        else
        {
            Debug.LogError("[ProgressiveDrawDistance] Could not get GridClient service.");
        }
    }

    void OnDestroy()
    {
        if (_client != null)
        {
            _client.Self.TeleportProgress -= Self_TeleportProgress;
        }
    }

    private void Self_TeleportProgress(object sender, TeleportEventArgs e)
    {
        if (e.Status == TeleportStatus.Start)
        {
            Debug.Log($"[ProgressiveDrawDistance] Teleport started. Setting draw distance to {MinDrawDistance}m.");
            SetDrawDistance(MinDrawDistance);

            if (_rampUpCoroutine != null)
            {
                StopCoroutine(_rampUpCoroutine);
                _rampUpCoroutine = null;
            }
        }
        else if (e.Status == TeleportStatus.Finished)
        {
            Debug.Log("[ProgressiveDrawDistance] Teleport finished. Starting draw distance ramp up.");
            _rampUpCoroutine = StartCoroutine(RampUpDrawDistance());
        }
        else if (e.Status == TeleportStatus.Failed || e.Status == TeleportStatus.Cancelled)
        {
             Debug.Log("[ProgressiveDrawDistance] Teleport failed or was cancelled. Ramping up draw distance immediately.");
             _rampUpCoroutine = StartCoroutine(RampUpDrawDistance());
        }
    }

    private IEnumerator RampUpDrawDistance()
    {
        float elapsedTime = 0;
        float startDrawDistance = GetCurrentDrawDistance();

        while (elapsedTime < RampUpTime)
        {
            elapsedTime += Time.deltaTime;
            float newDrawDistance = Mathf.Lerp(startDrawDistance, TargetDrawDistance, elapsedTime / RampUpTime);
            SetDrawDistance(newDrawDistance);
            Debug.Log($"[ProgressiveDrawDistance] Ramping up... Current: {newDrawDistance:F1}m, Target: {TargetDrawDistance}m");
            yield return null;
        }

        // Ensure the final draw distance is set exactly to the target
        SetDrawDistance(TargetDrawDistance);
        Debug.Log($"[ProgressiveDrawDistance] Ramp up complete. Draw distance set to {TargetDrawDistance}m.");
        _rampUpCoroutine = null;
    }

    private void SetDrawDistance(float distance)
    {
        if (_client != null)
        {
            // Set the draw distance in LibreMetaverse
            // NOTE: We are assuming the property is named 'Far'. This may need to be adjusted.
            _client.Self.Movement.Far = distance;
        }

        // Set the far clip plane on the main camera
        if (Camera.main != null)
        {
            Camera.main.farClipPlane = distance;
        }
    }

    private float GetCurrentDrawDistance()
    {
        if (Camera.main != null)
        {
            return Camera.main.farClipPlane;
        }
        // Fallback to the LibreMetaverse setting if the camera is not available
        return _client?.Self.Movement.Far ?? MinDrawDistance;
    }
}
