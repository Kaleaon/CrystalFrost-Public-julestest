using OpenMetaverse;
using System.Collections.Generic;
using UnityEngine;
using OMVVector2 = OpenMetaverse.Vector2;
using Vector2 = UnityEngine.Vector2;

public class ContactsList : MonoBehaviour
{
    // Start is called before the first frame update

    public GameObject baseContactButton;
	public GameObject console;
	
    public class ContactEntry
    {
        public string name;
        public UUID uuid;
        public GameObject button;
        public bool isOnline;
        TMPro.TMP_Text nameTag;
        public ContactEntry(string name, UUID uuid, GameObject button, int index, bool online)
        {

			this.name = name;
			this.uuid = uuid;
			this.button = button;
            this.isOnline = online;
			nameTag = button.GetComponentInChildren<TMPro.TMP_Text>();
			nameTag.text = name;
            nameTag.color = online ? Color.green : Color.gray;
            RectTransform rect = button.GetComponent<RectTransform>();
            Vector2 anchoredPos = rect.anchoredPosition;
			anchoredPos.y -= 30f * index;
			rect.anchoredPosition = anchoredPos;
			UI_IMButton b = button.GetComponent<UI_IMButton>();
			b.name = name;
			b.uuid = uuid;

		}

		public void UpdateEntry(string name, bool online)
		{
			this.name = name;
            this.isOnline = online;
			nameTag.text = name;
            nameTag.color = online ? Color.green : Color.gray;
			button.name = name;
			UI_IMButton b = button.GetComponent<UI_IMButton>();
			b.name = name;
			b.uuid = uuid;
			b.isContactButton = true;
		}
	}

    public List<ContactEntry> contactEntries = new List<ContactEntry>();
    void Awake()
    {
        contactEntries.Add(new ContactEntry("Loading...", UUID.Zero, baseContactButton, 0, false));
		console.SetActive(false);
    }

    public void Clear()
    {
        foreach (var entry in contactEntries)
        {
            if (entry.button != baseContactButton) // Don't destroy the prefab
            {
                Destroy(entry.button);
            }
        }
        contactEntries.Clear();
        baseContactButton.SetActive(false);
    }

	public void UpdateContact(string name, UUID uuid, bool online)
	{
		foreach(ContactEntry entry in contactEntries)
		{
			if (entry.uuid == uuid)
			{
				entry.UpdateEntry(name, online);
			}
		}
	}

    public void UpdateContactOnlineStatus(UUID uuid, bool online)
    {
        foreach (ContactEntry entry in contactEntries)
        {
            if (entry.uuid == uuid)
            {
                entry.UpdateEntry(entry.name, online);
            }
        }
    }
	public ContactEntry AddContact(string name, UUID uuid, bool online)
    {
        if(name == null) { name = "Loading..."; }
		contactEntries.Add(new ContactEntry($"{name}", uuid, Instantiate(baseContactButton, baseContactButton.transform.parent, true), contactEntries.Count - 1, online));
		return contactEntries[contactEntries.Count - 1];
	}
}
