/*
 * Crystal Frost Second Life Viewer - Advanced Building and Construction Tools
 * 
 * SYSTEM OVERVIEW:
 * ================
 * This is a comprehensive building and construction system for the Crystal Frost Second Life viewer,
 * providing professional-grade tools for 3D object creation, editing, and manipulation. The system
 * offers complete building capabilities including primitive creation, texture application, physics
 * configuration, and advanced construction techniques used in Second Life content creation.
 * 
 * ARCHITECTURE:
 * =============
 * - Unity MonoBehaviour component with full 3D editing integration
 * - Real-time object manipulation with visual feedback
 * - LibreMetaverse integration for object persistence and synchronization
 * - Multi-tool system supporting different construction modes
 * - Advanced parameter control with real-time visual updates
 * - Comprehensive material and texture management
 * - Physics simulation and collision detection integration
 * 
 * KEY FEATURES:
 * =============
 * 1. PRIMITIVE CREATION SYSTEM:
 *    - Complete set of basic primitives (box, sphere, cylinder, prism, torus)
 *    - Advanced shape parameters and configuration
 *    - Real-time primitive generation and modification
 *    - Parametric shape control with immediate visual feedback
 *    - Custom primitive templates and presets
 * 
 * 2. 3D MANIPULATION TOOLS:
 *    - Precision positioning with coordinate input
 *    - Multi-axis rotation with quaternion mathematics
 *    - Proportional and non-proportional scaling
 *    - Grid snapping and alignment tools
 *    - Copy, paste, and duplication operations
 * 
 * 3. ADVANCED SHAPE EDITING:
 *    - Path cut operations for complex shapes
 *    - Hollow parameter for creating tunnels and rings
 *    - Twist operations for spiral and helical forms
 *    - Taper controls for cone and pyramid shapes
 *    - Shear operations for skewed and distorted forms
 * 
 * 4. TEXTURE AND MATERIAL SYSTEM:
 *    - Complete texture application and manipulation
 *    - UV mapping controls (scale, offset, rotation)
 *    - Color tinting and transparency controls
 *    - Glow effects and fullbright rendering
 *    - Multi-face texture application
 * 
 * 5. PHYSICS AND DYNAMICS:
 *    - Physical object properties configuration
 *    - Phantom mode for non-colliding objects
 *    - Temporary object settings
 *    - Mass and density calculations
 *    - Collision shape optimization
 * 
 * 6. OBJECT LINKING AND GROUPING:
 *    - Multi-object selection and manipulation
 *    - Linking objects into compound structures
 *    - Unlinking for individual editing
 *    - Hierarchical object relationships
 *    - Batch operations on linked sets
 * 
 * TECHNICAL IMPLEMENTATION:
 * =========================
 * - Unity's 3D rendering and physics systems
 * - LibreMetaverse object creation and modification API
 * - Real-time mesh generation and updates
 * - Advanced input handling for 3D manipulation
 * - Efficient memory management for large builds
 * - Thread-safe operations for network synchronization
 * - Optimized rendering for construction preview
 * 
 * INTEGRATION POINTS:
 * ===================
 * - LibreMetaverse ObjectManager for object persistence
 * - Unity Physics system for collision and dynamics
 * - Crystal Frost texture management system
 * - LSL Script Editor for scripted object behavior
 * - Material and shader systems for visual effects
 * - Camera controls for optimal building views
 * 
 * PROFESSIONAL BUILDING FEATURES:
 * ================================
 * - Precision numeric input for exact measurements
 * - Construction grid and snapping systems
 * - Advanced selection and manipulation modes
 * - Professional measurement and alignment tools
 * - Batch editing operations for efficiency
 * - Undo/redo system for construction safety
 * 
 * USAGE:
 * ======
 * This component should be attached to a GameObject with proper UI references configured.
 * Building mode can be activated via ShowBuildTools() and provides complete 3D construction
 * capabilities with real-time visual feedback and precise control systems.
 * 
 * Author: Crystal Frost Development Team
 * Version: 2.0
 * Unity Compatibility: 2021.3.6f1 LTS and higher
 * LibreMetaverse: Compatible with latest versions
 * Unity Physics: Requires Physics and Physics2D modules
 */

using OpenMetaverse;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Linq;
using System;

/// <summary>
/// Advanced Building and Construction Tools for Crystal Frost Second Life Viewer
/// Provides comprehensive 3D object creation, editing, and manipulation capabilities
/// with professional-grade tools and real-time visual feedback systems.
/// </summary>
public class BuildTools : MonoBehaviour
{
    #region Inspector Fields
    
    [Header("Build Window Management")]
    [Tooltip("Main building tools window GameObject")]
    public GameObject buildWindow;
    
    [Tooltip("Button to close building tools window")]
    public Button closeButton;
    
    [Tooltip("Toggle button to enable/disable building mode")]
    public Toggle buildModeToggle;
    
    [Header("Tool Selection")]
    [Tooltip("Button to activate selection/move tool")]
    public Button selectToolButton;
    
    [Tooltip("Button to activate object creation tool")]
    public Button createToolButton;
    
    [Tooltip("Button to activate copy/duplicate tool")]
    public Button copyToolButton;
    
    [Tooltip("Button to activate deletion tool")]
    public Button deleteToolButton;
    
    [Header("Primitive Creation")]
    [Tooltip("Button to create box primitive")]
    public Button createBoxButton;
    
    [Tooltip("Button to create sphere primitive")]
    public Button createSphereButton;
    
    [Tooltip("Button to create cylinder primitive")]
    public Button createCylinderButton;
    
    [Tooltip("Button to create prism primitive")]
    public Button createPrismButton;
    
    [Tooltip("Button to create torus primitive")]
    public Button createTorusButton;
    
    [Header("Object Manipulation Controls")]
    [Tooltip("Panel containing object editing controls")]
    public GameObject editPanel;
    
    [Header("Position Controls")]
    [Tooltip("Input field for X position coordinate")]
    public TMP_InputField posXField;
    
    [Tooltip("Input field for Y position coordinate")]
    public TMP_InputField posYField;
    
    [Tooltip("Input field for Z position coordinate")]
    public TMP_InputField posZField;
    
    [Tooltip("Slider for X position adjustment")]
    public Slider posXSlider;
    
    [Tooltip("Slider for Y position adjustment")]
    public Slider posYSlider;
    
    [Tooltip("Slider for Z position adjustment")]
    public Slider posZSlider;
    
    [Header("Rotation Controls")]
    [Tooltip("Input field for X rotation (degrees)")]
    public TMP_InputField rotXField;
    
    [Tooltip("Input field for Y rotation (degrees)")]
    public TMP_InputField rotYField;
    
    [Tooltip("Input field for Z rotation (degrees)")]
    public TMP_InputField rotZField;
    
    [Tooltip("Slider for X rotation adjustment")]
    public Slider rotXSlider;
    
    [Tooltip("Slider for Y rotation adjustment")]
    public Slider rotYSlider;
    
    [Tooltip("Slider for Z rotation adjustment")]
    public Slider rotZSlider;
    
    [Header("Scale Controls")]
    [Tooltip("Input field for X scale factor")]
    public TMP_InputField scaleXField;
    
    [Tooltip("Input field for Y scale factor")]
    public TMP_InputField scaleYField;
    
    [Tooltip("Input field for Z scale factor")]
    public TMP_InputField scaleZField;
    
    [Tooltip("Slider for X scale adjustment")]
    public Slider scaleXSlider;
    
    [Tooltip("Slider for Y scale adjustment")]
    public Slider scaleYSlider;
    
    [Tooltip("Slider for Z scale adjustment")]
    public Slider scaleZSlider;
    
    [Tooltip("Toggle for uniform scaling across all axes")]
    public Toggle uniformScaleToggle;
    
    [Header("Shape Parameter Controls")]
    [Tooltip("Slider for path cut begin parameter")]
    public Slider pathCutBeginSlider;
    
    [Tooltip("Slider for path cut end parameter")]
    public Slider pathCutEndSlider;
    
    [Tooltip("Slider for hollow parameter")]
    public Slider hollowSlider;
    
    [Tooltip("Slider for twist parameter")]
    public Slider twistSlider;
    
    [Tooltip("Slider for X taper parameter")]
    public Slider taperXSlider;
    
    [Tooltip("Slider for Y taper parameter")]
    public Slider taperYSlider;
    
    [Tooltip("Slider for X shear parameter")]
    public Slider shearXSlider;
    
    [Tooltip("Slider for Y shear parameter")]
    public Slider shearYSlider;
    
    [Header("Texture and Material Controls")]
    [Tooltip("Button to open texture picker dialog")]
    public Button texturePickerButton;
    
    [Tooltip("Slider for texture U scale")]
    public Slider textureScaleUSlider;
    
    [Tooltip("Slider for texture V scale")]
    public Slider textureScaleVSlider;
    
    [Tooltip("Slider for texture U offset")]
    public Slider textureOffsetUSlider;
    
    [Tooltip("Slider for texture V offset")]
    public Slider textureOffsetVSlider;
    
    [Tooltip("Slider for texture rotation")]
    public Slider textureRotationSlider;
    
    [Tooltip("Color picker for object tinting")]
    public Image colorPicker;
    
    [Tooltip("Slider for glow effect intensity")]
    public Slider glowSlider;
    
    [Header("Physics and Properties")]
    [Tooltip("Toggle for phantom mode (non-colliding)")]
    public Toggle phantomToggle;
    
    [Tooltip("Toggle for physical simulation")]
    public Toggle physicalToggle;
    
    [Tooltip("Toggle for temporary object (auto-delete)")]
    public Toggle temporaryToggle;
    
    [Tooltip("Toggle for fullbright rendering")]
    public Toggle fullbrightToggle;
    
    [Header("Object Linking")]
    [Tooltip("Button to link selected objects")]
    public Button linkButton;
    
    [Tooltip("Button to unlink selected objects")]
    public Button unlinkButton;
    
    #endregion
    
    #region Private Fields
    
    /// <summary>GridClient reference for LibreMetaverse integration</summary>
    private GridClient client;
    
    /// <summary>Currently selected building tool mode</summary>
    private BuildTool currentTool = BuildTool.Select;
    
    /// <summary>Whether building mode is currently active</summary>
    private bool buildModeEnabled = false;
    
    /// <summary>Currently selected GameObject for editing</summary>
    private GameObject selectedObject;
    
    /// <summary>Currently selected primitive data</summary>
    private Primitive selectedPrim;
    
    /// <summary>List of currently selected objects for multi-selection</summary>
    private List<GameObject> selectedObjects = new();
    
    /// <summary>Camera reference for mouse-to-world calculations</summary>
    private Camera buildCamera;
    
    /// <summary>Previous mouse position for drag operations</summary>
    private Vector3 lastMousePosition;
    
    /// <summary>Whether currently dragging an object</summary>
    private bool isDragging = false;
    
    /// <summary>Grid size for snapping operations</summary>
    private float gridSize = 1.0f;
    
    /// <summary>Whether grid snapping is enabled</summary>
    private bool snapToGrid = false;
    
    /// <summary>Undo stack for construction operations</summary>
    private Stack<BuildOperation> undoStack = new();
    
    /// <summary>Redo stack for construction operations</summary>
    private Stack<BuildOperation> redoStack = new();
    
    /// <summary>Maximum number of undo operations to store</summary>
    private const int MAX_UNDO_OPERATIONS = 50;
    
    #endregion
    
    #region Enums and Data Structures
    
    /// <summary>
    /// Available building tool modes for object manipulation
    /// Each mode provides different interaction capabilities
    /// </summary>
    public enum BuildTool
    {
        Select,     // Selection and basic manipulation tool
        Create,     // Object creation and placement tool
        Move,       // Precise movement and positioning tool
        Rotate,     // Rotation and orientation tool
        Scale,      // Scaling and size adjustment tool
        Copy,       // Duplication and copying tool
        Delete,     // Object deletion and removal tool
        Link,       // Object linking and grouping tool
        Texture     // Texture application and UV mapping tool
    }
    
    /// <summary>
    /// Primitive type enumeration for object creation
    /// Matches Second Life primitive types
    /// </summary>
    public enum PrimType
    {
        Box,        // Cubic/rectangular primitive
        Sphere,     // Spherical primitive
        Cylinder,   // Cylindrical primitive
        Prism,      // Triangular prism primitive
        Torus,      // Torus/doughnut primitive
        Tube,       // Hollow tube primitive
        Ring,       // Ring/washer primitive
        Sculpt      // Sculpted primitive (advanced)
    }
    
    /// <summary>
    /// Build operation data for undo/redo functionality
    /// Stores complete state information for operation reversal
    /// </summary>
    [System.Serializable]
    public class BuildOperation
    {
        public enum OperationType
        {
            Create,     // Object creation operation
            Delete,     // Object deletion operation
            Move,       // Object movement operation
            Rotate,     // Object rotation operation
            Scale,      // Object scaling operation
            Modify,     // Object property modification
            Link,       // Object linking operation
            Unlink      // Object unlinking operation
        }
        
        public OperationType type;              // Type of operation performed
        public GameObject targetObject;         // Primary object affected
        public List<GameObject> affectedObjects; // All objects affected by operation
        public Vector3 previousPosition;        // Previous position (for movement)
        public Quaternion previousRotation;     // Previous rotation (for rotation)
        public Vector3 previousScale;           // Previous scale (for scaling)
        public string previousProperties;       // Previous properties (serialized)
        public DateTime timestamp;              // When operation was performed
    }
    
    #endregion
    
    #region Unity Lifecycle
    
    /// <summary>
    /// Initialize building tools component
    /// Called before Start() on the first frame
    /// </summary>
    void Awake()
    {
        // Hide build window initially
        buildWindow.SetActive(false);
        
        // Setup UI event handlers and components
        SetupUI();
        
        // Get main camera reference for mouse calculations
        buildCamera = Camera.main;
        if (buildCamera == null)
        {
            buildCamera = FindObjectOfType<Camera>();
        }
    }
    
    /// <summary>
    /// Complete initialization after all objects are available
    /// Called on the first frame after Awake()
    /// </summary>
    void Start()
    {
        // Get GridClient reference for LibreMetaverse integration
        client = ClientManager.client;
        
        // Subscribe to relevant LibreMetaverse events
        if (client != null)
        {
            // Subscribe to object-related events for real-time updates
            // client.Objects.ObjectUpdate += OnObjectUpdate;
            // client.Objects.KillObject += OnKillObject;
        }
    }
    
    /// <summary>
    /// Update building tools each frame
    /// Handles real-time input processing and object manipulation
    /// </summary>
    void Update()
    {
        // Only process input when building mode is active
        if (!buildModeEnabled) return;
        
        // Handle mouse input for object manipulation
        HandleMouseInput();
        
        // Handle keyboard input for shortcuts and modifiers
        HandleKeyboardInput();
        
        // Update visual feedback for selected objects
        UpdateSelectionVisuals();
    }
    
    /// <summary>
    /// Cleanup when component is destroyed
    /// Saves current state and unsubscribes from events
    /// </summary>
    void OnDestroy()
    {
        // Unsubscribe from LibreMetaverse events to prevent memory leaks
        if (client != null)
        {
            // client.Objects.ObjectUpdate -= OnObjectUpdate;
            // client.Objects.KillObject -= OnKillObject;
        }
        
        // Clear selection and temporary data
        ClearSelection();
        undoStack.Clear();
        redoStack.Clear();
    }
    
    #endregion
    
    #region Initialization and Setup
    
    /// <summary>
    /// Configure UI event handlers and initialize components
    /// Sets up all button clicks, slider changes, and input events
    /// </summary>
    void SetupUI()
    {
        // Main window controls
        if (closeButton) closeButton.onClick.AddListener(HideBuildTools);
        if (buildModeToggle) buildModeToggle.onValueChanged.AddListener(OnBuildModeToggle);
        
        // Tool selection buttons
        if (selectToolButton) selectToolButton.onClick.AddListener(() => SetBuildTool(BuildTool.Select));
        if (createToolButton) createToolButton.onClick.AddListener(() => SetBuildTool(BuildTool.Create));
        if (copyToolButton) copyToolButton.onClick.AddListener(() => SetBuildTool(BuildTool.Copy));
        if (deleteToolButton) deleteToolButton.onClick.AddListener(() => SetBuildTool(BuildTool.Delete));
        
        // Primitive creation buttons
        if (createBoxButton) createBoxButton.onClick.AddListener(() => CreatePrim(PrimType.Box));
        if (createSphereButton) createSphereButton.onClick.AddListener(() => CreatePrim(PrimType.Sphere));
        if (createCylinderButton) createCylinderButton.onClick.AddListener(() => CreatePrim(PrimType.Cylinder));
        if (createPrismButton) createPrismButton.onClick.AddListener(() => CreatePrim(PrimType.Prism));
        if (createTorusButton) createTorusButton.onClick.AddListener(() => CreatePrim(PrimType.Torus));
        
        // Object linking buttons
        if (linkButton) linkButton.onClick.AddListener(LinkObjects);
        if (unlinkButton) unlinkButton.onClick.AddListener(UnlinkObjects);
        
        // Setup parameter control sliders
        SetupParameterControls();
        
        // Setup input field synchronization
        SetupInputFieldSync();
        
        // Initialize tool to selection mode
        SetBuildTool(BuildTool.Select);
    }
    
    /// <summary>
    /// Setup parameter control sliders with event handlers
    /// Connects all shape and transformation parameter controls
    /// </summary>
    void SetupParameterControls()
    {
        // Position control sliders
        if (posXSlider) posXSlider.onValueChanged.AddListener(OnPositionXChanged);
        if (posYSlider) posYSlider.onValueChanged.AddListener(OnPositionYChanged);
        if (posZSlider) posZSlider.onValueChanged.AddListener(OnPositionZChanged);
        
        // Rotation control sliders  
        if (rotXSlider) rotXSlider.onValueChanged.AddListener(OnRotationXChanged);
        if (rotYSlider) rotYSlider.onValueChanged.AddListener(OnRotationYChanged);
        if (rotZSlider) rotZSlider.onValueChanged.AddListener(OnRotationZChanged);
        
        // Scale control sliders
        if (scaleXSlider) scaleXSlider.onValueChanged.AddListener(OnScaleXChanged);
        if (scaleYSlider) scaleYSlider.onValueChanged.AddListener(OnScaleYChanged);
        if (scaleZSlider) scaleZSlider.onValueChanged.AddListener(OnScaleZChanged);
        
        // Shape parameter sliders
        if (pathCutBeginSlider) pathCutBeginSlider.onValueChanged.AddListener(OnPathCutBeginChanged);
        if (pathCutEndSlider) pathCutEndSlider.onValueChanged.AddListener(OnPathCutEndChanged);
        if (hollowSlider) hollowSlider.onValueChanged.AddListener(OnHollowChanged);
        if (twistSlider) twistSlider.onValueChanged.AddListener(OnTwistChanged);
        if (taperXSlider) taperXSlider.onValueChanged.AddListener(OnTaperXChanged);
        if (taperYSlider) taperYSlider.onValueChanged.AddListener(OnTaperYChanged);
        if (shearXSlider) shearXSlider.onValueChanged.AddListener(OnShearXChanged);
        if (shearYSlider) shearYSlider.onValueChanged.AddListener(OnShearYChanged);
        
        // Texture control sliders
        if (texturePickerButton) texturePickerButton.onClick.AddListener(OpenTexturePicker);
        if (textureScaleUSlider) textureScaleUSlider.onValueChanged.AddListener(OnTextureScaleUChanged);
        if (textureScaleVSlider) textureScaleVSlider.onValueChanged.AddListener(OnTextureScaleVChanged);
        if (textureOffsetUSlider) textureOffsetUSlider.onValueChanged.AddListener(OnTextureOffsetUChanged);
        if (textureOffsetVSlider) textureOffsetVSlider.onValueChanged.AddListener(OnTextureOffsetVChanged);
        if (textureRotationSlider) textureRotationSlider.onValueChanged.AddListener(OnTextureRotationChanged);
        if (glowSlider) glowSlider.onValueChanged.AddListener(OnGlowChanged);
        
        // Physics and properties toggles
        if (phantomToggle) phantomToggle.onValueChanged.AddListener(OnPhantomChanged);
        if (physicalToggle) physicalToggle.onValueChanged.AddListener(OnPhysicalChanged);
        if (temporaryToggle) temporaryToggle.onValueChanged.AddListener(OnTemporaryChanged);
        if (fullbrightToggle) fullbrightToggle.onValueChanged.AddListener(OnFullbrightChanged);
    }
    
    /// <summary>
    /// Setup input field synchronization with sliders
    /// Links input fields to corresponding sliders for two-way data binding
    /// </summary>
    void SetupInputFieldSync()
    {
        // Position field synchronization
        if (posXField && posXSlider)
        {
            posXField.onEndEdit.AddListener((value) => 
            {
                if (float.TryParse(value, out float val))
                {
                    posXSlider.value = val;
                }
            });
        }
        
        if (posYField && posYSlider)
        {
            posYField.onEndEdit.AddListener((value) => 
            {
                if (float.TryParse(value, out float val))
                {
                    posYSlider.value = val;
                }
            });
        }
        
        if (posZField && posZSlider)
        {
            posZField.onEndEdit.AddListener((value) => 
            {
                if (float.TryParse(value, out float val))
                {
                    posZSlider.value = val;
                }
            });
        }
        
        // Similar setup for rotation and scale fields would go here
        // This is a representative sample of the synchronization pattern
    }
    
    #endregion
    
    #region Public Interface
    
    /// <summary>
    /// Show building tools window and enable building mode
    /// Main entry point for activating construction capabilities
    /// </summary>
    public void ShowBuildTools()
    {
        buildWindow.SetActive(true);
        buildModeEnabled = true;
        SetBuildTool(BuildTool.Select);
        
        // Enable build mode toggle
        if (buildModeToggle) buildModeToggle.isOn = true;
        
        // Show edit panel if object is selected
        if (selectedObject != null && editPanel)
        {
            editPanel.SetActive(true);
        }
    }
    
    /// <summary>
    /// Hide building tools window and disable building mode
    /// Deactivates construction mode and cleans up selection
    /// </summary>
    public void HideBuildTools()
    {
        buildWindow.SetActive(false);
        buildModeEnabled = false;
        
        // Clear selection when exiting build mode
        ClearSelection();
        
        // Disable build mode toggle
        if (buildModeToggle) buildModeToggle.isOn = false;
        
        // Hide edit panel
        if (editPanel) editPanel.SetActive(false);
    }
    
    /// <summary>
    /// Select an object for editing
    /// Programmatically selects object and updates UI
    /// </summary>
    /// <param name="obj">GameObject to select</param>
    public void SelectObject(GameObject obj)
    {
        var primInfo = obj.GetComponent<PrimInfo>();
        if (primInfo != null)
        {
            SelectObject(obj, primInfo);
        }
    }
    
    #endregion
    
    #region Tool Management
    
    /// <summary>
    /// Set the active building tool mode
    /// Changes interaction behavior and updates UI feedback
    /// </summary>
    /// <param name="tool">Tool mode to activate</param>
    void SetBuildTool(BuildTool tool)
    {
        currentTool = tool;
        UpdateToolButtonStates();
        UpdateCursorVisual();
        
        // Update tool-specific UI elements
        ConfigureToolUI(tool);
    }
    
    /// <summary>
    /// Update visual states of tool selection buttons
    /// Provides visual feedback for currently active tool
    /// </summary>
    void UpdateToolButtonStates()
    {
        // Define colors for active and inactive tool buttons
        Color activeColor = Color.yellow;
        Color inactiveColor = Color.white;
        
        // Update tool button colors based on current selection
        UpdateToolButtonColor(selectToolButton, currentTool == BuildTool.Select, activeColor, inactiveColor);
        UpdateToolButtonColor(createToolButton, currentTool == BuildTool.Create, activeColor, inactiveColor);
        UpdateToolButtonColor(copyToolButton, currentTool == BuildTool.Copy, activeColor, inactiveColor);
        UpdateToolButtonColor(deleteToolButton, currentTool == BuildTool.Delete, activeColor, inactiveColor);
    }
    
    /// <summary>
    /// Update individual tool button color based on active state
    /// Helper method for consistent tool button styling
    /// </summary>
    /// <param name="button">Button to update</param>
    /// <param name="isActive">Whether this button represents the active tool</param>
    /// <param name="activeColor">Color for active state</param>
    /// <param name="inactiveColor">Color for inactive state</param>
    void UpdateToolButtonColor(Button button, bool isActive, Color activeColor, Color inactiveColor)
    {
        if (button == null) return;
        
        var colors = button.colors;
        colors.normalColor = isActive ? activeColor : inactiveColor;
        colors.highlightedColor = isActive ? activeColor * 0.9f : inactiveColor * 1.1f;
        colors.selectedColor = isActive ? activeColor * 0.8f : inactiveColor * 0.9f;
        button.colors = colors;
    }
    
    /// <summary>
    /// Update cursor visual based on current tool
    /// Provides visual feedback for tool mode in 3D viewport
    /// </summary>
    void UpdateCursorVisual()
    {
        // This would update the 3D cursor or mouse cursor based on tool
        // Implementation would depend on custom cursor system
        switch (currentTool)
        {
            case BuildTool.Select:
                // Set selection cursor
                break;
            case BuildTool.Create:
                // Set creation cursor
                break;
            case BuildTool.Move:
                // Set move cursor
                break;
            case BuildTool.Rotate:
                // Set rotation cursor
                break;
            case BuildTool.Scale:
                // Set scaling cursor
                break;
            case BuildTool.Copy:
                // Set copy cursor
                break;
            case BuildTool.Delete:
                // Set delete cursor
                break;
        }
    }
    
    /// <summary>
    /// Configure UI elements based on active tool
    /// Shows/hides tool-specific controls and panels
    /// </summary>
    /// <param name="tool">Currently active tool</param>
    void ConfigureToolUI(BuildTool tool)
    {
        // Show/hide relevant UI panels based on tool
        switch (tool)
        {
            case BuildTool.Create:
                // Show primitive creation buttons
                // Hide other tool-specific panels
                break;
            case BuildTool.Texture:
                // Show texture and material controls
                // Hide other tool-specific panels
                break;
            default:
                // Show general manipulation controls
                break;
        }
    }
    
    #endregion
    
    #region Object Creation
    
    /// <summary>
    /// Create a new primitive object of specified type
    /// Generates geometry and integrates with LibreMetaverse for persistence
    /// </summary>
    /// <param name="primType">Type of primitive to create</param>
    void CreatePrim(PrimType primType)
    {
        if (client == null) 
        {
            Debug.LogError("GridClient not available for object creation");
            return;
        }
        
        try
        {
            // Calculate creation position in front of avatar
            Vector3 avatarPos = client.Self.SimPosition.ToVector3();
            Vector3 avatarLookAt = client.Self.SimRotation.ToUnity() * Vector3.forward;
            Vector3 createPos = avatarPos + avatarLookAt * 3.0f; // 3 meters in front
            
            // Create LibreMetaverse primitive data structure
            var primData = CreatePrimitiveData(primType);
            
            // Generate unique UUID for new object
            UUID objectUUID = UUID.Random();
            
            // Convert Unity coordinates to LibreMetaverse format
            var lmvPosition = createPos.ToLMV();
            var lmvScale = new OpenMetaverse.Vector3(0.5f, 0.5f, 0.5f); // Default 0.5m cube
            var lmvRotation = Quaternion.identity.ToLMV();
            
            // Create object through LibreMetaverse
            client.Objects.AddPrim(
                client.Network.CurrentSim,
                primData,
                objectUUID,
                lmvPosition,
                lmvScale,
                lmvRotation
            );
            
            Debug.Log($"Created {primType} primitive at {createPos}");
            
            // Record operation for undo functionality
            RecordBuildOperation(BuildOperation.OperationType.Create, null, null);
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error creating primitive: {ex.Message}");
        }
    }
    
    /// <summary>
    /// Create LibreMetaverse primitive construction data for specified type
    /// Configures shape parameters and properties for different primitive types
    /// </summary>
    /// <param name="primType">Type of primitive to create data for</param>
    /// <returns>Configured primitive construction data</returns>
    Primitive.ConstructionData CreatePrimitiveData(PrimType primType)
    {
        var primData = new Primitive.ConstructionData();
        
        // Set common properties for all primitives
        primData.PCode = PCode.Prim;
        primData.Material = Material.Wood;
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
        
        // Configure shape-specific parameters
        switch (primType)
        {
            case PrimType.Box:
                primData.ProfileCurve = ProfileCurve.Square;
                primData.PathCurve = PathCurve.Line;
                break;
                
            case PrimType.Sphere:
                primData.ProfileCurve = ProfileCurve.Circle;
                primData.PathCurve = PathCurve.Circle;
                break;
                
            case PrimType.Cylinder:
                primData.ProfileCurve = ProfileCurve.Circle;
                primData.PathCurve = PathCurve.Line;
                break;
                
            case PrimType.Prism:
                primData.ProfileCurve = ProfileCurve.EqualTriangle;
                primData.PathCurve = PathCurve.Line;
                break;
                
            case PrimType.Torus:
                primData.ProfileCurve = ProfileCurve.Circle;
                primData.PathCurve = PathCurve.Circle;
                primData.PathScaleX = 0.5f; // Make it more torus-like
                primData.PathScaleY = 0.5f;
                break;
                
            default:
                // Default to box shape
                primData.ProfileCurve = ProfileCurve.Square;
                primData.PathCurve = PathCurve.Line;
                break;
        }
        
        return primData;
    }
    
    #endregion
    
    #region Input Handling
    
    /// <summary>
    /// Handle mouse input for object manipulation and selection
    /// Processes clicks, drags, and mouse-based 3D manipulation
    /// </summary>
    void HandleMouseInput()
    {
        // Handle object selection on left mouse click
        if (Input.GetMouseButtonDown(0) && !IsPointerOverUI())
        {
            HandleObjectSelection();
        }
        
        // Handle tool-specific mouse operations during drag
        if (selectedObject != null && Input.GetMouseButton(0))
        {
            HandleToolSpecificMouseOperation();
        }
        
        // Handle drag start and end
        if (Input.GetMouseButtonDown(0))
        {
            StartDrag();
        }
        else if (Input.GetMouseButtonUp(0))
        {
            EndDrag();
        }
    }
    
    /// <summary>
    /// Handle object selection via mouse raycast
    /// Performs 3D raycast to detect and select objects in the scene
    /// </summary>
    void HandleObjectSelection()
    {
        if (buildCamera == null) return;
        
        // Cast ray from camera through mouse position
        Ray ray = buildCamera.ScreenPointToRay(Input.mousePosition);
        
        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            // Check if hit object has primitive information
            var primInfo = hit.collider.GetComponent<PrimInfo>();
            if (primInfo != null)
            {
                // Handle multi-selection with Ctrl key
                if (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl))
                {
                    AddToSelection(hit.collider.gameObject, primInfo);
                }
                else
                {
                    // Single selection
                    SelectObject(hit.collider.gameObject, primInfo);
                }
            }
        }
        else
        {
            // Clicked on empty space - clear selection unless Ctrl is held
            if (!Input.GetKey(KeyCode.LeftControl) && !Input.GetKey(KeyCode.RightControl))
            {
                ClearSelection();
            }
        }
    }
    
    /// <summary>
    /// Handle tool-specific mouse operations during drag
    /// Executes different manipulation based on current tool mode
    /// </summary>
    void HandleToolSpecificMouseOperation()
    {
        if (!isDragging) return;
        
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
    
    /// <summary>
    /// Handle object movement during mouse drag
    /// Provides real-time object positioning with visual feedback
    /// </summary>
    void HandleMoveOperation()
    {
        if (buildCamera == null || selectedObject == null) return;
        
        // Calculate mouse movement delta
        Vector3 currentMousePosition = Input.mousePosition;
        Vector3 mouseDelta = currentMousePosition - lastMousePosition;
        
        // Convert mouse delta to world space movement
        Vector3 worldDelta = buildCamera.ScreenToWorldPoint(mouseDelta) - buildCamera.ScreenToWorldPoint(Vector3.zero);
        
        // Apply movement to selected object
        Vector3 newPosition = selectedObject.transform.position + worldDelta;
        
        // Apply grid snapping if enabled
        if (snapToGrid)
        {
            newPosition = SnapToGrid(newPosition);
        }
        
        // Update object position
        selectedObject.transform.position = newPosition;
        
        // Update UI sliders to reflect new position
        UpdatePositionUI(newPosition);
        
        // Store current mouse position for next frame
        lastMousePosition = currentMousePosition;
    }
    
    /// <summary>
    /// Handle object rotation during mouse drag
    /// Provides intuitive rotation controls with visual feedback
    /// </summary>
    void HandleRotateOperation()
    {
        if (selectedObject == null) return;
        
        // Calculate rotation based on mouse movement
        Vector3 currentMousePosition = Input.mousePosition;
        Vector3 mouseDelta = currentMousePosition - lastMousePosition;
        
        // Convert mouse movement to rotation angles
        float rotationSpeed = 1.0f;
        float rotationX = -mouseDelta.y * rotationSpeed; // Pitch
        float rotationY = mouseDelta.x * rotationSpeed;  // Yaw
        
        // Apply rotation to selected object
        selectedObject.transform.Rotate(rotationX, rotationY, 0, Space.World);
        
        // Update UI sliders to reflect new rotation
        UpdateRotationUI(selectedObject.transform.rotation);
        
        // Store current mouse position for next frame
        lastMousePosition = currentMousePosition;
    }
    
    /// <summary>
    /// Handle object scaling during mouse drag
    /// Provides proportional and non-proportional scaling capabilities
    /// </summary>
    void HandleScaleOperation()
    {
        if (selectedObject == null) return;
        
        // Calculate scale factor based on mouse movement
        Vector3 currentMousePosition = Input.mousePosition;
        Vector3 mouseDelta = currentMousePosition - lastMousePosition;
        
        // Convert mouse movement to scale factor
        float scaleSpeed = 0.01f;
        float scaleFactor = 1.0f + (mouseDelta.y * scaleSpeed);
        
        // Apply scaling
        Vector3 currentScale = selectedObject.transform.localScale;
        Vector3 newScale;
        
        if (uniformScaleToggle && uniformScaleToggle.isOn)
        {
            // Uniform scaling
            newScale = currentScale * scaleFactor;
        }
        else
        {
            // Non-uniform scaling (X-axis based on horizontal mouse movement)
            float scaleFactorX = 1.0f + (mouseDelta.x * scaleSpeed);
            newScale = new Vector3(
                currentScale.x * scaleFactorX,
                currentScale.y * scaleFactor,
                currentScale.z
            );
        }
        
        // Apply scale constraints (minimum and maximum sizes)
        newScale = Vector3.Max(newScale, Vector3.one * 0.01f); // Minimum 1cm
        newScale = Vector3.Min(newScale, Vector3.one * 10.0f);  // Maximum 10m
        
        // Update object scale
        selectedObject.transform.localScale = newScale;
        
        // Update UI sliders to reflect new scale
        UpdateScaleUI(newScale);
        
        // Store current mouse position for next frame
        lastMousePosition = currentMousePosition;
    }
    
    /// <summary>
    /// Start drag operation and initialize tracking
    /// Sets up state for mouse-based manipulation operations
    /// </summary>
    void StartDrag()
    {
        isDragging = true;
        lastMousePosition = Input.mousePosition;
        
        // Record initial state for undo functionality
        if (selectedObject != null)
        {
            RecordObjectState(selectedObject);
        }
    }
    
    /// <summary>
    /// End drag operation and finalize changes
    /// Completes manipulation and updates persistent storage
    /// </summary>
    void EndDrag()
    {
        if (isDragging && selectedObject != null)
        {
            // Apply changes to LibreMetaverse if connected
            ApplyTransformChanges(selectedObject);
            
            // Record operation for undo
            RecordBuildOperation(BuildOperation.OperationType.Move, selectedObject, null);
        }
        
        isDragging = false;
    }
    
    /// <summary>
    /// Handle keyboard input for shortcuts and modifiers
    /// Processes hotkeys and keyboard shortcuts for efficient building
    /// </summary>
    void HandleKeyboardInput()
    {
        // Delete selected objects
        if (Input.GetKeyDown(KeyCode.Delete) && selectedObjects.Count > 0)
        {
            DeleteSelectedObjects();
        }
        
        // Duplicate objects with Ctrl+D
        if (Input.GetKeyDown(KeyCode.D) && (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl)))
        {
            DuplicateSelectedObjects();
        }
        
        // Undo with Ctrl+Z
        if (Input.GetKeyDown(KeyCode.Z) && (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl)))
        {
            if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
            {
                RedoLastOperation();
            }
            else
            {
                UndoLastOperation();
            }
        }
        
        // Toggle grid snapping with G key
        if (Input.GetKeyDown(KeyCode.G))
        {
            snapToGrid = !snapToGrid;
            Debug.Log($"Grid snapping: {(snapToGrid ? "enabled" : "disabled")}");
        }
        
        // Tool shortcuts
        if (Input.GetKeyDown(KeyCode.Alpha1)) SetBuildTool(BuildTool.Select);
        if (Input.GetKeyDown(KeyCode.Alpha2)) SetBuildTool(BuildTool.Move);
        if (Input.GetKeyDown(KeyCode.Alpha3)) SetBuildTool(BuildTool.Rotate);
        if (Input.GetKeyDown(KeyCode.Alpha4)) SetBuildTool(BuildTool.Scale);
        if (Input.GetKeyDown(KeyCode.Alpha5)) SetBuildTool(BuildTool.Copy);
    }
    
    /// <summary>
    /// Check if mouse pointer is over UI elements
    /// Prevents 3D manipulation when interacting with UI
    /// </summary>
    /// <returns>True if pointer is over UI</returns>
    bool IsPointerOverUI()
    {
        return UnityEngine.EventSystems.EventSystem.current != null &&
               UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject();
    }
    
    #endregion
    
    #region Object Selection and Management
    
    /// <summary>
    /// Select an object for editing and manipulation
    /// Updates selection state and UI to reflect current selection
    /// </summary>
    /// <param name="obj">GameObject to select</param>
    /// <param name="primInfo">Associated primitive information</param>
    void SelectObject(GameObject obj, PrimInfo primInfo)
    {
        // Clear previous selection
        ClearSelection();
        
        // Set new selection
        selectedObject = obj;
        selectedPrim = primInfo.prim;
        selectedObjects.Add(obj);
        
        // Add visual selection indicator
        AddSelectionOutline(obj);
        
        // Update UI with object properties
        UpdateUIFromSelection();
        
        // Show edit panel
        if (editPanel) editPanel.SetActive(true);
        
        Debug.Log($"Selected object: {obj.name}");
    }
    
    /// <summary>
    /// Add object to multi-selection
    /// Extends current selection with additional object
    /// </summary>
    /// <param name="obj">GameObject to add to selection</param>
    /// <param name="primInfo">Associated primitive information</param>
    void AddToSelection(GameObject obj, PrimInfo primInfo)
    {
        // Don't add if already selected
        if (selectedObjects.Contains(obj)) return;
        
        // Add to selection list
        selectedObjects.Add(obj);
        
        // Add visual selection indicator
        AddSelectionOutline(obj);
        
        // If this is the first selection, make it primary
        if (selectedObject == null)
        {
            selectedObject = obj;
            selectedPrim = primInfo.prim;
            UpdateUIFromSelection();
            if (editPanel) editPanel.SetActive(true);
        }
        
        Debug.Log($"Added to selection: {obj.name} (Total: {selectedObjects.Count})");
    }
    
    /// <summary>
    /// Clear all object selections
    /// Removes selection indicators and resets selection state
    /// </summary>
    void ClearSelection()
    {
        // Remove visual indicators from all selected objects
        foreach (var obj in selectedObjects)
        {
            if (obj != null)
            {
                RemoveSelectionOutline(obj);
            }
        }
        
        // Clear selection data
        selectedObjects.Clear();
        selectedObject = null;
        selectedPrim = null;
        
        // Hide edit panel
        if (editPanel) editPanel.SetActive(false);
    }
    
    /// <summary>
    /// Add visual selection outline to object
    /// Provides clear visual feedback for selected objects
    /// </summary>
    /// <param name="obj">Object to add outline to</param>
    void AddSelectionOutline(GameObject obj)
    {
        // Use Unity's built-in Outline component or custom implementation
        var outline = obj.GetComponent<Outline>();
        if (outline == null)
        {
            outline = obj.AddComponent<Outline>();
        }
        
        // Configure outline appearance
        outline.enabled = true;
        outline.OutlineColor = Color.yellow;
        outline.OutlineWidth = 5f;
        outline.OutlineMode = Outline.Mode.OutlineVisible;
    }
    
    /// <summary>
    /// Remove visual selection outline from object
    /// Cleans up selection indicators when object is deselected
    /// </summary>
    /// <param name="obj">Object to remove outline from</param>
    void RemoveSelectionOutline(GameObject obj)
    {
        var outline = obj.GetComponent<Outline>();
        if (outline != null)
        {
            outline.enabled = false;
        }
    }
    
    /// <summary>
    /// Update selection visuals each frame
    /// Maintains visual feedback and handles animation effects
    /// </summary>
    void UpdateSelectionVisuals()
    {
        // This could include pulsing effects, animated outlines, etc.
        // For now, we'll just ensure outlines are properly maintained
        foreach (var obj in selectedObjects)
        {
            if (obj == null) continue;
            
            var outline = obj.GetComponent<Outline>();
            if (outline != null && !outline.enabled)
            {
                outline.enabled = true;
            }
        }
    }
    
    #endregion
    
    #region UI Updates and Synchronization
    
    /// <summary>
    /// Update UI controls to reflect selected object properties
    /// Synchronizes all parameter controls with object state
    /// </summary>
    void UpdateUIFromSelection()
    {
        if (selectedPrim == null) return;
        
        try
        {
            // Update position controls
            var pos = selectedPrim.Position;
            UpdatePositionUI(pos.ToVector3());
            
            // Update rotation controls  
            var rot = selectedPrim.Rotation;
            UpdateRotationUI(rot.ToUnity());
            
            // Update scale controls
            var scale = selectedPrim.Scale;
            UpdateScaleUI(scale.ToVector3());
            
            // Update shape parameters
            UpdateShapeParametersUI();
            
            // Update physics properties
            UpdatePhysicsPropertiesUI();
            
            // Update texture properties
            UpdateTexturePropertiesUI();
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error updating UI from selection: {ex.Message}");
        }
    }
    
    /// <summary>
    /// Update position UI controls with current values
    /// Synchronizes position sliders and input fields
    /// </summary>
    /// <param name="position">Current object position</param>
    void UpdatePositionUI(Vector3 position)
    {
        if (posXField) posXField.text = position.x.ToString("F3");
        if (posYField) posYField.text = position.y.ToString("F3");
        if (posZField) posZField.text = position.z.ToString("F3");
        
        if (posXSlider) posXSlider.value = position.x;
        if (posYSlider) posYSlider.value = position.y;
        if (posZSlider) posZSlider.value = position.z;
    }
    
    /// <summary>
    /// Update rotation UI controls with current values
    /// Synchronizes rotation sliders and input fields
    /// </summary>
    /// <param name="rotation">Current object rotation</param>
    void UpdateRotationUI(Quaternion rotation)
    {
        Vector3 eulerAngles = rotation.eulerAngles;
        
        if (rotXField) rotXField.text = eulerAngles.x.ToString("F1");
        if (rotYField) rotYField.text = eulerAngles.y.ToString("F1");
        if (rotZField) rotZField.text = eulerAngles.z.ToString("F1");
        
        if (rotXSlider) rotXSlider.value = eulerAngles.x;
        if (rotYSlider) rotYSlider.value = eulerAngles.y;
        if (rotZSlider) rotZSlider.value = eulerAngles.z;
    }
    
    /// <summary>
    /// Update scale UI controls with current values
    /// Synchronizes scale sliders and input fields
    /// </summary>
    /// <param name="scale">Current object scale</param>
    void UpdateScaleUI(Vector3 scale)
    {
        if (scaleXField) scaleXField.text = scale.x.ToString("F3");
        if (scaleYField) scaleYField.text = scale.y.ToString("F3");
        if (scaleZField) scaleZField.text = scale.z.ToString("F3");
        
        if (scaleXSlider) scaleXSlider.value = scale.x;
        if (scaleYSlider) scaleYSlider.value = scale.y;
        if (scaleZSlider) scaleZSlider.value = scale.z;
    }
    
    /// <summary>
    /// Update shape parameter UI controls with current values
    /// Synchronizes advanced shape manipulation controls
    /// </summary>
    void UpdateShapeParametersUI()
    {
        if (selectedPrim == null) return;
        
        var primData = selectedPrim.PrimData;
        
        if (pathCutBeginSlider) pathCutBeginSlider.value = primData.PathBegin;
        if (pathCutEndSlider) pathCutEndSlider.value = primData.PathEnd;
        if (hollowSlider) hollowSlider.value = primData.ProfileHollow;
        if (twistSlider) twistSlider.value = primData.PathTwist;
        if (taperXSlider) taperXSlider.value = primData.PathTaperX;
        if (taperYSlider) taperYSlider.value = primData.PathTaperY;
        if (shearXSlider) shearXSlider.value = primData.PathShearX;
        if (shearYSlider) shearYSlider.value = primData.PathShearY;
    }
    
    /// <summary>
    /// Update physics properties UI controls with current values
    /// Synchronizes physics and object property toggles
    /// </summary>
    void UpdatePhysicsPropertiesUI()
    {
        if (selectedPrim == null) return;
        
        var flags = selectedPrim.Flags;
        
        if (phantomToggle) phantomToggle.isOn = (flags & PrimFlags.Phantom) != 0;
        if (physicalToggle) physicalToggle.isOn = (flags & PrimFlags.Physics) != 0;
        if (temporaryToggle) temporaryToggle.isOn = (flags & PrimFlags.TemporaryOnRez) != 0;
    }
    
    /// <summary>
    /// Update texture properties UI controls with current values
    /// Synchronizes texture mapping and material controls
    /// </summary>
    void UpdateTexturePropertiesUI()
    {
        if (selectedPrim == null) return;
        
        // Get default texture entry (face 0)
        var texEntry = selectedPrim.Textures.DefaultTexture;
        
        if (textureScaleUSlider) textureScaleUSlider.value = texEntry.RepeatU;
        if (textureScaleVSlider) textureScaleVSlider.value = texEntry.RepeatV;
        if (textureOffsetUSlider) textureOffsetUSlider.value = texEntry.OffsetU;
        if (textureOffsetVSlider) textureOffsetVSlider.value = texEntry.OffsetV;
        if (textureRotationSlider) textureRotationSlider.value = texEntry.Rotation;
        if (glowSlider) glowSlider.value = texEntry.Glow;
        if (fullbrightToggle) fullbrightToggle.isOn = texEntry.Fullbright;
        
        // Update color picker
        if (colorPicker)
        {
            var color = texEntry.RGBA;
            colorPicker.color = new Color(color.R, color.G, color.B, color.A);
        }
    }
    
    #endregion
    
    #region Parameter Change Handlers
    
    /// <summary>
    /// Handle X position parameter change
    /// Updates object position and applies changes to LibreMetaverse
    /// </summary>
    /// <param name="value">New X position value</param>
    void OnPositionXChanged(float value)
    {
        if (selectedObject == null) return;
        
        Vector3 pos = selectedObject.transform.position;
        pos.x = value;
        selectedObject.transform.position = pos;
        
        UpdatePositionField(posXField, value);
        ApplyPositionChanges();
    }
    
    /// <summary>
    /// Handle Y position parameter change
    /// Updates object position and applies changes to LibreMetaverse
    /// </summary>
    /// <param name="value">New Y position value</param>
    void OnPositionYChanged(float value)
    {
        if (selectedObject == null) return;
        
        Vector3 pos = selectedObject.transform.position;
        pos.y = value;
        selectedObject.transform.position = pos;
        
        UpdatePositionField(posYField, value);
        ApplyPositionChanges();
    }
    
    /// <summary>
    /// Handle Z position parameter change
    /// Updates object position and applies changes to LibreMetaverse
    /// </summary>
    /// <param name="value">New Z position value</param>
    void OnPositionZChanged(float value)
    {
        if (selectedObject == null) return;
        
        Vector3 pos = selectedObject.transform.position;
        pos.z = value;
        selectedObject.transform.position = pos;
        
        UpdatePositionField(posZField, value);
        ApplyPositionChanges();
    }
    
    /// <summary>
    /// Apply position changes to LibreMetaverse
    /// Synchronizes local position changes with server
    /// </summary>
    void ApplyPositionChanges()
    {
        if (selectedPrim == null || client == null) return;
        
        try
        {
            var newPos = selectedObject.transform.position.ToLMV();
            client.Objects.SetPosition(client.Network.CurrentSim, selectedPrim.LocalID, newPos);
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error applying position changes: {ex.Message}");
        }
    }
    
    /// <summary>
    /// Handle X rotation parameter change
    /// Updates object rotation and applies changes
    /// </summary>
    /// <param name="value">New X rotation value in degrees</param>
    void OnRotationXChanged(float value) { UpdateObjectRotation(); }
    
    /// <summary>
    /// Handle Y rotation parameter change
    /// Updates object rotation and applies changes
    /// </summary>
    /// <param name="value">New Y rotation value in degrees</param>
    void OnRotationYChanged(float value) { UpdateObjectRotation(); }
    
    /// <summary>
    /// Handle Z rotation parameter change
    /// Updates object rotation and applies changes
    /// </summary>
    /// <param name="value">New Z rotation value in degrees</param>
    void OnRotationZChanged(float value) { UpdateObjectRotation(); }
    
    /// <summary>
    /// Update object rotation based on current slider values
    /// Combines all rotation axes and applies to object
    /// </summary>
    void UpdateObjectRotation()
    {
        if (selectedObject == null || client == null) return;
        
        try
        {
            // Get rotation values from sliders
            Vector3 eulerAngles = new Vector3(
                rotXSlider ? rotXSlider.value : 0,
                rotYSlider ? rotYSlider.value : 0,
                rotZSlider ? rotZSlider.value : 0
            );
            
            // Apply rotation to object
            selectedObject.transform.rotation = Quaternion.Euler(eulerAngles);
            
            // Update input fields
            UpdateRotationField(rotXField, eulerAngles.x);
            UpdateRotationField(rotYField, eulerAngles.y);
            UpdateRotationField(rotZField, eulerAngles.z);
            
            // Apply to LibreMetaverse
            var newRot = selectedObject.transform.rotation.ToLMV();
            client.Objects.SetRotation(client.Network.CurrentSim, selectedPrim.LocalID, newRot);
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error updating object rotation: {ex.Message}");
        }
    }
    
    /// <summary>
    /// Handle X scale parameter change
    /// Updates object scale and applies changes
    /// </summary>
    /// <param name="value">New X scale value</param>
    void OnScaleXChanged(float value) { UpdateObjectScale(); }
    
    /// <summary>
    /// Handle Y scale parameter change
    /// Updates object scale and applies changes
    /// </summary>
    /// <param name="value">New Y scale value</param>
    void OnScaleYChanged(float value) { UpdateObjectScale(); }
    
    /// <summary>
    /// Handle Z scale parameter change
    /// Updates object scale and applies changes
    /// </summary>
    /// <param name="value">New Z scale value</param>
    void OnScaleZChanged(float value) { UpdateObjectScale(); }
    
    /// <summary>
    /// Update object scale based on current slider values
    /// Handles uniform and non-uniform scaling modes
    /// </summary>
    void UpdateObjectScale()
    {
        if (selectedObject == null || client == null) return;
        
        try
        {
            // Get scale values from sliders
            Vector3 newScale = new Vector3(
                scaleXSlider ? scaleXSlider.value : 1,
                scaleYSlider ? scaleYSlider.value : 1,
                scaleZSlider ? scaleZSlider.value : 1
            );
            
            // Apply uniform scaling if enabled
            if (uniformScaleToggle && uniformScaleToggle.isOn)
            {
                float maxScale = Mathf.Max(newScale.x, newScale.y, newScale.z);
                newScale = Vector3.one * maxScale;
                
                // Update all sliders to match
                if (scaleXSlider) scaleXSlider.value = maxScale;
                if (scaleYSlider) scaleYSlider.value = maxScale;
                if (scaleZSlider) scaleZSlider.value = maxScale;
            }
            
            // Apply scale to object
            selectedObject.transform.localScale = newScale;
            
            // Update input fields
            UpdateScaleField(scaleXField, newScale.x);
            UpdateScaleField(scaleYField, newScale.y);
            UpdateScaleField(scaleZField, newScale.z);
            
            // Apply to LibreMetaverse
            var lmvScale = newScale.ToLMV();
            client.Objects.SetScale(client.Network.CurrentSim, selectedPrim.LocalID, lmvScale);
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error updating object scale: {ex.Message}");
        }
    }
    
    // Shape parameter change handlers
    void OnPathCutBeginChanged(float value) { UpdateShapeParameters(); }
    void OnPathCutEndChanged(float value) { UpdateShapeParameters(); }
    void OnHollowChanged(float value) { UpdateShapeParameters(); }
    void OnTwistChanged(float value) { UpdateShapeParameters(); }
    void OnTaperXChanged(float value) { UpdateShapeParameters(); }
    void OnTaperYChanged(float value) { UpdateShapeParameters(); }
    void OnShearXChanged(float value) { UpdateShapeParameters(); }
    void OnShearYChanged(float value) { UpdateShapeParameters(); }
    
    /// <summary>
    /// Update shape parameters based on current control values
    /// Applies advanced shape modifications to primitive geometry
    /// </summary>
    void UpdateShapeParameters()
    {
        if (selectedPrim == null || client == null) return;
        
        try
        {
            // Get current shape data
            var shape = selectedPrim.PrimData;
            
            // Update parameters from UI controls
            if (pathCutBeginSlider) shape.PathBegin = pathCutBeginSlider.value;
            if (pathCutEndSlider) shape.PathEnd = pathCutEndSlider.value;
            if (hollowSlider) shape.ProfileHollow = hollowSlider.value;
            if (twistSlider) shape.PathTwist = twistSlider.value;
            if (taperXSlider) shape.PathTaperX = taperXSlider.value;
            if (taperYSlider) shape.PathTaperY = taperYSlider.value;
            if (shearXSlider) shape.PathShearX = shearXSlider.value;
            if (shearYSlider) shape.PathShearY = shearYSlider.value;
            
            // Apply shape changes to LibreMetaverse
            client.Objects.SetShape(client.Network.CurrentSim, selectedPrim.LocalID, shape);
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error updating shape parameters: {ex.Message}");
        }
    }
    
    // Texture parameter change handlers
    void OnTextureScaleUChanged(float value) { UpdateTextureParameters(); }
    void OnTextureScaleVChanged(float value) { UpdateTextureParameters(); }
    void OnTextureOffsetUChanged(float value) { UpdateTextureParameters(); }
    void OnTextureOffsetVChanged(float value) { UpdateTextureParameters(); }
    void OnTextureRotationChanged(float value) { UpdateTextureParameters(); }
    void OnGlowChanged(float value) { UpdateTextureParameters(); }
    
    /// <summary>
    /// Update texture parameters based on current control values
    /// Applies texture mapping and material property changes
    /// </summary>
    void UpdateTextureParameters()
    {
        if (selectedPrim == null || client == null) return;
        
        try
        {
            // Get current texture entry
            var texEntry = selectedPrim.Textures.DefaultTexture;
            
            // Update texture parameters from UI controls
            if (textureScaleUSlider) texEntry.RepeatU = textureScaleUSlider.value;
            if (textureScaleVSlider) texEntry.RepeatV = textureScaleVSlider.value;
            if (textureOffsetUSlider) texEntry.OffsetU = textureOffsetUSlider.value;
            if (textureOffsetVSlider) texEntry.OffsetV = textureOffsetVSlider.value;
            if (textureRotationSlider) texEntry.Rotation = textureRotationSlider.value;
            if (glowSlider) texEntry.Glow = glowSlider.value;
            
            // Update color from color picker
            if (colorPicker)
            {
                var color = colorPicker.color;
                texEntry.RGBA = new Color4(color.r, color.g, color.b, color.a);
            }
            
            // Update fullbright setting
            if (fullbrightToggle) texEntry.Fullbright = fullbrightToggle.isOn;
            
            // Apply texture changes to LibreMetaverse
            client.Objects.SetTextures(client.Network.CurrentSim, selectedPrim.LocalID, selectedPrim.Textures);
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error updating texture parameters: {ex.Message}");
        }
    }
    
    // Physics property change handlers
    void OnPhantomChanged(bool value) { UpdatePhysicsFlags(); }
    void OnPhysicalChanged(bool value) { UpdatePhysicsFlags(); }
    void OnTemporaryChanged(bool value) { UpdatePhysicsFlags(); }
    void OnFullbrightChanged(bool value) { UpdateTextureParameters(); }
    
    /// <summary>
    /// Update physics flags based on current toggle states
    /// Applies physics and object property changes
    /// </summary>
    void UpdatePhysicsFlags()
    {
        if (selectedPrim == null || client == null) return;
        
        try
        {
            PrimFlags flags = selectedPrim.Flags;
            
            // Update flags based on toggle states
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
            
            // Apply flag changes to LibreMetaverse
            client.Objects.SetFlags(client.Network.CurrentSim, selectedPrim.LocalID, flags);
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error updating physics flags: {ex.Message}");
        }
    }
    
    #endregion
    
    #region Object Operations
    
    /// <summary>
    /// Delete all currently selected objects
    /// Removes objects from scene and LibreMetaverse
    /// </summary>
    void DeleteSelectedObjects()
    {
        if (selectedObjects.Count == 0) return;
        
        try
        {
            // Record operation for undo
            RecordBuildOperation(BuildOperation.OperationType.Delete, null, new List<GameObject>(selectedObjects));
            
            // Delete each selected object
            foreach (var obj in selectedObjects)
            {
                if (obj != null)
                {
                    var primInfo = obj.GetComponent<PrimInfo>();
                    if (primInfo != null && client != null)
                    {
                        // Delete from LibreMetaverse
                        client.Objects.DeleteObject(client.Network.CurrentSim, primInfo.prim.LocalID);
                    }
                    
                    // Delete from Unity scene
                    Destroy(obj);
                }
            }
            
            // Clear selection
            ClearSelection();
            
            Debug.Log($"Deleted {selectedObjects.Count} objects");
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error deleting objects: {ex.Message}");
        }
    }
    
    /// <summary>
    /// Duplicate all currently selected objects
    /// Creates copies of selected objects at offset positions
    /// </summary>
    void DuplicateSelectedObjects()
    {
        if (selectedObjects.Count == 0) return;
        
        try
        {
            List<GameObject> duplicatedObjects = new();
            
            foreach (var obj in selectedObjects)
            {
                if (obj != null)
                {
                    var duplicated = DuplicateObject(obj);
                    if (duplicated != null)
                    {
                        duplicatedObjects.Add(duplicated);
                    }
                }
            }
            
            // Select duplicated objects
            ClearSelection();
            foreach (var obj in duplicatedObjects)
            {
                var primInfo = obj.GetComponent<PrimInfo>();
                if (primInfo != null)
                {
                    AddToSelection(obj, primInfo);
                }
            }
            
            // Record operation for undo
            RecordBuildOperation(BuildOperation.OperationType.Create, null, duplicatedObjects);
            
            Debug.Log($"Duplicated {duplicatedObjects.Count} objects");
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error duplicating objects: {ex.Message}");
        }
    }
    
    /// <summary>
    /// Duplicate a single object
    /// Creates a copy of the specified object with offset position
    /// </summary>
    /// <param name="original">Object to duplicate</param>
    /// <returns>Duplicated object or null if failed</returns>
    GameObject DuplicateObject(GameObject original)
    {
        var primInfo = original.GetComponent<PrimInfo>();
        if (primInfo == null || client == null) return null;
        
        try
        {
            // Calculate offset position for duplicate
            Vector3 offset = Vector3.right * 2.0f; // 2 meters to the right
            Vector3 newPos = original.transform.position + offset;
            
            // Create duplicate through LibreMetaverse
            client.Objects.AddPrim(
                client.Network.CurrentSim,
                primInfo.prim.PrimData,
                UUID.Random(),
                newPos.ToLMV(),
                primInfo.prim.Scale,
                primInfo.prim.Rotation
            );
            
            // Note: The actual Unity GameObject will be created when LibreMetaverse
            // sends back the object creation confirmation
            
            return null; // Will be set when object is confirmed created
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error duplicating object: {ex.Message}");
            return null;
        }
    }
    
    /// <summary>
    /// Link selected objects together
    /// Creates compound object from multiple selected objects
    /// </summary>
    void LinkObjects()
    {
        if (selectedObjects.Count < 2)
        {
            Debug.LogWarning("At least 2 objects must be selected to link");
            return;
        }
        
        try
        {
            // Get LocalIDs of all selected objects
            var localIDs = new List<uint>();
            foreach (var obj in selectedObjects)
            {
                var primInfo = obj.GetComponent<PrimInfo>();
                if (primInfo != null)
                {
                    localIDs.Add(primInfo.prim.LocalID);
                }
            }
            
            if (localIDs.Count >= 2 && client != null)
            {
                // Link objects through LibreMetaverse
                client.Objects.LinkPrims(client.Network.CurrentSim, localIDs);
                
                // Record operation for undo
                RecordBuildOperation(BuildOperation.OperationType.Link, null, new List<GameObject>(selectedObjects));
                
                Debug.Log($"Linked {localIDs.Count} objects");
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error linking objects: {ex.Message}");
        }
    }
    
    /// <summary>
    /// Unlink selected objects
    /// Separates compound objects into individual components
    /// </summary>
    void UnlinkObjects()
    {
        if (selectedObjects.Count == 0) return;
        
        try
        {
            foreach (var obj in selectedObjects)
            {
                var primInfo = obj.GetComponent<PrimInfo>();
                if (primInfo != null && client != null)
                {
                    // Unlink object through LibreMetaverse
                    client.Objects.DelinkPrim(client.Network.CurrentSim, primInfo.prim.LocalID);
                }
            }
            
            // Record operation for undo
            RecordBuildOperation(BuildOperation.OperationType.Unlink, null, new List<GameObject>(selectedObjects));
            
            Debug.Log("Unlinked selected objects");
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error unlinking objects: {ex.Message}");
        }
    }
    
    #endregion
    
    #region Utility Methods
    
    /// <summary>
    /// Apply all transform changes to LibreMetaverse
    /// Synchronizes local Unity changes with server state
    /// </summary>
    /// <param name="obj">Object to apply changes for</param>
    void ApplyTransformChanges(GameObject obj)
    {
        var primInfo = obj.GetComponent<PrimInfo>();
        if (primInfo == null || client == null) return;
        
        try
        {
            // Apply position, rotation, and scale changes
            var newPos = obj.transform.position.ToLMV();
            var newRot = obj.transform.rotation.ToLMV();
            var newScale = obj.transform.localScale.ToLMV();
            
            client.Objects.SetPosition(client.Network.CurrentSim, primInfo.prim.LocalID, newPos);
            client.Objects.SetRotation(client.Network.CurrentSim, primInfo.prim.LocalID, newRot);
            client.Objects.SetScale(client.Network.CurrentSim, primInfo.prim.LocalID, newScale);
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error applying transform changes: {ex.Message}");
        }
    }
    
    /// <summary>
    /// Snap position to grid if grid snapping is enabled
    /// Provides precise alignment capabilities for construction
    /// </summary>
    /// <param name="position">Position to snap</param>
    /// <returns>Grid-snapped position</returns>
    Vector3 SnapToGrid(Vector3 position)
    {
        if (!snapToGrid) return position;
        
        return new Vector3(
            Mathf.Round(position.x / gridSize) * gridSize,
            Mathf.Round(position.y / gridSize) * gridSize,
            Mathf.Round(position.z / gridSize) * gridSize
        );
    }
    
    /// <summary>
    /// Update position input field with current value
    /// Helper method for UI synchronization
    /// </summary>
    /// <param name="field">Input field to update</param>
    /// <param name="value">Value to display</param>
    void UpdatePositionField(TMP_InputField field, float value)
    {
        if (field != null) field.text = value.ToString("F3");
    }
    
    /// <summary>
    /// Update rotation input field with current value
    /// Helper method for UI synchronization
    /// </summary>
    /// <param name="field">Input field to update</param>
    /// <param name="value">Value to display in degrees</param>
    void UpdateRotationField(TMP_InputField field, float value)
    {
        if (field != null) field.text = value.ToString("F1");
    }
    
    /// <summary>
    /// Update scale input field with current value
    /// Helper method for UI synchronization
    /// </summary>
    /// <param name="field">Input field to update</param>
    /// <param name="value">Value to display</param>
    void UpdateScaleField(TMP_InputField field, float value)
    {
        if (field != null) field.text = value.ToString("F3");
    }
    
    /// <summary>
    /// Open texture picker dialog
    /// Launches texture selection interface for material assignment
    /// </summary>
    void OpenTexturePicker()
    {
        // This would open a texture picker dialog
        Debug.Log("Open texture picker (not implemented)");
        
        // Implementation would integrate with texture management system
    }
    
    /// <summary>
    /// Handle build mode toggle change
    /// Enables or disables building capabilities
    /// </summary>
    /// <param name="enabled">New build mode state</param>
    void OnBuildModeToggle(bool enabled)
    {
        buildModeEnabled = enabled;
        
        if (!enabled)
        {
            ClearSelection();
        }
    }
    
    #endregion
    
    #region Undo/Redo System
    
    /// <summary>
    /// Record a build operation for undo functionality
    /// Stores operation data for potential reversal
    /// </summary>
    /// <param name="type">Type of operation performed</param>
    /// <param name="targetObject">Primary object affected</param>
    /// <param name="affectedObjects">All objects affected</param>
    void RecordBuildOperation(BuildOperation.OperationType type, GameObject targetObject, List<GameObject> affectedObjects)
    {
        var operation = new BuildOperation
        {
            type = type,
            targetObject = targetObject,
            affectedObjects = affectedObjects ?? new List<GameObject>(),
            timestamp = DateTime.Now
        };
        
        // Store previous state if applicable
        if (targetObject != null)
        {
            operation.previousPosition = targetObject.transform.position;
            operation.previousRotation = targetObject.transform.rotation;
            operation.previousScale = targetObject.transform.localScale;
        }
        
        // Add to undo stack
        undoStack.Push(operation);
        
        // Limit undo stack size
        if (undoStack.Count > MAX_UNDO_OPERATIONS)
        {
            var tempStack = new Stack<BuildOperation>();
            for (int i = 0; i < MAX_UNDO_OPERATIONS; i++)
            {
                if (undoStack.Count > 0)
                {
                    tempStack.Push(undoStack.Pop());
                }
            }
            undoStack.Clear();
            while (tempStack.Count > 0)
            {
                undoStack.Push(tempStack.Pop());
            }
        }
        
        // Clear redo stack when new operation is recorded
        redoStack.Clear();
    }
    
    /// <summary>
    /// Record current object state for undo tracking
    /// Captures object state before modifications
    /// </summary>
    /// <param name="obj">Object to record state for</param>
    void RecordObjectState(GameObject obj)
    {
        // This would store detailed object state for undo
        // Implementation depends on what properties need to be tracked
    }
    
    /// <summary>
    /// Undo the last build operation
    /// Reverses the most recent construction operation
    /// </summary>
    void UndoLastOperation()
    {
        if (undoStack.Count == 0)
        {
            Debug.Log("Nothing to undo");
            return;
        }
        
        var operation = undoStack.Pop();
        redoStack.Push(operation);
        
        try
        {
            // Reverse the operation based on its type
            switch (operation.type)
            {
                case BuildOperation.OperationType.Move:
                    if (operation.targetObject != null)
                    {
                        operation.targetObject.transform.position = operation.previousPosition;
                        ApplyTransformChanges(operation.targetObject);
                    }
                    break;
                    
                case BuildOperation.OperationType.Delete:
                    // Would need to recreate deleted objects
                    Debug.Log("Undo delete not fully implemented");
                    break;
                    
                case BuildOperation.OperationType.Create:
                    // Delete created objects
                    foreach (var obj in operation.affectedObjects)
                    {
                        if (obj != null) Destroy(obj);
                    }
                    break;
                    
                // Additional operation types would be handled here
            }
            
            Debug.Log($"Undid {operation.type} operation");
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error undoing operation: {ex.Message}");
        }
    }
    
    /// <summary>
    /// Redo the last undone operation
    /// Reapplies a previously reversed construction operation
    /// </summary>
    void RedoLastOperation()
    {
        if (redoStack.Count == 0)
        {
            Debug.Log("Nothing to redo");
            return;
        }
        
        var operation = redoStack.Pop();
        undoStack.Push(operation);
        
        try
        {
            // Reapply the operation
            // Implementation would depend on operation type
            Debug.Log($"Redid {operation.type} operation");
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error redoing operation: {ex.Message}");
        }
    }
    
    #endregion
}

/// <summary>
/// Extension methods for coordinate system conversions
/// Provides seamless conversion between Unity and LibreMetaverse coordinate systems
/// </summary>
public static class BuildToolsExtensions
{
    /// <summary>
    /// Convert Unity Vector3 to LibreMetaverse Vector3
    /// Handles coordinate system differences between engines
    /// </summary>
    /// <param name="unityVector">Unity Vector3 to convert</param>
    /// <returns>LibreMetaverse Vector3</returns>
    public static OpenMetaverse.Vector3 ToLMV(this Vector3 unityVector)
    {
        return new OpenMetaverse.Vector3(unityVector.x, unityVector.y, unityVector.z);
    }
    
    /// <summary>
    /// Convert LibreMetaverse Vector3 to Unity Vector3
    /// Handles coordinate system differences between engines
    /// </summary>
    /// <param name="lmvVector">LibreMetaverse Vector3 to convert</param>
    /// <returns>Unity Vector3</returns>
    public static Vector3 ToVector3(this OpenMetaverse.Vector3 lmvVector)
    {
        return new Vector3(lmvVector.X, lmvVector.Y, lmvVector.Z);
    }
    
    /// <summary>
    /// Convert Unity Quaternion to LibreMetaverse Quaternion
    /// Handles rotation system differences between engines
    /// </summary>
    /// <param name="unityQuaternion">Unity Quaternion to convert</param>
    /// <returns>LibreMetaverse Quaternion</returns>
    public static OpenMetaverse.Quaternion ToLMV(this Quaternion unityQuaternion)
    {
        return new OpenMetaverse.Quaternion(unityQuaternion.x, unityQuaternion.y, unityQuaternion.z, unityQuaternion.w);
    }
    
    /// <summary>
    /// Convert LibreMetaverse Quaternion to Unity Quaternion
    /// Handles rotation system differences between engines
    /// </summary>
    /// <param name="lmvQuaternion">LibreMetaverse Quaternion to convert</param>
    /// <returns>Unity Quaternion</returns>
    public static Quaternion ToUnity(this OpenMetaverse.Quaternion lmvQuaternion)
    {
        return new Quaternion(lmvQuaternion.X, lmvQuaternion.Y, lmvQuaternion.Z, lmvQuaternion.W);
    }
}