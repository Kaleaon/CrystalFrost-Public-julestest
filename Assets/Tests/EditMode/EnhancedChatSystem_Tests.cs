using NUnit.Framework;
using UnityEngine;
using OpenMetaverse;
using System.Reflection;
using System.Collections;

namespace CrystalFrost.Tests
{
    public class EnhancedChatSystemTests
    {
        private GameObject chatSystemObject;
        private EnhancedChatSystem chatSystem;

        [SetUp]
        public void Setup()
        {
            chatSystemObject = new GameObject("EnhancedChatSystem");
            chatSystem = chatSystemObject.AddComponent<EnhancedChatSystem>();

            // Setup mock UI references
            chatSystem.chatPanelPrefab = new GameObject("ChatPanelPrefab");
            var contentObj = new GameObject("Content");
            contentObj.transform.SetParent(chatSystem.chatPanelPrefab.transform);

            chatSystem.chatMessagePrefab = new GameObject("ChatMessagePrefab");
            chatSystem.systemMessagePrefab = new GameObject("SystemMessagePrefab");
            chatSystem.tabButtonsRoot = new GameObject("TabButtonsRoot").transform;
            chatSystem.chatPanelsRoot = new GameObject("ChatPanelsRoot").transform;
            chatSystem.tabButtonPrefab = new GameObject("TabButtonPrefab");

            // Initialize chat system Awake via Reflection
            var awakeMethod = typeof(EnhancedChatSystem).GetMethod("Awake", BindingFlags.NonPublic | BindingFlags.Instance);
            awakeMethod?.Invoke(chatSystem, null);
        }

        [TearDown]
        public void TearDown()
        {
            if (chatSystemObject != null)
            {
                Object.DestroyImmediate(chatSystemObject);
            }
        }

        [Test]
        public void ActiveTab_MessageStreaming_CapsAt1000GameObjects()
        {
            // Stream 1500 messages to Local tab (active tab)
            var addMsgMethod = typeof(EnhancedChatSystem).GetMethod("AddMessageToTab", BindingFlags.NonPublic | BindingFlags.Instance);
            for (int i = 0; i < 1500; i++)
            {
                var msg = new EnhancedChatSystem.ChatMessage
                {
                    senderName = "TestSender",
                    message = $"Test message {i}",
                    type = EnhancedChatSystem.ChatType.Local,
                    timestamp = System.DateTime.Now
                };
                
                addMsgMethod.Invoke(chatSystem, new object[] { "Local", msg });
            }

            // Get active tab from chatTabs dictionary
            var chatTabsField = typeof(EnhancedChatSystem).GetField("chatTabs", BindingFlags.NonPublic | BindingFlags.Instance);
            var chatTabs = (IDictionary)chatTabsField.GetValue(chatSystem);
            var localTab = chatTabs["Local"];
            
            var messagesField = localTab.GetType().GetField("messages");
            var messagesList = (IList)messagesField.GetValue(localTab);
            
            var uiObjectsField = localTab.GetType().GetField("uiObjects");
            var uiObjectsQueue = (ICollection)uiObjectsField.GetValue(localTab);

            var contentRootField = localTab.GetType().GetField("contentRoot");
            var contentRoot = (Transform)contentRootField.GetValue(localTab);

            Assert.AreEqual(1000, messagesList.Count, "Messages count should be capped at 1000");
            Assert.AreEqual(1000, uiObjectsQueue.Count, "UI objects queue count should be capped at 1000");
            Assert.AreEqual(1000, contentRoot.childCount, "Content root child count should never exceed 1000");
        }

        [Test]
        public void BackgroundTab_ReceivingMessages_DefersRenderingAndSetsDirtyFlag()
        {
            // Create a background tab "GroupChat"
            var createTabMethod = typeof(EnhancedChatSystem).GetMethod("CreateChatTab", BindingFlags.NonPublic | BindingFlags.Instance);
            createTabMethod.Invoke(chatSystem, new object[] { "GroupChat", EnhancedChatSystem.ChatType.Group, UUID.Zero });

            // Stream 500 messages to background tab "GroupChat"
            var addMsgMethod = typeof(EnhancedChatSystem).GetMethod("AddMessageToTab", BindingFlags.NonPublic | BindingFlags.Instance);
            for (int i = 0; i < 500; i++)
            {
                var msg = new EnhancedChatSystem.ChatMessage
                {
                    senderName = "GroupSender",
                    message = $"Group message {i}",
                    type = EnhancedChatSystem.ChatType.Group,
                    timestamp = System.DateTime.Now
                };

                addMsgMethod.Invoke(chatSystem, new object[] { "GroupChat", msg });
            }

            var chatTabsField = typeof(EnhancedChatSystem).GetField("chatTabs", BindingFlags.NonPublic | BindingFlags.Instance);
            var chatTabs = (IDictionary)chatTabsField.GetValue(chatSystem);
            var groupTab = chatTabs["GroupChat"];

            var isDirtyField = groupTab.GetType().GetField("isDirty");
            bool isDirty = (bool)isDirtyField.GetValue(groupTab);

            var contentRootField = groupTab.GetType().GetField("contentRoot");
            var contentRoot = (Transform)contentRootField.GetValue(groupTab);

            Assert.IsTrue(isDirty, "Background tab should be marked dirty when receiving messages");
            Assert.AreEqual(0, contentRoot.childCount, "Background tab content root should have 0 instantiated UI GameObjects");

            // Now switch to GroupChat tab
            var switchToTabMethod = typeof(EnhancedChatSystem).GetMethod("SwitchToTab", BindingFlags.NonPublic | BindingFlags.Instance);
            switchToTabMethod.Invoke(chatSystem, new object[] { "GroupChat" });

            bool isDirtyAfterSwitch = (bool)isDirtyField.GetValue(groupTab);
            Assert.IsFalse(isDirtyAfterSwitch, "Tab should no longer be dirty after switching to it");
            Assert.AreEqual(500, contentRoot.childCount, "Content root should rebuild exactly 500 UI GameObjects on switch");
        }
    }
}
