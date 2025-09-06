using UnityEngine;
using OpenMetaverse;
using System.Collections.Generic;

public class ProfileManager : MonoBehaviour
{
    public ProfileUI profileUI;

    void Start()
    {
        ClientManager.client.Avatars.OnAvatarProperties += Avatars_OnAvatarProperties;
    }

    void OnDestroy()
    {
        if (ClientManager.client != null && ClientManager.client.Network.Connected)
        {
            ClientManager.client.Avatars.OnAvatarProperties -= Avatars_OnAvatarProperties;
        }
    }

    public void RequestProfile(UUID avatarID)
    {
        ClientManager.client.Avatars.RequestAvatarProperties(avatarID);
    }

    private void Avatars_OnAvatarProperties(object sender, AvatarPropertiesEventArgs e)
    {
        if (profileUI != null)
        {
            profileUI.DisplayProfile(e.Properties);
        }
    }
}
