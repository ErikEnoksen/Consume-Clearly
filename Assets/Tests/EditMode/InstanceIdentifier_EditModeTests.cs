using NUnit.Framework;
using Save;
using UnityEngine;

namespace Tests.EditMode
{
    [TestFixture]
    public class InstanceIdentifier_EditModeTests
    {
        private GameObject go1;
        private GameObject go2;

        [TearDown]
        public void TearDown()
        {
            if (go1 != null) Object.DestroyImmediate(go1);
            if (go2 != null) Object.DestroyImmediate(go2);
        }

        [Test]
        public void Id_IsAssignedAfterAwake()
        {
            go1 = new GameObject("Test");
            var identifier = go1.AddComponent<InstanceIdentifier>();
            Assert.IsFalse(string.IsNullOrEmpty(identifier.Id), "Id should be non-empty after Awake");
        }

        [Test]
        public void Id_IsValidGuid()
        {
            go1 = new GameObject("Test");
            var identifier = go1.AddComponent<InstanceIdentifier>();
            Assert.IsTrue(System.Guid.TryParse(identifier.Id, out _), "Id should parse as a valid GUID");
        }

        [Test]
        public void TwoIdentifiers_HaveUniqueIds()
        {
            go1 = new GameObject("Obj1");
            go2 = new GameObject("Obj2");
            var id1 = go1.AddComponent<InstanceIdentifier>();
            var id2 = go2.AddComponent<InstanceIdentifier>();

            Assert.AreNotEqual(id1.Id, id2.Id, "Each InstanceIdentifier should receive a unique GUID");
        }

        [Test]
        public void Id_RemainsStableOnRepeatedAccess()
        {
            go1 = new GameObject("Test");
            var identifier = go1.AddComponent<InstanceIdentifier>();
            string first = identifier.Id;
            string second = identifier.Id;
            Assert.AreEqual(first, second, "Id should not change between accesses");
        }
    }
}