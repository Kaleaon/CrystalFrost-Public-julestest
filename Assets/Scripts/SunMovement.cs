using UnityEngine;

/// <summary>
/// Controls the movement of the sun to simulate a day-night cycle.
/// </summary>
public class SunMovement : MonoBehaviour
{
    /// <summary>
    /// The duration of a full day-night cycle in minutes.
    /// </summary>
    public float dayCycleInMinutes = 1.0f;

    /// <summary>
    /// The transform of the sun object.
    /// </summary>
    public Transform sun;

    private void Update()
    {
        if (sun == null)
        {
            return;
        }

        // Calculate the time of day as a value between 0 and 1
        float timeOfDay = (Time.time / (dayCycleInMinutes * 60.0f)) % 1.0f;

        // Calculate the sun's rotation based on the time of day
        float sunAngle = timeOfDay * 360.0f;

        // Apply the rotation to the sun
        sun.rotation = Quaternion.Euler(new Vector3(sunAngle, 0, 0));
    }
}
