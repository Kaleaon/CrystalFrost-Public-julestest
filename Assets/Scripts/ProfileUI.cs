using UnityEngine;
using UnityEngine.UI;
using OpenMetaverse;
using TMPro;

public class ProfileUI : MonoBehaviour
{
    public GameObject ProfilePanel;
    public Image ProfilePicture;
    public TMP_Text ProfileText;

    public void ShowPanel()
    {
        ProfilePanel.SetActive(true);
    }

    public void HidePanel()
    {
        ProfilePanel.SetActive(false);
    }

    public void DisplayProfile(Avatar.AvatarProperties properties)
    {
        ProfileText.text = $"Name: {properties.Name}\n";
        ProfileText.text += $"Born: {properties.Born}\n";
        ProfileText.text += $"About: {properties.AboutText}\n";

        // Handle profile picture
        if (properties.ProfileImage != UUID.Zero)
        {
            FetchProfilePicture(properties.ProfileImage);
        }
    }

    void FetchProfilePicture(UUID imageID)
    {
        ClientManager.texturePipeline.RequestTexture(imageID, (texture) =>
        {
            if (texture != null)
            {
                ProfilePicture.sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f));
            }
        });
    }
}
