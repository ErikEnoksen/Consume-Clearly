using System.Collections;
using UnityEngine;

namespace Player
{
    public class MovementScript : MonoBehaviour
    {
        [Header("Movement Settings")] [SerializeField]
        private float speed = 8f;

        [SerializeField] private float jumpingPower = 14f;
        [SerializeField] private float landAcceleration = 30f;
        [SerializeField] private float landDeceleration = 100f;
        [SerializeField] private float airAcceleration = 30f;
        [SerializeField] private float airDeceleration = 100f;

        [Header("Jump Settings")] [SerializeField]
        private float coyoteTime = 0.2f;

        [SerializeField] private float jumpBufferTime = 0.2f;

        [Header("Gravity Multipliers")] [SerializeField]
        private float baseGravity = 2.5f;

        [SerializeField] private float fallMultiplier = 3.5f;
        [SerializeField] private float shortJumpMultiplier = 3f;

        [Header("Ground Check")] [SerializeField]
        private Transform groundCheck;

        [SerializeField] private LayerMask groundLayer;
        [SerializeField] private float groundCheckRadius = 0.2f;

        private Rigidbody2D rb;
        private float horizontal;
        private float targetSpeed;
        private float accelRate;
        private float coyoteTimeCounter;
        private float jumpBufferTimeCounter;
        private bool wasGrounded = true;

        private AnimationController animationController;

        private bool canMove = true;

        // Climb related
        public bool IsClimbing { get; private set; } = false;
        private float climbVerticalVelocity = 0f; // set by ClimbController each FixedUpdate
        private Transform currentClimbTransform = null;
        private bool currentClimbIsLadder = false;

        private void Awake()
        {
            ValidateComponents();
        }

        private void OnEnable()
        {
            Dialogue.OnDialogueStarted += OnDialogueStarted;
            Dialogue.OnDialogueEnded += OnDialogueEnded;
        }

        private void OnDisable()
        {
            Dialogue.OnDialogueStarted -= OnDialogueStarted;
            Dialogue.OnDialogueEnded -= OnDialogueEnded;
        }

        private void OnDialogueStarted(CompanionFriendship _) => FreezeMovement(true);
        private void OnDialogueEnded(DialogueObject _) => FreezeMovement(false);

        public void FreezeMovement(bool freeze)
        {
            canMove = !freeze;
            if (!canMove)
            {
                horizontal = 0f;
                jumpBufferTimeCounter = 0f;
                rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
                rb.constraints = RigidbodyConstraints2D.FreezeRotation | RigidbodyConstraints2D.FreezePositionX;
                animationController.SetWalking(false);
                animationController.SetIdle(true);
            }
            else
            {
                rb.constraints = RigidbodyConstraints2D.FreezeRotation;
            }
        }

        private void ValidateComponents()
        {
            rb = GetComponent<Rigidbody2D>();
            animationController = GetComponent<AnimationController>();
            if (rb == null)
            {
                Debug.LogError("Rigidbody2D missing from player!");
                enabled = false;
                return;
            }

            if (animationController == null)
            {
                Debug.LogError("AnimationController missing from player!");
                enabled = false;
                return;
            }

            if (groundCheck == null)
            {
                // Log an error so tests that expect the message still pass.
                Debug.LogError("Ground Check reference missing from player! Please set it using SetupGroundCheck.");

                // Disable component when groundCheck is missing. SetupGroundCheck will re-enable it when
                // a valid transform is provided (used by PlayMode tests that assign it after adding the component).
                enabled = false;
                return;
            }
        }

        public void SetupGroundCheck(Transform groundCheckTransform)
        {
            groundCheck = groundCheckTransform;
            if (groundCheck == null)
            {
                Debug.LogError("Failed to assign GroundCheck! Movement script will not function correctly.");
            }
            else
            {
                // If a GroundCheck is provided at runtime (e.g., PlayMode tests add it after the component),
                // ensure the component is enabled so behavior runs as expected.
                enabled = true;
            }
        }

        // All input is read in Update so GetKeyDown/GetKeyUp are never missed between FixedUpdate frames
        private void Update()
        {
            if (!canMove) return;

            float moveLeft = Input.GetKey(KeybindManager.Instance.GetKey("MoveLeft")) ? -1f : 0f;
            float moveRight = Input.GetKey(KeybindManager.Instance.GetKey("MoveRight")) ? 1f : 0f;
            horizontal = moveLeft + moveRight;

            // Buffer jump input regardless of ground state so pressing jump slightly before landing still works
            if (Input.GetKeyDown(KeybindManager.Instance.GetKey("Jump")))
            {
                jumpBufferTimeCounter = jumpBufferTime;
            }

            // Variable jump height: catch key release in Update so it is never missed
            if (Input.GetKeyUp(KeybindManager.Instance.GetKey("Jump")) && rb.linearVelocity.y > 0f)
            {
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, rb.linearVelocity.y * 0.1f);
            }
        }

        private void FixedUpdate()
        {
            if (!canMove) return;

            Move();

            float moveLeft = Input.GetKey(KeybindManager.Instance.GetKey("MoveLeft")) ? -1f : 0f;
            float moveRight = Input.GetKey(KeybindManager.Instance.GetKey("MoveRight")) ? 1f : 0f;
            horizontal = moveLeft + moveRight;
            bool isGrounded = IsGrounded();

            // Handle walking and idle animations
            if (isGrounded && !IsClimbing)
            {
                if (Mathf.Abs(horizontal) > 0.1f)
                {
                    animationController.SetWalking(true);
                    animationController.SetIdle(false);
                    if (!AudioManager.Instance.IsPlaying("Footstep"))
                        AudioManager.Instance.Play("Footstep");
                }
                else
                {
                    animationController.SetWalking(false);
                    animationController.SetIdle(true);
                    AudioManager.Instance.Stop("Footstep");
                }
            }

            // If climbing, apply the vertical velocity set by the ClimbController
            if (IsClimbing)
            {
                // Preserve horizontal velocity (we want to lock horizontal while climbing)
                float currentX = rb.linearVelocity.x;
                rb.linearVelocity = new Vector2(currentX, climbVerticalVelocity);
            }

            // Dynamic gravity for better jump feel
            if (!IsClimbing)
            {
                if (rb.linearVelocity.y < 0)
                {
                    // Falling - pull down fast for snappy landing
                    rb.gravityScale = fallMultiplier;
                }
                else if (rb.linearVelocity.y > 0 && !Input.GetButton("Jump"))
                {
                    // Released early - cut the jump short
                    rb.gravityScale = shortJumpMultiplier;
                }
                else
                {
                    // Holding jump or grounded - still has weight, no floating
                    rb.gravityScale = baseGravity;
                }
            }

            // Handle sprite flipping
            animationController.FlipSprite(horizontal);

            jump();
        }

        private bool IsGrounded()
        {
            if (groundCheck == null) return false;
            return Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayer);
        }

        private void Move()
        {
            // While climbing we avoid applying horizontal control. Horizontal remains locked or zero.
            if (IsClimbing)
            {
                // Lock horizontal movement while climbing
                rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
                rb.constraints = RigidbodyConstraints2D.FreezeRotation;
                return;
            }

            if (!IsGrounded())
            {
                // In the air, never freeze X so the player has full air control
                rb.constraints = RigidbodyConstraints2D.FreezeRotation;

                targetSpeed = horizontal * speed;
                accelRate = (Mathf.Abs(horizontal) > 0.01f) ? airAcceleration : airDeceleration;
                float newX = Mathf.MoveTowards(rb.linearVelocity.x, targetSpeed, accelRate * Time.fixedDeltaTime);
                rb.linearVelocity = new Vector2(newX, rb.linearVelocity.y);
            }
            else
            {
                if (Mathf.Abs(horizontal) > 0.01f)
                {
                    // Grounded with input - unfreeze and accelerate
                    rb.constraints = RigidbodyConstraints2D.FreezeRotation;

                    targetSpeed = horizontal * speed;
                    bool isChangingDirection = (horizontal > 0f && rb.linearVelocity.x < -0.01f) || (horizontal < 0f && rb.linearVelocity.x > 0.01f);
                    accelRate = isChangingDirection ? landDeceleration : landAcceleration;
                    float newX = Mathf.MoveTowards(rb.linearVelocity.x, targetSpeed, accelRate * Time.fixedDeltaTime);
                    rb.linearVelocity = new Vector2(newX, rb.linearVelocity.y);
                }
                else
                {
                    // Grounded with no input - freeze X so physics can't push us off edges
                    rb.constraints = RigidbodyConstraints2D.FreezeRotation | RigidbodyConstraints2D.FreezePositionX;
                    rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
                }
            }
        }
        
        public void jump()
        {
            bool grounded = IsGrounded();

            // Play landing sound when we just touched the ground
            if (grounded && !wasGrounded)
            {
                AudioManager.Instance.Play("JumpEnd");
            }
            wasGrounded = grounded;

            // Coyote time: reset timer while grounded and not rising (prevents re-arming
            // coyote immediately after a jump while the player is still touching the ground)
            if (grounded && rb.linearVelocity.y <= 0f)
            {
                coyoteTimeCounter = coyoteTime;
            }
            else
            {
                coyoteTimeCounter -= Time.fixedDeltaTime;
            }

            // Decrement jump buffer each fixed frame so it expires naturally
            jumpBufferTimeCounter -= Time.fixedDeltaTime;

            jumpAction();
        }

        public void jumpAction()
        {
            // Allow jump when within coyote time (or grounded) AND the player pressed jump recently (buffer)
            bool canJump = (coyoteTimeCounter > 0f || IsGrounded()) && jumpBufferTimeCounter > 0f;

            if (canJump)
            {
                // Apply jump velocity
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpingPower);

                // Play jump sound only when a jump actually happens
                AudioManager.Instance.Play("Jump");

                // Fire-and-forget animation trigger, no coroutine blocking the next jump
                animationController.TriggerJump();

                // Consume both timers so we cannot double-jump
                jumpBufferTimeCounter = 0f;
                coyoteTimeCounter = 0f;

                Debug.Log("Jump applied");
            }
        }

        // Climb control API ------------------
        public void GrantJumpCoyote()
        {
            coyoteTimeCounter = coyoteTime;
        }

        public void EnterClimb(Transform climbTransform, bool isLadder, bool snapToX = true)
        {
            IsClimbing = true;
            currentClimbTransform = climbTransform;
            currentClimbIsLadder = isLadder;

            // Snap player X to climb object's X (snap-to-ladder behavior)
            if (snapToX && currentClimbTransform != null)
            {
                Vector3 pos = transform.position;
                pos.x = currentClimbTransform.position.x;
                transform.position = pos;
            }

            // Ensure velocity reset on enter and disable gravity while climbing
            rb.linearVelocity = new Vector2(0f, 0f);
            rb.gravityScale = 0f;

            // Notify animator via AnimationController
            animationController.SetWalking(false);
            animationController.SetIdle(false);
            animationController.CancelJump();
            animationController.SetClimbActive(true);
            animationController.SetClimbLadder(isLadder);
            animationController.SetClimbRope(!isLadder);
        }

        public void ExitClimb(Vector2 exitVelocity)
        {
            IsClimbing = false;
            currentClimbTransform = null;
            currentClimbIsLadder = false;

            // Restore gravity and apply exit velocity
            rb.gravityScale = baseGravity;
            rb.linearVelocity = exitVelocity;

            // Reset climb vertical control
            climbVerticalVelocity = 0f;

            // Notify animator
            animationController.SetClimbActive(false);
            animationController.SetClimbLadder(false);
            animationController.SetClimbRope(false);
        }

        // Called by ClimbController each FixedUpdate to specify the vertical velocity while climbing
        public void SetClimbVertical(float verticalVelocity)
        {
            climbVerticalVelocity = verticalVelocity;
        }


#if UNITY_EDITOR
        public void Test_ApplyHorizontalForFixedUpdates(float horizontalValue, int steps = 3)
        {
            if (rb == null) rb = GetComponent<Rigidbody2D>();

            for (int i = 0; i < Mathf.Max(1, steps); i++)
            {
                horizontal = horizontalValue;
                Move();
            }
        }


        public void Test_Jump()
        {
            // For tests, simulate a jump button press by filling the jump buffer and calling the jump logic.
            // Do NOT force coyote time or directly set the rigidbody velocity here so tests that depend
            // on actual grounded/coyote timing remain valid.
            jumpBufferTimeCounter = jumpBufferTime;
            jumpAction();
        }

        // Backwards-compatible helper for tests that want to force a jump regardless of coyote timing.
        public void Test_Jump_ForceCoyote()
        {
            coyoteTimeCounter = coyoteTime;
            jumpBufferTimeCounter = jumpBufferTime;
            jumpAction();

            // Ensure tests observing velocity immediately can see the applied jump.
            if (rb != null)
            {
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpingPower);
            }
        }
#endif
    }
}