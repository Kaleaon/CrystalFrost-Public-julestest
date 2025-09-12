using UnityEngine;
using UnityEngine.UI;
using OpenMetaverse;
using System.Collections.Generic;
using System;
using TMPro;

namespace CrystalFrost.UI
{
    /// <summary>
    /// UI component for selecting avatar attachment points
    /// Provides intuitive interface for attaching objects to specific body locations
    /// </summary>
    public class AttachmentPointSelectorUI : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private GameObject attachmentPointButtonPrefab;
        [SerializeField] private Transform buttonContainer;
        [SerializeField] private Button closeButton;
        [SerializeField] private TextMeshProUGUI titleText;

        private InventoryItem _currentItem;
        private Action<AttachmentPoint> _onAttachmentSelected;

        // Comprehensive list of avatar attachment points with user-friendly names
        private readonly Dictionary<AttachmentPoint, string> _attachmentPointNames = new()
        {
            { AttachmentPoint.Chest, "Chest" },
            { AttachmentPoint.Skull, "Head" },
            { AttachmentPoint.LeftShoulder, "Left Shoulder" },
            { AttachmentPoint.RightShoulder, "Right Shoulder" },
            { AttachmentPoint.LeftHand, "Left Hand" },
            { AttachmentPoint.RightHand, "Right Hand" },
            { AttachmentPoint.LeftFoot, "Left Foot" },
            { AttachmentPoint.RightFoot, "Right Foot" },
            { AttachmentPoint.Spine, "Spine" },
            { AttachmentPoint.Pelvis, "Pelvis" },
            { AttachmentPoint.Mouth, "Mouth" },
            { AttachmentPoint.Chin, "Chin" },
            { AttachmentPoint.LeftEar, "Left Ear" },
            { AttachmentPoint.RightEar, "Right Ear" },
            { AttachmentPoint.LeftEyeball, "Left Eye" },
            { AttachmentPoint.RightEyeball, "Right Eye" },
            { AttachmentPoint.Nose, "Nose" },
            { AttachmentPoint.RightUpperArm, "Right Upper Arm" },
            { AttachmentPoint.RightForearm, "Right Forearm" },
            { AttachmentPoint.LeftUpperArm, "Left Upper Arm" },
            { AttachmentPoint.LeftForearm, "Left Forearm" },
            { AttachmentPoint.RightUpperLeg, "Right Upper Leg" },
            { AttachmentPoint.RightLowerLeg, "Right Lower Leg" },
            { AttachmentPoint.LeftUpperLeg, "Left Upper Leg" },
            { AttachmentPoint.LeftLowerLeg, "Left Lower Leg" },
            { AttachmentPoint.Stomach, "Stomach" },
            { AttachmentPoint.LeftPec, "Left Pec" },
            { AttachmentPoint.RightPec, "Right Pec" },
            { AttachmentPoint.Center2, "Center 2" },
            { AttachmentPoint.TopRight, "Top Right" },
            { AttachmentPoint.TopLeft, "Top Left" },
            { AttachmentPoint.BottomLeft, "Bottom Left" },
            { AttachmentPoint.BottomRight, "Bottom Right" }
        };

        // Categorized attachment points for better organization
        private readonly Dictionary<string, List<AttachmentPoint>> _attachmentCategories = new()
        {
            { "Head & Face", new List<AttachmentPoint> { 
                AttachmentPoint.Skull, AttachmentPoint.Mouth, AttachmentPoint.Chin, 
                AttachmentPoint.LeftEar, AttachmentPoint.RightEar, AttachmentPoint.LeftEyeball, 
                AttachmentPoint.RightEyeball, AttachmentPoint.Nose 
            }},
            { "Body", new List<AttachmentPoint> { 
                AttachmentPoint.Chest, AttachmentPoint.Spine, AttachmentPoint.Pelvis, 
                AttachmentPoint.Stomach, AttachmentPoint.LeftPec, AttachmentPoint.RightPec 
            }},
            { "Arms & Hands", new List<AttachmentPoint> { 
                AttachmentPoint.LeftShoulder, AttachmentPoint.RightShoulder, AttachmentPoint.LeftHand, 
                AttachmentPoint.RightHand, AttachmentPoint.RightUpperArm, AttachmentPoint.RightForearm,
                AttachmentPoint.LeftUpperArm, AttachmentPoint.LeftForearm 
            }},
            { "Legs & Feet", new List<AttachmentPoint> { 
                AttachmentPoint.LeftFoot, AttachmentPoint.RightFoot, AttachmentPoint.RightUpperLeg,
                AttachmentPoint.RightLowerLeg, AttachmentPoint.LeftUpperLeg, AttachmentPoint.LeftLowerLeg 
            }},
            { "HUD Positions", new List<AttachmentPoint> { 
                AttachmentPoint.Center2, AttachmentPoint.TopRight, AttachmentPoint.TopLeft,
                AttachmentPoint.BottomLeft, AttachmentPoint.BottomRight 
            }}
        };

        private void Start()
        {
            closeButton?.onClick.AddListener(Hide);
            CreateAttachmentPointButtons();
        }

        /// <summary>
        /// Shows the attachment point selector for a specific inventory item
        /// </summary>
        /// <param name="item">The inventory item to attach</param>
        /// <param name="onSelected">Callback when attachment point is selected</param>
        public void Show(InventoryItem item, Action<AttachmentPoint> onSelected)
        {
            _currentItem = item;
            _onAttachmentSelected = onSelected;
            
            if (titleText != null)
                titleText.text = $"Attach '{item.Name}' to:";
            
            gameObject.SetActive(true);
        }

        /// <summary>
        /// Hides the attachment point selector
        /// </summary>
        public void Hide()
        {
            gameObject.SetActive(false);
            _currentItem = null;
            _onAttachmentSelected = null;
        }

        private void CreateAttachmentPointButtons()
        {
            if (attachmentPointButtonPrefab == null || buttonContainer == null)
            {
                Debug.LogError("AttachmentPointSelectorUI: Missing required prefab or container references");
                return;
            }

            foreach (var category in _attachmentCategories)
            {
                // Create category header
                CreateCategoryHeader(category.Key);
                
                // Create buttons for each attachment point in this category
                foreach (var attachmentPoint in category.Value)
                {
                    CreateAttachmentPointButton(attachmentPoint);
                }
            }
        }

        private void CreateCategoryHeader(string categoryName)
        {
            GameObject headerGO = new GameObject($"Category_{categoryName}");
            headerGO.transform.SetParent(buttonContainer);
            
            TextMeshProUGUI headerText = headerGO.AddComponent<TextMeshProUGUI>();
            headerText.text = categoryName;
            headerText.fontSize = 14f;
            headerText.fontStyle = FontStyles.Bold;
            headerText.color = Color.white;
            
            // Add some spacing
            LayoutElement layoutElement = headerGO.AddComponent<LayoutElement>();
            layoutElement.minHeight = 25f;
        }

        private void CreateAttachmentPointButton(AttachmentPoint attachmentPoint)
        {
            GameObject buttonGO = Instantiate(attachmentPointButtonPrefab, buttonContainer);
            Button button = buttonGO.GetComponent<Button>();
            
            if (button == null)
            {
                Debug.LogError("AttachmentPointSelectorUI: Button prefab doesn't have Button component");
                return;
            }

            // Set button text
            TextMeshProUGUI buttonText = buttonGO.GetComponentInChildren<TextMeshProUGUI>();
            if (buttonText != null && _attachmentPointNames.TryGetValue(attachmentPoint, out string pointName))
            {
                buttonText.text = pointName;
            }

            // Add click handler
            button.onClick.AddListener(() => OnAttachmentPointSelected(attachmentPoint));
        }

        private void OnAttachmentPointSelected(AttachmentPoint attachmentPoint)
        {
            if (_currentItem != null && _onAttachmentSelected != null)
            {
                try
                {
                    _onAttachmentSelected.Invoke(attachmentPoint);
                    Hide();
                }
                catch (Exception ex)
                {
                    Debug.LogError($"AttachmentPointSelectorUI: Error attaching item: {ex.Message}");
                }
            }
        }

        private void OnDestroy()
        {
            closeButton?.onClick.RemoveAllListeners();
        }
    }
}