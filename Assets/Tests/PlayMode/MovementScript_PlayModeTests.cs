using System.Collections;
using System.Reflection;
using NUnit.Framework;
using Player;
using UnityEngine;
using UnityEngine.TestTools;

namespace Tests.PlayMode
{
    public class MovementScript_PlayModeTests
    {
        private GameObject player;
        private MovementScript movementScript;
        private Rigidbody2D rb;
        private GameObject ground;
        private Transform groundCheck;
        private GameObject audioManagerObj;
        private GameObject keybindManagerObj;

        [SetUp]
        public void SetUp()
        {
            CreateTestScene();
        }

        private void CreateTestScene()
        {
            // Set up required singletons before any MonoBehaviour that depends on them
            keybindManagerObj = new GameObject("KeybindManager");
            keybindManagerObj.AddComponent<KeybindManager>();

            audioManagerObj = new GameObject("AudioManager");
            audioManagerObj.AddComponent<AudioManager>();

            ground = new GameObject("Ground");
            ground.transform.position = Vector2.zero;
            var groundCollider = ground.AddComponent<BoxCollider2D>();
            groundCollider.size = new Vector2(10, 1);
            ground.layer = LayerMask.NameToLayer("Ground");

            // Create player with all required components first
            player = new GameObject("Player");
            rb = player.AddComponent<Rigidbody2D>();
            player.AddComponent<SpriteRenderer>();
            var animator = player.AddComponent<Animator>();
            player.AddComponent<AnimationController>();

            // Place player so groundCheck clearly overlaps the ground collider on CI
            player.transform.position = new Vector2(0, 0.6f);
            rb.gravityScale = 2f;

            // Disable Animator to avoid repeated "Animator is not playing an AnimatorController" logs on CI
            animator.enabled = false;

            // Create ground check before adding MovementScript
            var groundCheckObj = new GameObject("GroundCheck");
            groundCheckObj.transform.parent = player.transform;
            groundCheckObj.transform.localPosition = new Vector3(0, -0.5f, 0);
            groundCheck = groundCheckObj.transform;

            // Add MovementScript last, after setting up all dependencies
            LogAssert.Expect(LogType.Error,
                "Ground Check reference missing from player! Please set it using SetupGroundCheck.");
            movementScript = player.AddComponent<MovementScript>();

            movementScript.SetupGroundCheck(groundCheck);

            movementScript.GetType()
                .GetField("groundLayer", BindingFlags.NonPublic | BindingFlags.Instance)
                .SetValue(movementScript, (LayerMask)LayerMask.GetMask("Ground"));
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(player);
            Object.DestroyImmediate(ground);
            Object.DestroyImmediate(audioManagerObj);
            Object.DestroyImmediate(keybindManagerObj);

            // Reset singleton references to prevent test pollution between runs
            KeybindManager.Instance = null;
            typeof(AudioManager)
                .GetProperty("Instance", BindingFlags.Public | BindingFlags.Static)
                .SetValue(null, null);
        }

        // --- Movement ---

        [UnityTest]
        public IEnumerator PlayerMovesRightWhenInputIsPositive()
        {
            // Wait one FixedUpdate so physics initialises before we call Move()
            yield return new WaitForFixedUpdate();

            movementScript.Test_ApplyHorizontalForFixedUpdates(1f, 3);

            // Assert before the next FixedUpdate runs — real FixedUpdate reads Input.GetKey()
            // which returns 0 in tests and would immediately decelerate X back to zero.
            Assert.Greater(rb.linearVelocity.x, 0.05f, "Player should move right when horizontal input is positive.");
        }

        // --- Jump ---

        [UnityTest]
        public IEnumerator PlayerJumpsWhenGroundedAndJumpExecuted()
        {
            player.transform.position = new Vector2(0, 0.6f);
            yield return new WaitForFixedUpdate();

            float expectedJumpPower = (float)movementScript.GetType()
                .GetField("jumpingPower", BindingFlags.NonPublic | BindingFlags.Instance)
                .GetValue(movementScript);

            movementScript.Test_Jump_ForceCoyote();

            Assert.That(rb.linearVelocity.y, Is.EqualTo(expectedJumpPower).Within(0.5f),
                "Player should jump with correct vertical velocity when grounded and jump is executed.");
        }

        [UnityTest]
        public IEnumerator PlayerDoesNotJumpWhenNotGroundedAndNoCoyoteTime()
        {
            player.transform.position = new Vector2(0, 5);
            yield return new WaitForSeconds(0.3f);

            movementScript.Test_Jump();
            yield return new WaitForFixedUpdate();

            Assert.LessOrEqual(rb.linearVelocity.y, 0.1f,
                "Player should not jump when not grounded and coyote time expired.");
        }

        // --- Climb ---

        [UnityTest]
        public IEnumerator EnterClimb_SetsIsClimbingTrue()
        {
            var climbObj = new GameObject("Climb");
            climbObj.transform.position = player.transform.position;

            movementScript.EnterClimb(climbObj.transform, isLadder: true);
            yield return null;

            Assert.IsTrue(movementScript.IsClimbing, "IsClimbing should be true after EnterClimb.");

            Object.DestroyImmediate(climbObj);
        }

        [UnityTest]
        public IEnumerator ExitClimb_AfterEnterClimb_SetsIsClimbingFalse()
        {
            var climbObj = new GameObject("Climb");
            climbObj.transform.position = player.transform.position;

            movementScript.EnterClimb(climbObj.transform, isLadder: true);
            yield return null;

            movementScript.ExitClimb(Vector2.zero);
            yield return null;

            Assert.IsFalse(movementScript.IsClimbing, "IsClimbing should be false after ExitClimb.");

            Object.DestroyImmediate(climbObj);
        }

        [UnityTest]
        public IEnumerator SetClimbVertical_WhileClimbing_AppliesVerticalVelocityInFixedUpdate()
        {
            var climbObj = new GameObject("Climb");
            climbObj.transform.position = player.transform.position;

            movementScript.EnterClimb(climbObj.transform, isLadder: true);
            movementScript.SetClimbVertical(3f);

            yield return new WaitForFixedUpdate();

            Assert.That(rb.linearVelocity.y, Is.EqualTo(3f).Within(0.1f),
                "Vertical velocity should match the value set by SetClimbVertical while climbing.");

            Object.DestroyImmediate(climbObj);
        }
    }
}