using LevelObjects;
using UnityEngine;

namespace Player
{
    // Requirements: Ladder and Rope objects should be tagged with "Ladder" or "Rope" and have a Trigger Collider2D.
    public class ClimbController : MonoBehaviour
    {
        [Header("Climb Settings")] [SerializeField]
        private float climbSpeed = 3f;

        [Header("Debug")] [SerializeField]
        private bool debugLogs = true;

        [Header("Debug Keys")] [SerializeField]
        private KeyCode debugEnterKey = KeyCode.K;

        [SerializeField] private float pushOffHorizontal = 6f;
        [SerializeField] private float pushOffVertical = 3f;
        [SerializeField] private float jumpOffVertical = 8f;

        private MovementScript movement;
        private AnimationController animationController;

        // The currently available climbable collider we can enter (not necessarily actively climbing).
        private Collider2D availableClimb = null;
        private bool availableIsLadder = false;
        private LevelObjects.Climbable activeClimbable = null;

        private void Awake()
        {
            movement = GetComponent<MovementScript>();
            animationController = GetComponent<AnimationController>();

            if (movement == null)
            {
                Debug.LogError("ClimbController requires MovementScript on the same GameObject.");
                enabled = false;
                return;
            }

            if (animationController == null)
            {
                Debug.LogError("ClimbController requires AnimationController on the same GameObject.");
                enabled = false;
                return;
            }
        }

        private void Update()
        {
            // If not currently climbing and we are overlapping a climbable, allow entering by pressing W or Up
            if (!movement.IsClimbing && availableClimb != null)
            {
                float v = Input.GetAxisRaw("Vertical");
                if (Input.GetKeyDown(KeyCode.W) || v > 0f)
                {
                    if (debugLogs) Debug.Log($"[ClimbController] Enter requested. availableClimb={availableClimb.name}, v={v}");
                    EnterAvailableClimb();
                    return;
                }

                // Debug: force enter climb with debug key
                if (debugLogs && Input.GetKeyDown(debugEnterKey))
                {
                    Debug.Log($"[ClimbController] Debug enter key pressed. Forcing enter on {availableClimb.name}");
                    EnterAvailableClimb();
                    return;
                }
            }

            // While climbing, check for exit inputs
            if (movement.IsClimbing)
            {
                // Jump exits upward
                if (Input.GetButtonDown("Jump"))
                {
                    if (debugLogs) Debug.Log("[ClimbController] Jump input while climbing - exiting");
                    Vector2 exitVel = new Vector2(0f, jumpOffVertical);
                    movement.ExitClimb(exitVel);
                    return;
                }

                // A or D pushes off horizontally (and exits)
                if (Input.GetKeyDown(KeyCode.A))
                {
                    if (debugLogs) Debug.Log("[ClimbController] A pressed - push off left");
                    Vector2 exitVel = new Vector2(-pushOffHorizontal, pushOffVertical);
                    movement.ExitClimb(exitVel);
                    return;
                }
                if (Input.GetKeyDown(KeyCode.D))
                {
                    if (debugLogs) Debug.Log("[ClimbController] D pressed - push off right");
                    Vector2 exitVel = new Vector2(pushOffHorizontal, pushOffVertical);
                    movement.ExitClimb(exitVel);
                    return;
                }
            }
        }

        private void FixedUpdate()
        {
            if (movement.IsClimbing)
            {
                // Read vertical input and drive the climb vertical velocity
                float v = Input.GetAxisRaw("Vertical");
                if (debugLogs) Debug.Log($"[ClimbController] Driving climb vertical vel. input={v}");
                float targetVel = v * climbSpeed;
                movement.SetClimbVertical(targetVel);

                // Set ClimbActive based on vertical input: active while moving, false when released (hang idle)
                bool isMovingOnClimb = Mathf.Abs(v) > 0.1f;
                animationController.SetClimbActive(isMovingOnClimb);

                // If the active climbable defines bounds, clamp player's Y between them
                if (activeClimbable != null)
                {
                    var top = activeClimbable.topPoint;
                    var bottom = activeClimbable.bottomPoint;
                    if (top != null && bottom != null)
                    {
                        Vector3 pos = transform.position;
                        pos.y = Mathf.Clamp(pos.y, bottom.position.y, top.position.y);
                        transform.position = pos;
                    }
                }
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (other == null) return;
            if (debugLogs) Debug.Log($"[ClimbController] OnTriggerEnter2D: {other.name}");

            HandleClimbableEnter(other);
        }

        private void OnCollisionEnter2D(Collision2D collision)
        {
            if (collision == null || collision.collider == null) return;
            if (debugLogs) Debug.Log($"[ClimbController] OnCollisionEnter2D: {collision.collider.name}");

            HandleClimbableEnter(collision.collider);
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            if (other == null) return;

            // If we are currently climbing and we are leaving the climb trigger we entered from, exit climbing.
            if (movement.IsClimbing && availableClimb != null && other == availableClimb)
            {
                movement.ExitClimb(new Vector2(0f, 0f));
            }

            if (other == availableClimb)
            {
                availableClimb = null;
                availableIsLadder = false;
                activeClimbable = null;
                if (debugLogs) Debug.Log($"[ClimbController] Cleared availableClimb (exited trigger): {other.name}");
            }
        }

        private void OnCollisionExit2D(Collision2D collision)
        {
            if (collision == null || collision.collider == null) return;

            var other = collision.collider;

            // If we are currently climbing and we are leaving the climb collider we entered from, exit climbing.
            if (movement.IsClimbing && availableClimb != null && other == availableClimb)
            {
                movement.ExitClimb(new Vector2(0f, 0f));
            }

            if (other == availableClimb)
            {
                availableClimb = null;
                availableIsLadder = false;
                activeClimbable = null;
                if (debugLogs) Debug.Log($"[ClimbController] Cleared availableClimb (exited collision): {other.name}");
            }
        }

        private void HandleClimbableEnter(Collider2D other)
        {
            if (other == null) return;

            var climbable = other.GetComponent<LevelObjects.Climbable>();
            if (climbable != null)
            {
                availableClimb = other;
                availableIsLadder = (climbable.climbType == LevelObjects.Climbable.ClimbType.Ladder);
                activeClimbable = climbable;
                if (debugLogs) Debug.Log($"[ClimbController] Found LevelObjects.Climbable on {other.name}. isLadder={availableIsLadder} snapToX={activeClimbable.snapToX}");
                return;
            }

            var fallback = other.GetComponent<Climbable>();
            if (fallback != null)
            {
                availableClimb = other;
                availableIsLadder = (fallback.climbType == Climbable.ClimbType.Ladder);
                activeClimbable = null;
                if (debugLogs) Debug.Log($"[ClimbController] Found Player.Climbable on {other.name}. isLadder={availableIsLadder}");
                return;
            }

            if (other.CompareTag("Ladder"))
            {
                availableClimb = other;
                availableIsLadder = true;
                if (debugLogs) Debug.Log($"[ClimbController] Found Ladder tagged on {other.name}");
            }
            else if (other.CompareTag("Rope"))
            {
                availableClimb = other;
                availableIsLadder = false;
                if (debugLogs) Debug.Log($"[ClimbController] Found Rope tagged on {other.name}");
            }
        }

        private void EnterAvailableClimb()
        {
            if (availableClimb == null) return;

            // Enter climb via MovementScript - use the active Climbable's snapToX when present
            bool snapToX = true;
            if (activeClimbable != null)
            {
                snapToX = activeClimbable.snapToX;
            }
            if (debugLogs) Debug.Log($"[ClimbController] Entering climb on {availableClimb.name}. isLadder={availableIsLadder} snapToX={snapToX}");
            movement.EnterClimb(availableClimb.transform, availableIsLadder, snapToX);
        }
    }
}
