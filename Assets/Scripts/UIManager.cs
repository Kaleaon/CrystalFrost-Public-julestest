using UnityEngine;

public class UIManager : MonoBehaviour
{
    public void TogglePanel(GameObject panel)
    {
        if (panel != null)
        {
            panel.SetActive(!panel.activeSelf);
        }
    }
}
