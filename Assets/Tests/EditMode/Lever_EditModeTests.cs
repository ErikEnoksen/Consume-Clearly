using System.Reflection;
using NUnit.Framework;
using LevelObjects.Interactable;
using Save;
using UnityEngine;

namespace Tests.EditMode
{
    [TestFixture]
    public class Lever_EditModeTests
    {
        private GameObject leverGO;
        private Lever lever;

        [SetUp]
        public void SetUp()
        {
            leverGO = new GameObject("Lever");
            lever = leverGO.AddComponent<Lever>();

            // Provide a handle so SaveState / LoadState don't throw
            lever.handle = leverGO.transform;

            // Provide an empty linked-object array so Interact()'s foreach is safe
            lever.linkedObjects = new Interactable[0];
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(leverGO);
        }

        // ── Interactable base (GetUniqueId, InteractionRange, RequiresLever) ────────

        [Test]
        public void GetUniqueId_IsNonEmpty_AfterAwake()
        {
            Assert.IsFalse(string.IsNullOrEmpty(lever.GetUniqueId()),
                "A GUID should be assigned in Awake");
        }

        [Test]
        public void GetUniqueId_IsValidGuid()
        {
            Assert.IsTrue(System.Guid.TryParse(lever.GetUniqueId(), out _));
        }

        [Test]
        public void InteractionRange_HasPositiveDefault()
        {
            Assert.Greater(lever.InteractionRange, 0f);
        }

        [Test]
        public void RequiresLever_IsFalseByDefault()
        {
            Assert.IsFalse(lever.RequiresLever);
        }

        // ── SaveState ─────────────────────────────────────────────────────────────

        [Test]
        public void SaveState_ReturnsStateWithMatchingUniqueId()
        {
            InteractableObjectState state = lever.SaveState();
            Assert.AreEqual(lever.GetUniqueId(), state.uniqueId);
        }

        [Test]
        public void SaveState_IsActiveIsFalseByDefault()
        {
            InteractableObjectState state = lever.SaveState();
            Assert.IsFalse(state.isActive, "Lever starts in the off (false) state");
        }

        [Test]
        public void SaveState_PositionMatchesTransform()
        {
            leverGO.transform.position = new Vector3(3f, 4f, 0f);
            InteractableObjectState state = lever.SaveState();
            Assert.AreEqual(leverGO.transform.position, state.position);
        }

        // ── Interact ──────────────────────────────────────────────────────────────

        [Test]
        public void Interact_TogglesIsOnState()
        {
            // Before: isOn = false → SaveState.isActive = false
            Assert.IsFalse(lever.SaveState().isActive);

            lever.Interact();

            // After first interact: isOn = true
            Assert.IsTrue(lever.SaveState().isActive);
        }

        [Test]
        public void Interact_TogglesBackOnSecondCall()
        {
            lever.Interact();

            // FlipHandle coroutine sets isFlipping = true synchronously; reset it so
            // the second Interact() is not blocked by the in-progress animation guard.
            typeof(Lever)
                .GetField("isFlipping", BindingFlags.NonPublic | BindingFlags.Instance)
                .SetValue(lever, false);

            lever.Interact();
            Assert.IsFalse(lever.SaveState().isActive);
        }

        // ── LoadState ─────────────────────────────────────────────────────────────

        [Test]
        public void LoadState_SetsIsOnFromState()
        {
            var state = new InteractableObjectState
            {
                uniqueId = lever.GetUniqueId(),
                isActive = true,
                position = Vector3.zero,
                rotation = Quaternion.identity
            };

            lever.LoadState(state);

            Assert.IsTrue(lever.SaveState().isActive);
        }

        [Test]
        public void LoadState_IgnoresStateWithMismatchedId()
        {
            var state = new InteractableObjectState
            {
                uniqueId = "wrong-id-totally-different",
                isActive = true
            };

            lever.LoadState(state);

            // isOn should remain unchanged (false)
            Assert.IsFalse(lever.SaveState().isActive);
        }

        [Test]
        public void LoadState_NullStateIsIgnored()
        {
            Assert.DoesNotThrow(() => lever.LoadState(null));
        }

        [Test]
        public void LoadState_SetsHandleRotation()
        {
            var targetRotation = Quaternion.Euler(0f, 0f, 45f);
            var state = new InteractableObjectState
            {
                uniqueId = lever.GetUniqueId(),
                isActive = false,
                rotation = targetRotation
            };

            lever.LoadState(state);

            Assert.AreEqual(targetRotation, lever.handle.localRotation);
        }
    }
}