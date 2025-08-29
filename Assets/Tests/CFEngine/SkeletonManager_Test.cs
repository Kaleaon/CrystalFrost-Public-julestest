using NUnit.Framework;
using CrystalFrost.WorldState;
using System.Linq;
using UnityEngine;
using Moq;
using OpenMetaverse.Assets;
using OpenMetaverse.StructuredData;
using OpenMetaverse;

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
            // We also need to register it with the service locator for the integration test.
            _skeletonManager = new SkeletonManager();
            CrystalFrost.Services.RegisterService<ISkeletonManager>(_skeletonManager);
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

        [Test]
        public void RiggedMesh_Integration_BuildsCorrectHierarchy()
        {
            // This is an integration test to ensure RiggedMesh uses the SkeletonManager correctly.

            // Arrange
            // 1. Mock the AssetMesh
            var meshAsset = new Mock<AssetMesh>();
            var meshData = new OSDMap();
            var skinMap = new OSDMap();

            // Create a simple joint name list for our test mesh
            var jointNames = new OSDArray();
            jointNames.Add("mHead");
            jointNames.Add("mNeck");
            jointNames.Add("mChest");

            // Create a dummy inverse bind matrix array
            var invBindMatrices = new OSDArray();
            var dummyMatrix = new OSDArray();
            for(int i = 0; i < 16; i++) dummyMatrix.Add(OSD.FromReal(1.0));
            invBindMatrices.Add(dummyMatrix);
            invBindMatrices.Add(dummyMatrix);
            invBindMatrices.Add(dummyMatrix);

            skinMap["joint_names"] = jointNames;
            skinMap["inverse_bind_matrix"] = invBindMatrices;
            meshData["skin"] = skinMap;

            // We need a minimal set of other data for the parser to not throw an exception
            meshData["high_lod"] = new OSDArray();

            // The Decode method needs to return true and set the MeshData property
            meshAsset.Setup(m => m.Decode()).Returns(true);
            meshAsset.Setup(m => m.MeshData).Returns(meshData);

            // 2. Mock the Primitive
            var prim = new Primitive();

            // Act
            bool success = OpenMetaverse.Rendering.RiggedMesh.TryDecodeFromAsset(prim, meshAsset.Object, DetailLevel.Highest, out var resultMesh);

            // Assert
            Assert.IsTrue(success);
            Assert.IsNotNull(resultMesh);
            Assert.AreEqual(3, resultMesh.Joints.Length);

            // Check names
            Assert.AreEqual("mHead", resultMesh.Joints[0].Name);
            Assert.AreEqual("mNeck", resultMesh.Joints[1].Name);
            Assert.AreEqual("mChest", resultMesh.Joints[2].Name);

            // Check remapped parent indices
            // mHead's parent should be mNeck (index 1 in the mesh-specific list)
            Assert.AreEqual(1, resultMesh.Joints[0].Parent);
            // mNeck's parent should be mChest (index 2 in the mesh-specific list)
            Assert.AreEqual(2, resultMesh.Joints[1].Parent);
            // mChest's parent is mSpine4, which is not in our list, so it should be -1 (a root)
            Assert.AreEqual(-1, resultMesh.Joints[2].Parent);
        }
    }
}
