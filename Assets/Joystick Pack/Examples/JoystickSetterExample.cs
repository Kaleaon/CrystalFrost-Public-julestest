using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// An example of how to set the properties of a joystick.
/// </summary>
public class JoystickSetterExample : MonoBehaviour
{
    /// <summary>
    /// The joystick to modify.
    /// </summary>
    public VariableJoystick variableJoystick;
    /// <summary>
    /// The text that displays the current value of the joystick.
    /// </summary>
    public Text valueText;
    /// <summary>
    /// The background image of the joystick.
    /// </summary>
    public Image background;
    /// <summary>
    /// The sprites for the different axis options.
    /// </summary>
    public Sprite[] axisSprites;

    /// <summary>
    /// Called when the mode of the joystick is changed.
    /// </summary>
    /// <param name="index">The index of the new mode.</param>
    public void ModeChanged(int index)
    {
        switch(index)
        {
            case 0:
                variableJoystick.SetMode(JoystickType.Fixed);
                break;
            case 1:
                variableJoystick.SetMode(JoystickType.Floating);
                break;
            case 2:
                variableJoystick.SetMode(JoystickType.Dynamic);
                break;
            default:
                break;
        }     
    }

    /// <summary>
    /// Called when the axis options of the joystick are changed.
    /// </summary>
    /// <param name="index">The index of the new axis options.</param>
    public void AxisChanged(int index)
    {
        switch (index)
        {
            case 0:
                variableJoystick.AxisOptions = AxisOptions.Both;
                background.sprite = axisSprites[index];
                break;
            case 1:
                variableJoystick.AxisOptions = AxisOptions.Horizontal;
                background.sprite = axisSprites[index];
                break;
            case 2:
                variableJoystick.AxisOptions = AxisOptions.Vertical;
                background.sprite = axisSprites[index];
                break;
            default:
                break;
        }
    }

    /// <summary>
    /// Called when the snap X property of the joystick is changed.
    /// </summary>
    /// <param name="value">The new value of the snap X property.</param>
    public void SnapX(bool value)
    {
        variableJoystick.SnapX = value;
    }

    /// <summary>
    /// Called when the snap Y property of the joystick is changed.
    /// </summary>
    /// <param name="value">The new value of the snap Y property.</param>
    public void SnapY(bool value)
    {
        variableJoystick.SnapY = value;
    }

    private void Update()
    {
        valueText.text = "Current Value: " + variableJoystick.Direction;
    }
}