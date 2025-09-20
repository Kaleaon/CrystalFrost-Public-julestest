using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// An example of a player that can be controlled with a joystick.
/// </summary>
public class JoystickPlayerExample : MonoBehaviour
{
    /// <summary>
    /// The speed of the player.
    /// </summary>
    public float speed;
    /// <summary>
    /// The joystick that controls the player.
    /// </summary>
    public VariableJoystick variableJoystick;
    /// <summary>
    /// The rigidbody of the player.
    /// </summary>
    public Rigidbody rb;

    /// <summary>
    /// Called every fixed framerate frame.
    /// </summary>
    public void FixedUpdate()
    {
        Vector3 direction = Vector3.forward * variableJoystick.Vertical + Vector3.right * variableJoystick.Horizontal;
        rb.AddForce(direction * speed * Time.fixedDeltaTime, ForceMode.VelocityChange);
    }
}