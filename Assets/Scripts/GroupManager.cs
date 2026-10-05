using UnityEngine;
using OpenMetaverse;
using System.Collections.Generic;

public class GroupManager : MonoBehaviour
{
    public Dictionary<UUID, Group> Groups = new Dictionary<UUID, Group>();

    void Start()
    {
        ClientManager.client.Groups.OnGroupJoined += Groups_OnGroupJoined;
        ClientManager.client.Groups.OnGroupLeft += Groups_OnGroupLeft;
        ClientManager.client.Groups.OnCurrentGroups += Groups_OnCurrentGroups;
        ClientManager.client.Groups.OnGroupNotices += Groups_OnGroupNotices;
        RequestGroups();
    }

    void OnDestroy()
    {
        if (ClientManager.client != null && ClientManager.client.Network.Connected)
        {
            ClientManager.client.Groups.OnGroupJoined -= Groups_OnGroupJoined;
            ClientManager.client.Groups.OnGroupLeft -= Groups_OnGroupLeft;
            ClientManager.client.Groups.OnCurrentGroups -= Groups_OnCurrentGroups;
            ClientManager.client.Groups.OnGroupNotices -= Groups_OnGroupNotices;
        }
    }

    public void RequestGroups()
    {
        ClientManager.client.Groups.RequestCurrentGroups();
    }

    private void Groups_OnCurrentGroups(object sender, CurrentGroupsEventArgs e)
    {
        Groups = e.Groups;
        Debug.Log($"Received {Groups.Count} groups.");
    }

    private void Groups_OnGroupLeft(object sender, GroupLeftEventArgs e)
    {
        if (Groups.ContainsKey(e.GroupID))
        {
            Groups.Remove(e.GroupID);
        }
    }

    private void Groups_OnGroupJoined(object sender, GroupJoinedEventArgs e)
    {
        if (!Groups.ContainsKey(e.GroupID))
        {
            // We need to request the group details
            ClientManager.client.Groups.RequestGroupProfile(e.GroupID);
        }
    }

    public void RequestGroupNotices(UUID groupID)
    {
        ClientManager.client.Groups.RequestGroupNotices(groupID);
    }

    private void Groups_OnGroupNotices(object sender, GroupNoticesEventArgs e)
    {
        Debug.Log($"Received {e.Notices.Count} notices for group {e.GroupID}");
        GroupNoticesUI noticesUI = FindObjectOfType<GroupNoticesUI>();
        if (noticesUI != null)
        {
            noticesUI.DisplayNotices(e.Notices);
        }
    }
}
