using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace Tests.EditMode
{
    /// <summary>
    /// EditMode tests for PlayerInteract.
    /// Update() is tested only for its null-guard (no KeybindManager singleton in editor).
    /// GetFacingDirection() is tested via reflection using the spriteRenderer flipX state.
    /// FindBestInteractable() requires physics and is covered by PlayMode tests.
    /// </summary>
    [TestFixture]
    public class PlayerInteract_EditModeTests
    {
        private GameObject playerGO;
        private Player.PlayerInteract interact;
        private GameObject pressEGO;

        [SetUp]
        public void SetUp()
        {
            playerGO = new GameObject("Player");
            pressEGO = new GameObject("PressE");
            pressEGO.SetActive(true);

            interact = playerGO.AddComponent<Player.PlayerInteract>();
            interact.pressE = pressEGO;
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(playerGO);
            Object.DestroyImmediate(pressEGO);
        }

        // ── Public field defaults ─────────────────────────────────────────────────

        [Test]
        public void OriginOffset_HasPositiveDefault()
        {
            Assert.Greater(interact.originOffset, 0f);
        }

        [Test]
        public void MaxSearchRadius_HasPositiveDefault()
        {
            Assert.Greater(interact.maxSearchRadius, 0f);
        }

        [Test]
        public void CircleRadius_HasPositiveDefault()
        {
            Assert.Greater(interact.circleRadius, 0f);
        }

        // ── Update null guard ─────────────────────────────────────────────────────

        [Test]
        public void Update_DoesNotThrowWhenKeybindManagerInstanceIsNull()
        {
            // pressE must be inactive so SetActive(false) in the else branch won't error.
            pressEGO.SetActive(false);

            var updateMethod = typeof(Player.PlayerInteract)
                .GetMethod("Update", BindingFlags.NonPublic | BindingFlags.Instance);

            Assert.DoesNotThrow(() => updateMethod.Invoke(interact, null));
        }

        // ── GetFacingDirection (via reflection) ───────────────────────────────────

        [Test]
        public void GetFacingDirection_ReturnsRight_WhenSpriteNotFlipped()
        {
            var sr = playerGO.AddComponent<SpriteRenderer>();
            sr.flipX = false;

            // Inject spriteRenderer via reflection (it's set in Awake; override it here)
            typeof(Player.PlayerInteract)
                .GetField("spriteRenderer", BindingFlags.NonPublic | BindingFlags.Instance)
                .SetValue(interact, sr);

            var method = typeof(Player.PlayerInteract)
                .GetMethod("GetFacingDirection", BindingFlags.NonPublic | BindingFlags.Instance);
            var dir = (Vector2)method.Invoke(interact, null);

            Assert.AreEqual(Vector2.right, dir);
        }

        [Test]
        public void GetFacingDirection_ReturnsLeft_WhenSpriteFlipped()
        {
            var sr = playerGO.AddComponent<SpriteRenderer>();
            sr.flipX = true;

            typeof(Player.PlayerInteract)
                .GetField("spriteRenderer", BindingFlags.NonPublic | BindingFlags.Instance)
                .SetValue(interact, sr);

            var method = typeof(Player.PlayerInteract)
                .GetMethod("GetFacingDirection", BindingFlags.NonPublic | BindingFlags.Instance);
            var dir = (Vector2)method.Invoke(interact, null);

            Assert.AreEqual(Vector2.left, dir);
        }
    }
}
