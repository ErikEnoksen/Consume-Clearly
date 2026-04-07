using NUnit.Framework;
using UnityEngine;

namespace Tests.EditMode
{
    [TestFixture]
    public class KeybindManager_EditModeTests
    {
        private GameObject go;
        private KeybindManager km;

        [SetUp]
        public void SetUp()
        {
            // Clear PlayerPrefs so defaults load without overrides
            PlayerPrefs.DeleteAll();

            // Destroy any lingering singleton from a previous test
            if (KeybindManager.Instance != null)
                Object.DestroyImmediate(KeybindManager.Instance.gameObject);

            KeybindManager.Instance = null;

            go = new GameObject("KeybindManager");
            km = go.AddComponent<KeybindManager>(); // Awake() loads clean defaults
        }

        [TearDown]
        public void TearDown()
        {
            PlayerPrefs.DeleteAll();
            Object.DestroyImmediate(go);
            KeybindManager.Instance = null;
        }

        // ── Default keybinds ──────────────────────────────────────────────────────

        [Test]
        public void GetKey_DefaultJump_IsSpace()
        {
            Assert.AreEqual(KeyCode.Space, km.GetKey("Jump"));
        }

        [Test]
        public void GetKey_DefaultInteract_IsE()
        {
            Assert.AreEqual(KeyCode.E, km.GetKey("Interact"));
        }

        [Test]
        public void GetKey_DefaultInventory_IsQ()
        {
            Assert.AreEqual(KeyCode.Q, km.GetKey("Inventory"));
        }

        [Test]
        public void GetKey_DefaultPause_IsP()
        {
            Assert.AreEqual(KeyCode.P, km.GetKey("Pause"));
        }

        [Test]
        public void GetKey_UnknownAction_ReturnsNone()
        {
            Assert.AreEqual(KeyCode.None, km.GetKey("NonExistentAction"));
        }

        // ── SetKey ────────────────────────────────────────────────────────────────

        [Test]
        public void SetKey_ChangesBinding()
        {
            km.SetKey("Jump", KeyCode.Z);
            Assert.AreEqual(KeyCode.Z, km.GetKey("Jump"));
        }

        [Test]
        public void SetKey_PreventsDuplicateBinding()
        {
            // E is already bound to Interact; binding Jump to E should be rejected
            km.SetKey("Jump", KeyCode.E);
            Assert.AreEqual(KeyCode.Space, km.GetKey("Jump"),
                "Jump should stay on Space when E is already bound to Interact");
        }

        [Test]
        public void SetKey_PersistsToPlayerPrefs()
        {
            km.SetKey("Jump", KeyCode.Z);
            Assert.AreEqual("Z", PlayerPrefs.GetString("Jump"));
        }

        // ── GetKeyAsString ────────────────────────────────────────────────────────

        [Test]
        public void GetKeyAsString_ReturnsCorrectDefaultName()
        {
            Assert.AreEqual("Space", km.GetKeyAsString("Jump"));
        }

        [Test]
        public void GetKeyAsString_ReflectsAfterSetKey()
        {
            km.SetKey("Jump", KeyCode.Z);
            Assert.AreEqual("Z", km.GetKeyAsString("Jump"));
        }

        // ── Singleton ─────────────────────────────────────────────────────────────

        [Test]
        public void Singleton_IsAssignedAfterAwake()
        {
            Assert.AreEqual(km, KeybindManager.Instance);
        }
    }
}