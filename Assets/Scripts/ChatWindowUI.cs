using OpenMetaverse;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ChatWindowUI : MonoBehaviour
{
    // Start is called before the first frame update
    public GameObject chatConsole;
    public GameObject contactsConsole;
    public ContactsList contactsList;
    public RectTransform contactsRectTransform;
    void Start()
    {
        ClientManager.chatWindow = this;
        contactsList = gameObject.GetComponent<ContactsList>();
        ClientManager.client.Friends.OnFriendOnline += FriendOnline;
        ClientManager.client.Friends.OnFriendOffline += FriendOffline;
    }

    void OnDestroy()
    {
        if (ClientManager.client != null && ClientManager.client.Network.Connected)
        {
            ClientManager.client.Friends.OnFriendOnline -= FriendOnline;
            ClientManager.client.Friends.OnFriendOffline -= FriendOffline;
        }
    }

    public TMPro.TMP_Text contactButtonText;
    bool contactsMode = false;
    public void ContactsButton()
    {
        contactsMode = !contactsMode;
        if (contactsMode) contactButtonText.text = "Chat";
        else contactButtonText.text = "Contacts";

        chatConsole.SetActive(!contactsMode);
        contactsConsole.SetActive(contactsMode);
		ClientManager.soundManager.PlayUISound(new UUID("4c8c3c77-de8d-bde2-b9b8-32635e0fd4a6"));

	}

	public void SwitchToIM(UUID uuid)
	{
		contactsMode = !contactsMode;
		if (contactsMode) contactButtonText.text = "Chat";
		else contactButtonText.text = "Contacts";

		chatConsole.SetActive(!contactsMode);
		contactsConsole.SetActive(contactsMode);
		ClientManager.chat.SwitchTab(uuid);
	}

	public void PopulateContacts()
    {
        contactsList.Clear();
        ContactsList.ContactEntry contactEntry;
        List<UUID> avatarNames = new List<UUID>();
		ClientManager.client.Friends.FriendList.ForEach(delegate (FriendInfo friend)
		{
            contactEntry = contactsList.AddContact(friend.Name, friend.UUID, friend.IsOnline);
            contactEntry.button.SetActive(true);
            avatarNames.Add(friend.UUID);
		});
		ClientManager.client.Avatars.RequestAvatarNames(avatarNames);
	}

    void FriendOnline(object sender, FriendInfoEventArgs e)
    {
        contactsList.UpdateContactOnlineStatus(e.Friend.UUID, true);
    }

    void FriendOffline(object sender, FriendInfoEventArgs e)
    {
        contactsList.UpdateContactOnlineStatus(e.Friend.UUID, false);
    }
}
