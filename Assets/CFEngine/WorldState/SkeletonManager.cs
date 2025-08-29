using System.Collections.Generic;
using System.Xml;
using UnityEngine;
using OpenMetaverse.Rendering; // For JointInfo

namespace CrystalFrost.WorldState
{
    public interface ISkeletonManager
    {
        IReadOnlyDictionary<string, JointInfo> Joints { get; }
        JointInfo GetJoint(string name);
        List<JointInfo> JointList { get; }
    }

    public class SkeletonManager : ISkeletonManager
    {
        private readonly Dictionary<string, JointInfo> _joints = new();
        private readonly List<JointInfo> _jointList = new();

        public IReadOnlyDictionary<string, JointInfo> Joints => _joints;
        public List<JointInfo> JointList => _jointList;

        public SkeletonManager()
        {
            LoadSkeleton();
        }

        public JointInfo GetJoint(string name)
        {
            _joints.TryGetValue(name, out var joint);
            return joint;
        }

        private void LoadSkeleton()
        {
            // In a real build, this should come from an asset bundle.
            // For now, load directly from the Assets folder.
            var xmlPath = System.IO.Path.Combine(Application.dataPath, "character/avatar_skeleton.xml");
            if (!System.IO.File.Exists(xmlPath))
            {
                Debug.LogError("avatar_skeleton.xml not found at " + xmlPath);
                return;
            }

            var xmlDoc = new XmlDocument();
            xmlDoc.Load(xmlPath);

            var rootNode = xmlDoc.SelectSingleNode("linden_skeleton");
            if (rootNode != null)
            {
                ParseBones(rootNode, -1); // Start with no parent (-1 index)
            }
        }

        private void ParseBones(XmlNode parentNode, int parentIndex)
        {
            foreach (XmlNode node in parentNode.ChildNodes)
            {
                if (node.Name == "bone")
                {
                    var joint = new JointInfo
                    {
                        Name = node.Attributes["name"].Value,
                        Parent = parentIndex
                    };

                    if(node.Attributes["pivot"] != null)
                    {
                        joint.SkinOffset = ParseVector3(node.Attributes["pivot"].Value);
                    }

                    _jointList.Add(joint);
                    var currentIndex = _jointList.Count - 1;
                    _joints[joint.Name] = joint;

                    // Recursively parse children
                    ParseBones(node, currentIndex);
                }
            }
        }

        private Vector3 ParseVector3(string s)
        {
            var parts = s.Split(' ');
            if (parts.Length != 3) return Vector3.zero;

            // Use InvariantCulture to ensure parsing works regardless of system locale
            float.TryParse(parts[0], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var x);
            float.TryParse(parts[1], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var y);
            float.TryParse(parts[2], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var z);

            return new Vector3(x, y, z);
        }
    }
}
