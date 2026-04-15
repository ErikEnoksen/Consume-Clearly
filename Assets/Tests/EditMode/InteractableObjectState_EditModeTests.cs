using NUnit.Framework;
using Save;
using UnityEngine;

namespace Tests.EditMode
{
    [TestFixture]
    public class InteractableObjectState_EditModeTests
    {
        [Test]
        public void DefaultState_HasEmptyUniqueId()
        {
            var state = new InteractableObjectState();
            Assert.IsTrue(string.IsNullOrEmpty(state.uniqueId));
        }

        [Test]
        public void DefaultState_IsActiveIsFalse()
        {
            var state = new InteractableObjectState();
            Assert.IsFalse(state.isActive);
        }

        [Test]
        public void DefaultState_PositionIsZero()
        {
            var state = new InteractableObjectState();
            Assert.AreEqual(Vector3.zero, state.position);
        }

        [Test]
        public void FieldAssignment_PersistsAllValues()
        {
            var state = new InteractableObjectState
            {
                uniqueId = "abc-123",
                isActive = true,
                position = new Vector3(1f, 2f, 3f),
                rotation = Quaternion.Euler(0f, 90f, 0f),
                ropeVariantName = "HeavyRope"
            };

            Assert.AreEqual("abc-123", state.uniqueId);
            Assert.IsTrue(state.isActive);
            Assert.AreEqual(new Vector3(1f, 2f, 3f), state.position);
            Assert.AreEqual(Quaternion.Euler(0f, 90f, 0f), state.rotation);
            Assert.AreEqual("HeavyRope", state.ropeVariantName);
        }

        [Test]
        public void DefaultState_RopeVariantNameIsNull()
        {
            var state = new InteractableObjectState();
            Assert.IsNull(state.ropeVariantName);
        }
    }
}
