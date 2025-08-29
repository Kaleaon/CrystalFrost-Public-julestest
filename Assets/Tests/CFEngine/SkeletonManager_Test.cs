using NUnit.Framework;
using CrystalFrost.WorldState;
using System.Linq;
using UnityEngine;

namespace CrystalFrostEngine.Tests
{
    public class SkeletonManager_Test
    {
        private ISkeletonManager _skeletonManager;

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            // The SkeletonManager loads its data in the constructor,
            // so we only need to create it once for all tests.
            _skeletonManager = new SkeletonManager();
        }

        [Test]
        public void SkeletonManager_LoadsAllBones()
        {
            // The avatar_skeleton.xml file specifies num_bones="133"
            Assert.AreEqual(133, _skeletonManager.JointList.Count);
        }

        [Test]
        public void SkeletonManager_CorrectlyParsesHierarchy()
        {
            // Test a few key parent-child relationships
            var mHead = _skeletonManager.GetJoint("mHead");
            var mNeck = _skeletonManager.GetJoint("mNeck");
            var mChest = _skeletonManager.GetJoint("mChest");

            Assert.IsNotNull(mHead);
            Assert.IsNotNull(mNeck);
            Assert.IsNotNull(mChest);

            var neckIndex = _skeletonManager.JointList.IndexOf(mNeck);
            var chestIndex = _skeletonManager.JointList.IndexOf(mChest);

            // mHead's parent should be mNeck
            Assert.AreEqual(neckIndex, mHead.Parent);
            // mNeck's parent should be mChest
            Assert.AreEqual(chestIndex, mNeck.Parent);
        }

        [Test]
        public void SkeletonManager_ParsesSkinOffset()
        {
            // Test the pivot (SkinOffset) for mPelvis
            var mPelvis = _skeletonManager.GetJoint("mPelvis");
            Assert.IsNotNull(mPelvis);

            // pivot="0.000000 0.000000 1.067015"
            var expectedOffset = new Vector3(0.0f, 0.0f, 1.067015f);

            // Using a tolerance for floating point comparison
            Assert.That(mPelvis.SkinOffset.x, Is.EqualTo(expectedOffset.x).Within(0.00001));
            Assert.That(mPelvis.SkinOffset.y, Is.EqualTo(expectedOffset.y).Within(0.00001));
            Assert.That(mPelvis.SkinOffset.z, Is.EqualTo(expectedOffset.z).Within(0.00001));
        }

        [Test]
        public void SkeletonManager_HandlesRootBone()
        {
            // The root bone, mPelvis, should have a parent index of -1
            var mPelvis = _skeletonManager.GetJoint("mPelvis");
            Assert.IsNotNull(mPelvis);
            Assert.AreEqual(-1, mPelvis.Parent);
        }
    }
}
