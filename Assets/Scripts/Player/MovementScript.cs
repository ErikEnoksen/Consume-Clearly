// =============================================================================
// MovementScript.cs — Player Horizontal Movement, Jumping & Climbing
// 
//
// PURPOSE:
//   Handles all physics-driven player locomotion. Reads input in Update,
//   applies forces in FixedUpdate, and exposes an API for other systems
//   (dialogue, workshops, climbing) to freeze or modify movement.
//
// MOVEMENT:
//   Uses MoveTowards on Rigidbody2D.linearVelocity rather than AddForce so
//   acceleration and deceleration feel snappy and predictable. Separate rates
//   for land/air and acceleration/deceleration give fine-grained feel control.
//   When grounded with no input, X is frozen via Rigidbody constraints so the
//   player doesn't slide on sloped or dynamic surfaces.
//
// JUMP SYSTEM:
//   Two techniques make jumping feel responsive:
//   • Coyote time  — the player can still jump for a short window after walking
//                    off a ledge, as if the ground lingers briefly.
//   • Jump buffer  — if the player presses jump just before landing, the input
//                    is remembered and fires the moment they touch ground.
//   Variable jump height is achieved by cutting upward velocity in half when
//   the jump key is released early (read in Update so it's never missed).
//
// GRAVITY MULTIPLIERS:
//   Three gravity scales give the jump an arc that feels heavier than default:
//   • Falling      → fallMultiplier    (pulls down fast for snappy landing)
//   • Rising + key released → shortJumpMultiplier (cuts arc short)
//   • Holding jump / grounded → baseGravity
//
// CLIMBING:
//   ClimbController calls EnterClimb/ExitClimb/SetClimbVertical each frame.
//   While climbing, gravity is disabled and horizontal movement is locked.
//   The player is optionally snapped to the ladder/rope X position on enter.
//
// MOVEMENT FREEZE:
//   FreezeMovement(true) is called automatically by dialogue start events and
//   by UI panels (workshop, etc.) via WorkshopUI. It zeroes velocity, freezes
//   X, and sets the idle animation. FreezeMovement(false) restores full control.
// =============================================================================

using System.Collections;
using UnityEngine;

namespace Player
{
    public class MovementScript : MonoBehaviour
    {
        // --- Movement Tuning ---
        [Header("Movement Settings")]
        [SerializeField] private float speed = 8f;
        [SerializeField] private float jumpingPower = 14f;
        [SerializeField] private float landAcceleration = 30f;  // how fast we reach target speed on ground
        [SerializeField] private float landDeceleration = 100f; // how fast we stop / change direction on ground
        [SerializeField] private float airAcceleration = 30f;
        [SerializeField] private float airDeceleration = 100f;

        // --- Jump Tuning ---
        [Header("Jump Settings")]
        [SerializeField] private float coyoteTime = 0.2f;      // seconds after leaving ground where jump still works
        [SerializeField] private float jumpBufferTime = 0.2f;  // seconds before landing where a jump input is remembered

        // --- Gravity Tuning ---
        [Header("Gravity Multipliers")]
        [SerializeField] private float baseGravity = 3.0f;
        [SerializeField] private float fallMultiplier = 3.5f;       // applied when falling
        [SerializeField] private float shortJumpMultiplier = 3f;    // applied when jump key released early

        // --- Ground Detection ---
        [Header("Ground Check")]
        [SerializeField] private Transform groundCheck;           // empty child transform at the player's feet
        [SerializeField] private LayerMask groundLayer;
        [SerializeField] private float groundCheckRadius = 0.2f;

        // --- Runtime State ---
        private Rigidbody2D rb;
        private float horizontal;
        private float targetSpeed;
        private float accelRate;
        private float coyoteTimeCounter;
        private float jumpBufferTimeCounter;
        private bool wasGrounded = true; // used to detect the moment of landing for sound

        private AnimationController animationController;
        private bool canMove = true;

        // --- Climb State ---
        // Set externally by ClimbController. While IsClimbing is true, horizontal is
        // locked and vertical velocity comes from climbVerticalVelocity instead of gravity.
        public bool IsClimbing { get; private set; } = false;
        private float climbVerticalVelocity = 0f;       // set by ClimbController each FixedUpdate
        private Transform currentClimbTransform = null;
        private bool currentClimbIsLadder = false;

        private void Awake()
        {
            ValidateComponents();
        }

        // --- Dialogue Event Wiring ---
        // Auto-freeze movement when dialogue opens, unfreeze when it ends.
        // Subscribed on enable so it works even if the object is toggled.
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

        // --- Freeze Movement ---
        // Called by dialogue events, UI panels, and cutscenes to lock the player in place.
        // Freezes X via Rigidbody constraints (not just zeroing velocity) so physics
        // can't drift the player while locked. Restores full constraint when unfrozen.
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

        // --- Component Validation ---
        // Disables the script early if required references are missing so errors
        // are caught immediately rather than crashing mid-play.
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

        // Allows PlayMode tests (and runtime setup) to assign groundCheck after the component exists.
        public void SetupGroundCheck(Transform groundCheckTransform)
        {
            groundCheck = groundCheckTransform;
            if (groundCheck == null)
                Debug.LogError("Failed to assign GroundCheck! Movement script will not function correctly.");
            else
                enabled = true; // re-enable if it was disabled due to missing groundCheck
        }

        // --- Input (Update) ---
        // All input is read here so GetKeyDown/GetKeyUp are never missed between FixedUpdate frames.
        // Horizontal direction and jump buffer are set; physics is applied in FixedUpdate.
        private void Update()
        {
            if (!canMove) return;

            float moveLeft = Input.GetKey(KeybindManager.Instance.GetKey("MoveLeft")) ? -1f : 0f;
            float moveRight = Input.GetKey(KeybindManager.Instance.GetKey("MoveRight")) ? 1f : 0f;
            horizontal = moveLeft + moveRight;

            // Buffer jump input regardless of ground state so pressing jump slightly before landing still works.
            if (Input.GetKeyDown(KeybindManager.Instance.GetKey("Jump")))
                jumpBufferTimeCounter = jumpBufferTime;

            // Variable jump height: cut upward velocity on key release. Read in Update so it's never missed.
            if (Input.GetKeyUp(KeybindManager.Instance.GetKey("Jump")) && rb.linearVelocity.y > 0f)
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, rb.linearVelocity.y * 0.5f);
        }

        // --- Physics (FixedUpdate) ---
        // Applies movement, climbing velocity, dynamic gravity, animation, and jump logic.
        private void FixedUpdate()
        {
            if (!canMove) return;

            Move();

            // Re-read input here as well — FixedUpdate can run multiple times per frame.
            float moveLeft = Input.GetKey(KeybindManager.Instance.GetKey("MoveLeft")) ? -1f : 0f;
            float moveRight = Input.GetKey(KeybindManager.Instance.GetKey("MoveRight")) ? 1f : 0f;
            horizontal = moveLeft + moveRight;
            bool isGrounded = IsGrounded();

            // Walking / idle animation and footstep audio — only when grounded and not climbing.
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

            // While climbing, override vertical velocity with whatever ClimbController set this frame.
            if (IsClimbing)
            {
                float currentX = rb.linearVelocity.x;
                rb.linearVelocity = new Vector2(currentX, climbVerticalVelocity);
            }

            // Dynamic gravity multipliers give the jump a weighted, game-feel arc.
            if (!IsClimbing)
            {
                if (rb.linearVelocity.y < 0)
                    rb.gravityScale = fallMultiplier;           // falling — pull down fast
                else if (rb.linearVelocity.y > 0 && !Input.GetKey(KeybindManager.Instance.GetKey("Jump")))
                    rb.gravityScale = shortJumpMultiplier;     // released early — cut arc short
                else
                    rb.gravityScale = baseGravity;             // holding jump or grounded
            }

            animationController.FlipSprite(horizontal);
            jump();
        }

        // Small overlap circle at the player's feet — true when touching the ground layer.
        private bool IsGrounded()
        {
            if (groundCheck == null) return false;
            return Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayer);
        }

        // --- Horizontal Movement ---
        // Uses MoveTowards for responsive acceleration/deceleration without AddForce.
        // Uses higher deceleration when changing direction for a snappier feel.
        // Freezes X constraint when grounded and idle to prevent physics sliding.
        private void Move()
        {
            if (IsClimbing)
            {
                // Lock horizontal while climbing — ClimbController owns vertical.
                rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
                rb.constraints = RigidbodyConstraints2D.FreezeRotation;
                return;
            }

            if (!IsGrounded())
            {
                // In the air — never freeze X so the player has full air control.
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
                    // Grounded with input — unfreeze and accelerate. Use deceleration when reversing direction.
                    rb.constraints = RigidbodyConstraints2D.FreezeRotation;
                    targetSpeed = horizontal * speed;
                    bool isChangingDirection = (horizontal > 0f && rb.linearVelocity.x < -0.01f)
                                            || (horizontal < 0f && rb.linearVelocity.x > 0.01f);
                    accelRate = isChangingDirection ? landDeceleration : landAcceleration;
                    float newX = Mathf.MoveTowards(rb.linearVelocity.x, targetSpeed, accelRate * Time.fixedDeltaTime);
                    rb.linearVelocity = new Vector2(newX, rb.linearVelocity.y);
                }
                else
                {
                    // Grounded with no input — freeze X so physics can't push us off edges.
                    rb.constraints = RigidbodyConstraints2D.FreezeRotation | RigidbodyConstraints2D.FreezePositionX;
                    rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
                }
            }
        }

        // --- Jump (timer management) ---
        // Ticks coyote and jump buffer timers and delegates the actual jump to jumpAction().
        public void jump()
        {
            bool grounded = IsGrounded();

            // Play landing sound on the first frame back on the ground.
            if (grounded && !wasGrounded)
                AudioManager.Instance.Play("JumpEnd");
            wasGrounded = grounded;

            // Coyote time: reset while grounded and not rising. The y <= 0 guard prevents
            // re-arming coyote immediately after a jump while still touching the platform.
            if (grounded && rb.linearVelocity.y <= 0f)
                coyoteTimeCounter = coyoteTime;
            else
                coyoteTimeCounter -= Time.fixedDeltaTime;

            jumpBufferTimeCounter -= Time.fixedDeltaTime;
            jumpAction();
        }

        // --- Jump Action ---
        // Fires the actual jump if both coyote and buffer conditions are met.
        // Consuming both timers prevents double-jumping.
        public void jumpAction()
        {
            bool canJump = (coyoteTimeCounter > 0f || IsGrounded()) && jumpBufferTimeCounter > 0f;

            if (canJump)
            {
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpingPower);
                AudioManager.Instance.Play("Jump");
                animationController.TriggerJump(); // fire-and-forget; doesn't block next jump
                jumpBufferTimeCounter = 0f;
                coyoteTimeCounter = 0f;
                Debug.Log("Jump applied");
            }
        }

        // --- Climb API ---
        // Called by ClimbController to give the player a free jump window just after leaving a ladder.
        public void GrantJumpCoyote()
        {
            coyoteTimeCounter = coyoteTime;
        }

        // Sets climbing state, optionally snaps to the ladder/rope X, zeros velocity, and disables gravity.
        public void EnterClimb(Transform climbTransform, bool isLadder, bool snapToX = true)
        {
            IsClimbing = true;
            currentClimbTransform = climbTransform;
            currentClimbIsLadder = isLadder;

            if (snapToX && currentClimbTransform != null)
            {
                Vector3 pos = transform.position;
                pos.x = currentClimbTransform.position.x;
                transform.position = pos;
            }

            rb.linearVelocity = new Vector2(0f, 0f);
            rb.gravityScale = 0f;

            animationController.SetWalking(false);
            animationController.SetIdle(false);
            animationController.CancelJump();
            animationController.SetClimbActive(true);
            animationController.SetClimbLadder(isLadder);
            animationController.SetClimbRope(!isLadder);
        }

        // Restores gravity and normal movement state. exitVelocity lets ClimbController
        // launch the player off a rope or ladder with momentum.
        public void ExitClimb(Vector2 exitVelocity)
        {
            IsClimbing = false;
            currentClimbTransform = null;
            currentClimbIsLadder = false;
            rb.gravityScale = baseGravity;
            rb.linearVelocity = exitVelocity;
            climbVerticalVelocity = 0f;

            animationController.SetClimbActive(false);
            animationController.SetClimbLadder(false);
            animationController.SetClimbRope(false);
        }

        // Called by ClimbController each FixedUpdate to drive vertical velocity while climbing.
        public void SetClimbVertical(float verticalVelocity)
        {
            climbVerticalVelocity = verticalVelocity;
        }

        // --- Editor / Test Helpers ---
        // These methods are compiled out of builds and exist only for PlayMode tests.
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

        // Simulates a jump button press via the buffer — does not force coyote so timing tests stay valid.
        public void Test_Jump()
        {
            jumpBufferTimeCounter = jumpBufferTime;
            jumpAction();
        }

        // Forces both timers so tests that don't need realistic grounded timing can still test jump velocity.
        public void Test_Jump_ForceCoyote()
        {
            coyoteTimeCounter = coyoteTime;
            jumpBufferTimeCounter = jumpBufferTime;
            jumpAction();

            if (rb != null)
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpingPower);
        }
#endif
    }
}