using UnityEngine;
using OpenMetaverse;
using System.Collections.Generic;

public class GroupNoticesUI : MonoBehaviour
{
    public GameObject GroupNoticesPanel;
    public TMPro.TMP_Text NoticesText;

    public void ShowNotices(UUID groupID)
    {
        GroupNoticesPanel.SetActive(true);
        NoticesText.text = $"Fetching notices for group {groupID}...";
        GroupManager groupManager = FindObjectOfType<GroupManager>();
        if (groupManager != null)
        {
            groupManager.RequestGroupNotices(groupID);
        }
    }

    public void DisplayNotices(List<GroupNotice> notices)
    {
        string text = "";
        foreach (var notice in notices)
        {
            text += $"<b>{notice.Subject}</b>\n";
            text += $"<i>From: {notice.FromName}</i>\n";
            text += $"{notice.Message}\n\n";
        }
        NoticesText.text = text;
    }

    public void ClosePanel()
    {
        GroupNoticesPanel.SetActive(false);
    }
}
