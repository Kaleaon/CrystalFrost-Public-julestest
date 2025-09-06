using UnityEngine;
using OpenMetaverse;
using TMPro;

public class ObjectInspectorUI : MonoBehaviour
{
    public GameObject ObjectInspectorPanel;
    public TMP_Text PropertiesText;

    public void ShowPanel()
    {
        ObjectInspectorPanel.SetActive(true);
    }

    public void HidePanel()
    {
        ObjectInspectorPanel.SetActive(false);
    }

    private Dictionary<UUID, string> nameCache = new Dictionary<UUID, string>();

    public void DisplayProperties(Primitive prim)
    {
        string creatorName = GetName(prim.Properties.CreatorID);
        string ownerName = GetName(prim.Properties.OwnerID);

        string text = $"Name: {prim.Properties.Name}\n";
        text += $"Description: {prim.Properties.Description}\n";
        text += $"Creator: {creatorName}\n";
        text += $"Owner: {ownerName}\n";
        PropertiesText.text = text;

        // Subscribe to name updates
        ClientManager.client.Avatars.OnUUIDNameReply += Avatars_OnUUIDNameReply;
    }

    private void Avatars_OnUUIDNameReply(object sender, UUIDNameReplyEventArgs e)
    {
        foreach (var kvp in e.Names)
        {
            nameCache[kvp.Key] = kvp.Value;
        }
    }

    string GetName(UUID id)
    {
        if (nameCache.ContainsKey(id))
        {
            return nameCache[id];
        }
        else
        {
            ClientManager.client.Avatars.RequestAvatarName(id);
            return "Loading...";
        }
    }
}
