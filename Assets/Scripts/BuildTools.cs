using OpenMetaverse;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class BuildTools : MonoBehaviour
{
    [Header("Build Window")]
    public GameObject buildWindow;
    public Button closeButton;
    
    [Header("Create Tools")]
    public Button createCubeButton;
    public Button createSphereButton;
    public Button createCylinderButton;
    public Button createPyramidButton;
    public Button createTorusButton;
    public Button createTubeButton;
    public Button createRingButton;
    public Button createSculptButton;
    
    [Header("Edit Tools")]
    public GameObject editPanel;
    public Button selectButton;
    public Button moveButton;
    public Button rotateButton;
    public Button scaleButton;
    public Button duplicateButton;
    public Button deleteButton;
    public Button linkButton;
    public Button unlinkButton;
    
    [Header("Position Controls")]
    public TMP_InputField posXField;
    public TMP_InputField posYField;
    public TMP_InputField posZField;
    public Slider posXSlider;
    public Slider posYSlider;
    public Slider posZSlider;
    
    [Header("Rotation Controls")]
    public TMP_InputField rotXField;
    public TMP_InputField rotYField;
    public TMP_InputField rotZField;
    public Slider rotXSlider;
    public Slider rotYSlider;
    public Slider rotZSlider;
    
    [Header("Scale Controls")]
    public TMP_InputField scaleXField;
    public TMP_InputField scaleYField;
    public TMP_InputField scaleZField;
    public Slider scaleXSlider;
    public Slider scaleYSlider;
    public Slider scaleZSlider;
    public Toggle uniformScaleToggle;
    
    [Header("Shape Controls")]
    public Slider pathCutBeginSlider;
    public Slider pathCutEndSlider;
    public Slider hollowSlider;
    public Slider twistSlider;
    public Slider taperXSlider;
    public Slider taperYSlider;
    public Slider shearXSlider;
    public Slider shearYSlider;
    
    [Header("Texture Controls")]
    public Button texturePickerButton;
    public Slider textureScaleUSlider;
    public Slider textureScaleVSlider;
    public Slider textureOffsetUSlider;
    public Slider textureOffsetVSlider;
    public Slider textureRotationSlider;
    public ColorPicker colorPicker;
    public Slider glowSlider;
    public Toggle fullbrightToggle;
    
    [Header("Physics")]
    public Toggle phantomToggle;
    public Toggle physicalToggle;
    public Toggle temporaryToggle;
    
    private GridClient client;
    private GameObject selectedObject;
    private Primitive selectedPrim;
    private bool buildModeEnabled = false;
    private BuildTool currentTool = BuildTool.Select;
    
    public enum BuildTool
    {
        Select,
        Move,
        Rotate,
        Scale,
        Create
    }

    void Awake()
    {
        buildWindow.SetActive(false);
        SetupUI();
    }

    void SetupUI()
    {
        if (closeButton) closeButton.onClick.AddListener(() => buildWindow.SetActive(false));
        
        // Create tools
        if (createCubeButton) createCubeButton.onClick.AddListener(() => CreatePrim(PrimType.Box));
        if (createSphereButton) createSphereButton.onClick.AddListener(() => CreatePrim(PrimType.Sphere));
        if (createCylinderButton) createCylinderButton.onClick.AddListener(() => CreatePrim(PrimType.Cylinder));
        if (createPyramidButton) createPyramidButton.onClick.AddListener(() => CreatePrim(PrimType.Prism));
        if (createTorusButton) createTorusButton.onClick.AddListener(() => CreatePrim(PrimType.Torus));
        if (createTubeButton) createTubeButton.onClick.AddListener(() => CreatePrim(PrimType.Tube));
        if (createRingButton) createRingButton.onClick.AddListener(() => CreatePrim(PrimType.Ring));
        if (createSculptButton) createSculptButton.onClick.AddListener(() => CreatePrim(PrimType.Sculpt));
        
        // Edit tools
        if (selectButton) selectButton.onClick.AddListener(() => SetBuildTool(BuildTool.Select));
        if (moveButton) moveButton.onClick.AddListener(() => SetBuildTool(BuildTool.Move));
        if (rotateButton) rotateButton.onClick.AddListener(() => SetBuildTool(BuildTool.Rotate));
        if (scaleButton) scaleButton.onClick.AddListener(() => SetBuildTool(BuildTool.Scale));
        if (duplicateButton) duplicateButton.onClick.AddListener(DuplicateObject);
        if (deleteButton) deleteButton.onClick.AddListener(DeleteObject);
        if (linkButton) linkButton.onClick.AddListener(LinkObjects);
        if (unlinkButton) unlinkButton.onClick.AddListener(UnlinkObjects);
        
        // Position sliders
        if (posXSlider) posXSlider.onValueChanged.AddListener(OnPositionXChanged);
        if (posYSlider) posYSlider.onValueChanged.AddListener(OnPositionYChanged);
        if (posZSlider) posZSlider.onValueChanged.AddListener(OnPositionZChanged);
        
        // Rotation sliders
        if (rotXSlider) rotXSlider.onValueChanged.AddListener(OnRotationXChanged);
        if (rotYSlider) rotYSlider.onValueChanged.AddListener(OnRotationYChanged);
        if (rotZSlider) rotZSlider.onValueChanged.AddListener(OnRotationZChanged);
        
        // Scale sliders
        if (scaleXSlider) scaleXSlider.onValueChanged.AddListener(OnScaleXChanged);
        if (scaleYSlider) scaleYSlider.onValueChanged.AddListener(OnScaleYChanged);
        if (scaleZSlider) scaleZSlider.onValueChanged.AddListener(OnScaleZChanged);
        
        // Shape sliders
        if (pathCutBeginSlider) pathCutBeginSlider.onValueChanged.AddListener(OnPathCutBeginChanged);
        if (pathCutEndSlider) pathCutEndSlider.onValueChanged.AddListener(OnPathCutEndChanged);
        if (hollowSlider) hollowSlider.onValueChanged.AddListener(OnHollowChanged);
        if (twistSlider) twistSlider.onValueChanged.AddListener(OnTwistChanged);
        if (taperXSlider) taperXSlider.onValueChanged.AddListener(OnTaperXChanged);
        if (taperYSlider) taperYSlider.onValueChanged.AddListener(OnTaperYChanged);
        if (shearXSlider) shearXSlider.onValueChanged.AddListener(OnShearXChanged);
        if (shearYSlider) shearYSlider.onValueChanged.AddListener(OnShearYChanged);
        
        // Texture controls
        if (texturePickerButton) texturePickerButton.onClick.AddListener(OpenTexturePicker);
        if (textureScaleUSlider) textureScaleUSlider.onValueChanged.AddListener(OnTextureScaleUChanged);
        if (textureScaleVSlider) textureScaleVSlider.onValueChanged.AddListener(OnTextureScaleVChanged);
        if (textureOffsetUSlider) textureOffsetUSlider.onValueChanged.AddListener(OnTextureOffsetUChanged);
        if (textureOffsetVSlider) textureOffsetVSlider.onValueChanged.AddListener(OnTextureOffsetVChanged);
        if (textureRotationSlider) textureRotationSlider.onValueChanged.AddListener(OnTextureRotationChanged);
        if (glowSlider) glowSlider.onValueChanged.AddListener(OnGlowChanged);
        
        // Physics toggles
        if (phantomToggle) phantomToggle.onValueChanged.AddListener(OnPhantomChanged);
        if (physicalToggle) physicalToggle.onValueChanged.AddListener(OnPhysicalChanged);
        if (temporaryToggle) temporaryToggle.onValueChanged.AddListener(OnTemporaryChanged);
        if (fullbrightToggle) fullbrightToggle.onValueChanged.AddListener(OnFullbrightChanged);
        
        // Setup input fields
        SetupInputFields();
    }

    void SetupInputFields()
    {
        // Link input fields to sliders
        if (posXField && posXSlider)
        {
            posXField.onEndEdit.AddListener((value) => 
            {
                if (float.TryParse(value, out float val))
                    posXSlider.value = val;
            });
        }
        
        // Similar setup for other input fields...
    }

    void Start()
    {
        client = ClientManager.client;
    }

    public void ShowBuildTools()
    {
        buildWindow.SetActive(true);
        buildModeEnabled = true;
        SetBuildTool(BuildTool.Select);
    }

    public void HideBuildTools()
    {
        buildWindow.SetActive(false);
        buildModeEnabled = false;
        DeselectObject();
    }

    void SetBuildTool(BuildTool tool)
    {
        currentTool = tool;
        UpdateToolButtonStates();
    }

    void UpdateToolButtonStates()
    {
        // Update visual states of tool buttons
        // Implementation depends on your UI design
    }

    void CreatePrim(PrimType primType)
    {
        if (client == null) return;
        
        // Calculate position in front of avatar
        Vector3 avatarPos = client.Self.SimPosition.ToVector3();
        Vector3 avatarLookAt = client.Self.SimRotation.ToUnity() * Vector3.forward;
        Vector3 createPos = avatarPos + avatarLookAt * 2.0f;
        
        // Create prim data
        var primData = new Primitive.ConstructionData();
        primData.PCode = PCode.Prim;
        primData.Material = Material.Wood;
        primData.PathCurve = PathCurve.Line;
        primData.ProfileCurve = ProfileCurve.Square;
        primData.PathEnd = 1f;
        primData.PathRadiusOffset = 0f;
        primData.PathRevolutions = 1f;
        primData.PathScaleX = 1f;
        primData.PathScaleY = 1f;
        primData.PathShearX = 0f;
        primData.PathShearY = 0f;
        primData.PathSkew = 0f;
        primData.PathStart = 0f;
        primData.PathTaperX = 0f;
        primData.PathTaperY = 0f;
        primData.PathTwist = 0f;
        primData.PathTwistBegin = 0f;
        primData.ProfileBegin = 0f;
        primData.ProfileEnd = 1f;
        primData.ProfileHollow = 0f;
        
        // Set shape based on prim type
        switch (primType)
        {
            case PrimType.Box:
                primData.ProfileCurve = ProfileCurve.Square;
                break;
            case PrimType.Sphere:
                primData.ProfileCurve = ProfileCurve.Circle;
                primData.PathCurve = PathCurve.Circle;
                break;
            case PrimType.Cylinder:
                primData.ProfileCurve = ProfileCurve.Circle;
                break;
            case PrimType.Prism:
                primData.ProfileCurve = ProfileCurve.EqualTriangle;
                break;
            case PrimType.Torus:
                primData.ProfileCurve = ProfileCurve.Circle;
                primData.PathCurve = PathCurve.Circle;
                break;
        }
        
        // Create the prim
        client.Objects.AddPrim(
            client.Network.CurrentSim,
            primData,
            UUID.Random(),
            createPos.ToLMV(),
            new OpenMetaverse.Vector3(0.5f, 0.5f, 0.5f),
            Quaternion.identity.ToLMV()
        );
    }

    void Update()
    {
        if (!buildModeEnabled) return;
        
        HandleMouseInput();
        HandleKeyboardInput();
    }

    void HandleMouseInput()
    {
        if (Input.GetMouseButtonDown(0) && !UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject())
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                var primInfo = hit.collider.GetComponent<PrimInfo>();
                if (primInfo != null)
                {
                    SelectObject(hit.collider.gameObject, primInfo);
                }
            }
        }
        
        // Handle tool-specific mouse operations
        if (selectedObject != null && Input.GetMouseButton(0))
        {
            switch (currentTool)
            {
                case BuildTool.Move:
                    HandleMoveOperation();
                    break;
                case BuildTool.Rotate:
                    HandleRotateOperation();
                    break;
                case BuildTool.Scale:
                    HandleScaleOperation();
                    break;
            }
        }
    }

    void HandleKeyboardInput()
    {
        if (Input.GetKeyDown(KeyCode.Delete) && selectedObject != null)
        {
            DeleteObject();
        }
        
        if (Input.GetKeyDown(KeyCode.D) && Input.GetKey(KeyCode.LeftControl) && selectedObject != null)
        {
            DuplicateObject();
        }
    }

    void SelectObject(GameObject obj, PrimInfo primInfo)
    {
        DeselectObject();
        
        selectedObject = obj;
        selectedPrim = primInfo.prim;
        
        // Highlight selected object
        var outline = obj.GetComponent<Outline>();
        if (outline == null) outline = obj.AddComponent<Outline>();
        outline.enabled = true;
        outline.OutlineColor = Color.yellow;
        outline.OutlineWidth = 5f;
        
        // Update UI with object properties
        UpdateUIFromSelection();
        
        if (editPanel) editPanel.SetActive(true);
    }

    void DeselectObject()
    {
        if (selectedObject != null)
        {
            var outline = selectedObject.GetComponent<Outline>();
            if (outline) outline.enabled = false;
        }
        
        selectedObject = null;
        selectedPrim = null;
        
        if (editPanel) editPanel.SetActive(false);
    }

    void UpdateUIFromSelection()
    {
        if (selectedPrim == null) return;
        
        // Update position fields
        var pos = selectedPrim.Position;
        if (posXField) posXField.text = pos.X.ToString("F3");
        if (posYField) posYField.text = pos.Y.ToString("F3");
        if (posZField) posZField.text = pos.Z.ToString("F3");
        if (posXSlider) posXSlider.value = pos.X;
        if (posYSlider) posYSlider.value = pos.Y;
        if (posZSlider) posZSlider.value = pos.Z;
        
        // Update rotation fields
        var rot = selectedPrim.Rotation.GetEulerAngles();
        if (rotXField) rotXField.text = (rot.X * Mathf.Rad2Deg).ToString("F1");
        if (rotYField) rotYField.text = (rot.Y * Mathf.Rad2Deg).ToString("F1");
        if (rotZField) rotZField.text = (rot.Z * Mathf.Rad2Deg).ToString("F1");
        
        // Update scale fields
        var scale = selectedPrim.Scale;
        if (scaleXField) scaleXField.text = scale.X.ToString("F3");
        if (scaleYField) scaleYField.text = scale.Y.ToString("F3");
        if (scaleZField) scaleZField.text = scale.Z.ToString("F3");
        if (scaleXSlider) scaleXSlider.value = scale.X;
        if (scaleYSlider) scaleYSlider.value = scale.Y;
        if (scaleZSlider) scaleZSlider.value = scale.Z;
        
        // Update shape properties
        var primData = selectedPrim.PrimData;
        if (pathCutBeginSlider) pathCutBeginSlider.value = primData.PathBegin;
        if (pathCutEndSlider) pathCutEndSlider.value = primData.PathEnd;
        if (hollowSlider) hollowSlider.value = primData.ProfileHollow;
        if (twistSlider) twistSlider.value = primData.PathTwist;
        if (taperXSlider) taperXSlider.value = primData.PathTaperX;
        if (taperYSlider) taperYSlider.value = primData.PathTaperY;
        if (shearXSlider) shearXSlider.value = primData.PathShearX;
        if (shearYSlider) shearYSlider.value = primData.PathShearY;
        
        // Update physics properties
        if (phantomToggle) phantomToggle.isOn = (selectedPrim.Flags & PrimFlags.Phantom) != 0;
        if (physicalToggle) physicalToggle.isOn = (selectedPrim.Flags & PrimFlags.Physics) != 0;
        if (temporaryToggle) temporaryToggle.isOn = (selectedPrim.Flags & PrimFlags.TemporaryOnRez) != 0;
    }

    void HandleMoveOperation()
    {
        // Implement object movement with mouse
        // This would need proper 3D manipulation math
    }

    void HandleRotateOperation()
    {
        // Implement object rotation with mouse
        // This would need proper 3D manipulation math
    }

    void HandleScaleOperation()
    {
        // Implement object scaling with mouse
        // This would need proper 3D manipulation math
    }

    void DuplicateObject()
    {
        if (selectedPrim == null || client == null) return;
        
        // Duplicate the selected prim
        var newPos = selectedPrim.Position;
        newPos.X += 1.0f; // Offset by 1 meter
        
        client.Objects.AddPrim(
            client.Network.CurrentSim,
            selectedPrim.PrimData,
            UUID.Random(),
            newPos,
            selectedPrim.Scale,
            selectedPrim.Rotation
        );
    }

    void DeleteObject()
    {
        if (selectedPrim == null || client == null) return;
        
        client.Objects.DeleteObject(client.Network.CurrentSim, selectedPrim.LocalID);
        DeselectObject();
    }

    void LinkObjects()
    {
        // This would implement object linking
        Debug.Log("Link objects");
    }

    void UnlinkObjects()
    {
        // This would implement object unlinking
        Debug.Log("Unlink objects");
    }

    void OpenTexturePicker()
    {
        // This would open a texture picker dialog
        Debug.Log("Open texture picker");
    }

    #region UI Event Handlers

    void OnPositionXChanged(float value)
    {
        if (selectedPrim == null) return;
        UpdateObjectPosition();
    }

    void OnPositionYChanged(float value)
    {
        if (selectedPrim == null) return;
        UpdateObjectPosition();
    }

    void OnPositionZChanged(float value)
    {
        if (selectedPrim == null) return;
        UpdateObjectPosition();
    }

    void UpdateObjectPosition()
    {
        if (selectedPrim == null || client == null) return;
        
        var newPos = new OpenMetaverse.Vector3(
            posXSlider.value,
            posYSlider.value,
            posZSlider.value
        );
        
        client.Objects.SetPosition(client.Network.CurrentSim, selectedPrim.LocalID, newPos);
        
        // Update input fields
        if (posXField) posXField.text = newPos.X.ToString("F3");
        if (posYField) posYField.text = newPos.Y.ToString("F3");
        if (posZField) posZField.text = newPos.Z.ToString("F3");
    }

    void OnRotationXChanged(float value) { UpdateObjectRotation(); }
    void OnRotationYChanged(float value) { UpdateObjectRotation(); }
    void OnRotationZChanged(float value) { UpdateObjectRotation(); }

    void UpdateObjectRotation()
    {
        if (selectedPrim == null || client == null) return;
        
        var euler = new OpenMetaverse.Vector3(
            rotXSlider.value * Mathf.Deg2Rad,
            rotYSlider.value * Mathf.Deg2Rad,
            rotZSlider.value * Mathf.Deg2Rad
        );
        
        var newRot = OpenMetaverse.Quaternion.CreateFromEulers(euler);
        client.Objects.SetRotation(client.Network.CurrentSim, selectedPrim.LocalID, newRot);
    }

    void OnScaleXChanged(float value) { UpdateObjectScale(); }
    void OnScaleYChanged(float value) { UpdateObjectScale(); }
    void OnScaleZChanged(float value) { UpdateObjectScale(); }

    void UpdateObjectScale()
    {
        if (selectedPrim == null || client == null) return;
        
        var newScale = new OpenMetaverse.Vector3(
            scaleXSlider.value,
            scaleYSlider.value,
            scaleZSlider.value
        );
        
        // Apply uniform scaling if enabled
        if (uniformScaleToggle && uniformScaleToggle.isOn)
        {
            float maxScale = Mathf.Max(newScale.X, newScale.Y, newScale.Z);
            newScale = new OpenMetaverse.Vector3(maxScale, maxScale, maxScale);
            
            scaleXSlider.value = maxScale;
            scaleYSlider.value = maxScale;
            scaleZSlider.value = maxScale;
        }
        
        client.Objects.SetScale(client.Network.CurrentSim, selectedPrim.LocalID, newScale);
        
        // Update input fields
        if (scaleXField) scaleXField.text = newScale.X.ToString("F3");
        if (scaleYField) scaleYField.text = newScale.Y.ToString("F3");
        if (scaleZField) scaleZField.text = newScale.Z.ToString("F3");
    }

    void OnPathCutBeginChanged(float value) { UpdateShape(); }
    void OnPathCutEndChanged(float value) { UpdateShape(); }
    void OnHollowChanged(float value) { UpdateShape(); }
    void OnTwistChanged(float value) { UpdateShape(); }
    void OnTaperXChanged(float value) { UpdateShape(); }
    void OnTaperYChanged(float value) { UpdateShape(); }
    void OnShearXChanged(float value) { UpdateShape(); }
    void OnShearYChanged(float value) { UpdateShape(); }

    void UpdateShape()
    {
        if (selectedPrim == null || client == null) return;
        
        var shape = selectedPrim.PrimData;
        shape.PathBegin = pathCutBeginSlider.value;
        shape.PathEnd = pathCutEndSlider.value;
        shape.ProfileHollow = hollowSlider.value;
        shape.PathTwist = twistSlider.value;
        shape.PathTaperX = taperXSlider.value;
        shape.PathTaperY = taperYSlider.value;
        shape.PathShearX = shearXSlider.value;
        shape.PathShearY = shearYSlider.value;
        
        client.Objects.SetShape(client.Network.CurrentSim, selectedPrim.LocalID, shape);
    }

    void OnTextureScaleUChanged(float value) { UpdateTexture(); }
    void OnTextureScaleVChanged(float value) { UpdateTexture(); }
    void OnTextureOffsetUChanged(float value) { UpdateTexture(); }
    void OnTextureOffsetVChanged(float value) { UpdateTexture(); }
    void OnTextureRotationChanged(float value) { UpdateTexture(); }
    void OnGlowChanged(float value) { UpdateTexture(); }

    void UpdateTexture()
    {
        if (selectedPrim == null || client == null) return;
        
        // Update texture properties
        var texEntry = selectedPrim.Textures.DefaultTexture;
        texEntry.RepeatU = textureScaleUSlider.value;
        texEntry.RepeatV = textureScaleVSlider.value;
        texEntry.OffsetU = textureOffsetUSlider.value;
        texEntry.OffsetV = textureOffsetVSlider.value;
        texEntry.Rotation = textureRotationSlider.value;
        texEntry.Glow = glowSlider.value;
        
        if (colorPicker)
        {
            var color = colorPicker.CurrentColor;
            texEntry.RGBA = new Color4(color.r, color.g, color.b, color.a);
        }
        
        if (fullbrightToggle)
        {
            texEntry.Fullbright = fullbrightToggle.isOn;
        }
        
        client.Objects.SetTextures(client.Network.CurrentSim, selectedPrim.LocalID, selectedPrim.Textures);
    }

    void OnPhantomChanged(bool value)
    {
        UpdateFlags();
    }

    void OnPhysicalChanged(bool value)
    {
        UpdateFlags();
    }

    void OnTemporaryChanged(bool value)
    {
        UpdateFlags();
    }

    void OnFullbrightChanged(bool value)
    {
        UpdateTexture();
    }

    void UpdateFlags()
    {
        if (selectedPrim == null || client == null) return;
        
        PrimFlags flags = selectedPrim.Flags;
        
        if (phantomToggle && phantomToggle.isOn)
            flags |= PrimFlags.Phantom;
        else
            flags &= ~PrimFlags.Phantom;
            
        if (physicalToggle && physicalToggle.isOn)
            flags |= PrimFlags.Physics;
        else
            flags &= ~PrimFlags.Physics;
            
        if (temporaryToggle && temporaryToggle.isOn)
            flags |= PrimFlags.TemporaryOnRez;
        else
            flags &= ~PrimFlags.TemporaryOnRez;
        
        client.Objects.SetFlags(client.Network.CurrentSim, selectedPrim.LocalID, flags);
    }

    #endregion
}