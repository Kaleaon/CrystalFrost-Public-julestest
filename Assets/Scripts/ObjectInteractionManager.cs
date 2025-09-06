using UnityEngine;
using OpenMetaverse;

public class ObjectInteractionManager : MonoBehaviour
{
    private ObjectInspectorUI objectInspectorUI;

    void Start()
    {
        ClientManager.client.Objects.OnObjectProperties += Objects_OnObjectProperties;
        objectInspectorUI = FindObjectOfType<ObjectInspectorUI>();
    }

    void OnDestroy()
    {
        if (ClientManager.client != null && ClientManager.client.Network.Connected)
        {
            ClientManager.client.Objects.OnObjectProperties -= Objects_OnObjectProperties;
        }
    }

    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                PrimInfo primInfo = hit.collider.GetComponent<PrimInfo>();
                if (primInfo != null)
                {
                    uint localID = primInfo.localID;
                    Debug.Log($"Clicked on object with localID: {localID}");
                    ClientManager.client.Objects.SelectObject(ClientManager.client.Network.CurrentSim.Handle, localID);
                }
            }
        }
    }

    private void Objects_OnObjectProperties(object sender, ObjectPropertiesEventArgs e)
    {
        if (objectInspectorUI != null)
        {
            objectInspectorUI.ShowPanel();
            objectInspectorUI.DisplayProperties(e.Properties.Prim);
        }
    }
}
