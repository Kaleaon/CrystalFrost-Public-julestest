using UnityEngine;
using UnityEngine.UI;
using System.IO;
using OpenMetaverse;
using OpenMetaverse.Imaging;

public class SnapshotPanel : MonoBehaviour
{
    [Header("UI Elements")]
    public Image PreviewImage;
    public Toggle InterfaceToggle;
    public Toggle LBalanceToggle;
    public Toggle HUDsToggle;
    public GameObject[] mainUIElements;
    public GameObject[] hudElements;
    public GameObject lBalanceElement;
    public Button RefreshButton;
    public Button SaveToDiskButton;
    public Button SaveToInventoryButton;
    public TMPro.TMP_Text StatusText;

    private Texture2D lastSnapshot;
    private Canvas canvas;

    void Start()
    {
        canvas = GetComponent<Canvas>();
        if (canvas == null)
        {
            // If the canvas is not on this object, maybe it's on a parent.
            // For this specific use case, we'll assume it's on the same GameObject.
            // A more robust solution might search upwards in the hierarchy.
            canvas = gameObject.AddComponent<Canvas>();
        }

        RefreshButton.onClick.AddListener(TakeSnapshot);
        SaveToDiskButton.onClick.AddListener(SaveSnapshotToDisk);

        SaveToInventoryButton.onClick.AddListener(SaveSnapshotToInventory);


        // Initial snapshot
        TakeSnapshot();
    }

    public void TakeSnapshot()
    {
        if (StatusText != null) StatusText.text = "";
        // This is a coroutine, so we need to start it
        StartCoroutine(CaptureScreenshot());
    }

    private System.Collections.IEnumerator CaptureScreenshot()
    {
        // Hide UI elements based on toggles before taking screenshot
        bool interfaceWasActive = InterfaceToggle.isOn;
        bool lBalanceWasActive = LBalanceToggle.isOn;
        bool hudsWasActive = HUDsToggle.isOn;

        // For now, we assume these toggles control the visibility of GameObjects.
        // The actual logic to hide/show these elements will be more complex and will be implemented later.
        // Hide UI elements based on toggles
        SetUIActive(mainUIElements, !InterfaceToggle.isOn);
        SetUIActive(hudElements, !HUDsToggle.isOn);
        if (lBalanceElement != null) lBalanceElement.SetActive(!LBalanceToggle.isOn);

        if(canvas != null) canvas.enabled = false;

        // Wait for end of frame so all rendering is complete
        yield return new WaitForEndOfFrame();

        lastSnapshot = ScreenCapture.CaptureScreenshotAsTexture();

        if(canvas != null) canvas.enabled = true;

        // Show UI elements again
        SetUIActive(mainUIElements, true);
        SetUIActive(hudElements, true);
        if (lBalanceElement != null) lBalanceElement.SetActive(true);

        RefreshPreview();
    }

    void SetUIActive(GameObject[] elements, bool active)
    {
        foreach (var element in elements)
        {
            if (element != null)
            {
                element.SetActive(active);
            }
        }
    }


    void RefreshPreview()
    {
        if (lastSnapshot != null)
        {
            //Create a new sprite from the texture
            Sprite snapshotSprite = Sprite.Create(lastSnapshot, new Rect(0, 0, lastSnapshot.width, lastSnapshot.height), new Vector2(0.5f, 0.5f));

            //Apply the sprite to the preview image
            PreviewImage.sprite = snapshotSprite;
        }
    }

    void SaveSnapshotToDisk()
    {
        if (lastSnapshot != null)
        {
            if (StatusText != null) StatusText.text = "Saving to disk...";
            byte[] bytes = lastSnapshot.EncodeToPNG();
            string path = Path.Combine(Application.persistentDataPath, $"snapshot_{System.DateTime.Now:yyyy-MM-dd_HH-mm-ss}.png");
            File.WriteAllBytes(path, bytes);
            if (StatusText != null) StatusText.text = $"Saved to {path}";
            Debug.Log($"Snapshot saved to {path}");
        }
        else
        {
            if (StatusText != null) StatusText.text = "No snapshot to save.";
            Debug.LogWarning("No snapshot to save.");
        }
    }

    void SaveSnapshotToInventory()
    {
        if (lastSnapshot != null)
        {
            if (StatusText != null) StatusText.text = "Encoding texture...";
            byte[] jpeg2000Data;
            try
            {
                jpeg2000Data = OpenJPEG.EncodeFromImage(lastSnapshot, true);
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Failed to encode snapshot to JPEG2000: {e.Message}");
                if (StatusText != null) StatusText.text = "Error encoding texture.";
                return;
            }

            if (StatusText != null) StatusText.text = "Uploading image...";
            UUID photoAlbumID = ClientManager.client.Inventory.FindFolderForType(FolderType.PhotoAlbum);
            string name = $"Snapshot {System.DateTime.Now:yyyy-MM-dd HH-mm-ss}";

            ClientManager.client.Inventory.RequestUploadImage(jpeg2000Data, name, "Snapshot taken with Crystal Frost", photoAlbumID, (success, status, itemID, assetID) =>
            {
                if (success)
                {
                    if (StatusText != null) StatusText.text = "Snapshot saved to inventory!";
                    Debug.Log($"Snapshot saved to inventory with item ID {itemID}");
                }
                else
                {
                    if (StatusText != null) StatusText.text = $"Error saving to inventory: {status}";
                    Debug.LogError($"Failed to save snapshot to inventory: {status}");
                }
            });
        }
        else
        {
            if (StatusText != null) StatusText.text = "No snapshot to save.";
            Debug.LogWarning("No snapshot to save.");
        }
    }
}
