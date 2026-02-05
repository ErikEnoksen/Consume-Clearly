using NUnit.Framework;
using Player;
using UnityEngine;
using UnityEngine.TestTools;

namespace Tests.EditMode
{
    [TestFixture]
    public class MovementScript_EditModeTests
    {
        private GameObject playerObj;
        private MovementScript movement;
        private Transform groundCheck;

        private const string AssertMessage = "Assertion failed on expression: 'ShouldRunBehaviour()'";
        private const string RigidbodyErrorMessage = "Rigidbody2D missing from player!";

        private const string GroundCheckErrorMessage =
            "Ground Check reference missing from player! Please set it using SetupGroundCheck.";

        private const string AnimationControllerErrorMessage = "AnimationController missing from player!";

        [SetUp]
        public void SetUp()
        {
            playerObj = new GameObject("Player");
            groundCheck = new GameObject("GroundCheck").transform;
        }

        [TearDown]
        public void TearDown()
        {
            if (playerObj != null)
                Object.DestroyImmediate(playerObj);
            if (groundCheck != null)
                Object.DestroyImmediate(groundCheck.gameObject);
        }
    
        [Test]
        public void SetupGroundCheckAssignsTransform()
        {
            movement = playerObj.AddComponent<MovementScript>();
            movement.SetupGroundCheck(groundCheck);

            var field = typeof(MovementScript).GetField("groundCheck",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            Assert.AreEqual(groundCheck, field.GetValue(movement));
        }

        [Test]
        public void ValidateComponentsMissingRigidbodyDisablesScript()
        {
            // Add required components except Rigidbody2D
            var animator = playerObj.AddComponent<Animator>();
            var spriteRenderer = playerObj.AddComponent<SpriteRenderer>();
            var animController = playerObj.AddComponent<AnimationController>();

            // Setup animator and sprite renderer on animation controller using reflection
            typeof(AnimationController)
                .GetField("animator", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                .SetValue(animController, animator);
            typeof(AnimationController)
                .GetField("spriteRenderer",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                .SetValue(animController, spriteRenderer);

            // Add MovementScript and setup groundCheck
            movement = playerObj.AddComponent<MovementScript>();
            movement.SetupGroundCheck(groundCheck);

            // First expect assertion message, then error message
            LogAssert.Expect(LogType.Assert, AssertMessage);
            LogAssert.Expect(LogType.Assert, AssertMessage);
            LogAssert.Expect(LogType.Error, RigidbodyErrorMessage);

            // Validate components
            movement.SendMessage("ValidateComponents", null, SendMessageOptions.DontRequireReceiver);

            // Check if script is disabled
            Assert.IsFalse(movement.enabled);
        }

        [Test]
        public void ValidateComponentsMissingGroundCheckDisablesScript()
        {
            // Add all required components except setting up groundCheck
            playerObj.AddComponent<Rigidbody2D>();
            var animator = playerObj.AddComponent<Animator>();
            var spriteRenderer = playerObj.AddComponent<SpriteRenderer>();
            var animController = playerObj.AddComponent<AnimationController>();

            // Setup animator and sprite renderer on animation controller
            typeof(AnimationController)
                .GetField("animator", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                .SetValue(animController, animator);
            typeof(AnimationController)
                .GetField("spriteRenderer",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                .SetValue(animController, spriteRenderer);

            // Add MovementScript without setting up groundCheck
            movement = playerObj.AddComponent<MovementScript>();

            // First expect assertion message, then error message
            LogAssert.Expect(LogType.Assert, AssertMessage);
            LogAssert.Expect(LogType.Assert, AssertMessage);
            LogAssert.Expect(LogType.Error, GroundCheckErrorMessage);

            // Validate components
            movement.SendMessage("ValidateComponents", null, SendMessageOptions.DontRequireReceiver);

            // Check if script is disabled
            Assert.IsFalse(movement.enabled);
        }

        [Test]
        public void ValidateComponentsMissingAnimationControllerDisablesScript()
        {
            // Add only Rigidbody2D and set up groundCheck
            playerObj.AddComponent<Rigidbody2D>();
            movement = playerObj.AddComponent<MovementScript>();
            movement.SetupGroundCheck(groundCheck);

            // First expect assertion message, then error message
            LogAssert.Expect(LogType.Assert, AssertMessage);
            LogAssert.Expect(LogType.Error, AnimationControllerErrorMessage);

            // Validate components
            movement.SendMessage("ValidateComponents", null, SendMessageOptions.DontRequireReceiver);

            // Check if script is disabled
            Assert.IsFalse(movement.enabled);
        }
    }
}