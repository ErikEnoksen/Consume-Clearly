// =============================================================================
// CompanionFollow2D.cs — NavMesh-Backed 2D Companion Follower
// 
//
// PURPOSE:
//   Moves a companion toward the player using NavMesh pathfinding for obstacle
//   avoidance, combined with direct Rigidbody2D velocity for smooth movement.
//   Unlike a pure NavMeshAgent, this drives the physics body manually so the
//   companion has proper 2D gravity and can jump over obstacles.
//
// MOVEMENT FLOW (FixedUpdate):
//   1. If too far (> teleportDistance) → snap to player instantly
//   2. If close enough (< followDistance) → idle in place
//   3. Otherwise → UpdatePathfinding() + HandleMovement() + CheckIfStuck()
//
// PATHFINDING:
//   NavMesh.CalculatePath() is called when the target moves more than repathDistance,
//   or when the companion strays more than stuckCheckDistance from the current path.
//   The path corners are averaged (SmoothPath) to reduce sharp direction changes.
//   If no valid path exists, the companion falls back to moving directly toward the target.
//
// STUCK DETECTION:
//   If the companion barely moves for stuckTimeout seconds, the path is recalculated.
//   This handles cases where the NavMesh path is valid but geometry blocks actual movement.
//
// OFF-MESH LINKS:
//   TraverseOffMeshLink() handles jump-link traversal with a sine-curve arc.
//   The call site (TryHandleOffMeshLink) is currently commented out — the coroutine
//   exists for when proper OffMeshLink detection is wired up.
//
// ACTIVATION:
//   The companion starts inactive (companionActivated = false) and only begins
//   following after a specific dialogue ("MinerDialogue") ends. Change the dialogue
//   name check in OnDialogueEndedHandler to control which conversation unlocks following.
//
// GIZMOS (editor only):
//   Red lines = raw NavMesh path corners
//   Green lines = smoothed path
//   Blue sphere = current target corner
//   Yellow sphere = ground check radius
// =============================================================================

using System.Collections;
using NavMeshPlus.NavMeshPlus_master.NavMeshComponents.Scripts;
using Player;
using UnityEngine;
using UnityEngine.AI;

namespace Companion
{
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(AnimationController))]
    public class CompanionFollow2D : MonoBehaviour
    {
        // --- Inspector Fields ---
        [Header("References")]
        [Tooltip("Target transform to follow (usually the player)")]
        [SerializeField] private Transform target;
        [SerializeField] private NavMeshSurface surface2D;

        [Header("Movement Settings")]
        [SerializeField] private float moveSpeed = 5f;
        [SerializeField] private float jumpForce = 12f;
        [SerializeField] private bool canJump = true;
        [SerializeField] private float followDistance = 2f;       // stop following within this range
        [SerializeField] private float teleportDistance = 15f;    // snap to player if this far away

        [Header("Ground Settings")]
        [SerializeField] private float groundCheckRadius = 0.1f;
        [SerializeField] private Transform groundCheck;
        [SerializeField] private LayerMask groundLayer;

        [Header("Path Settings")]
        [SerializeField] private float repathDistance = 1.0f;      // recalculate when target moves this far
        [SerializeField] private float stuckCheckDistance = 2.0f;  // recalculate if this far off the path
        [SerializeField] private float stuckTimeout = 1.0f;        // recalculate if barely moving for this long
        [SerializeField] private int smoothingPoints = 3;           // averaging window for path smoothing

        [Header("OffMesh Link Settings")]
        [Tooltip("Height of the jump arc when traversing off-mesh links")]
        [SerializeField] private float linkJumpHeight = 2.5f;
        [Tooltip("Duration of the jump animation over an off-mesh link")]
        [SerializeField] private float linkJumpDuration = 0.5f;

        // --- Runtime State ---
        private bool companionActivated = false; // stays false until unlocked by dialogue
        private Rigidbody2D rb;
        private NavMeshPath path;
        private AnimationController animationController;
        private int currentCorner = 1;       // index into path.corners being tracked toward
        private float coyoteTimeCounter;
        private Vector3 lastTargetPosition;  // used to decide when to repath
        private float stuckTimer;
        private Vector3 lastPosition;        // used to measure movement for stuck detection
        private Vector3[] smoothedPath;      // averaged version of the NavMesh corners

        // Blocks FixedUpdate movement while the companion is mid-link traversal.
        private bool isOnOffMeshLink = false;

        // --- Initialization ---
        // Subscribe to dialogue event for activation, then find and cache components and target.
        private void Start()
        {
            Dialogue.OnDialogueEnded += OnDialogueEndedHandler;
            InitializeComponents();
            FindTarget();
        }

        // Called by CompanionManager when the player is ready. Recalculates path immediately.
        public void SetTarget(Transform newTarget)
        {
            target = newTarget;
            lastTargetPosition = target.position;
            RecalculatePath();
        }

        // --- Main Movement Loop ---
        // Runs only after the companion is activated. Handles teleport, idle, and active following.
        private void FixedUpdate()
        {
            if (!companionActivated || !IsTargetValid() || isOnOffMeshLink) return;

            float distanceToPlayer = Vector2.Distance(transform.position, target.position);

            // Too far — snap instantly rather than running a long path.
            if (distanceToPlayer > teleportDistance)
            {
                TeleportToPlayer();
                return;
            }

            // Close enough — stop and idle.
            if (distanceToPlayer <= followDistance)
            {
                HandleIdleState();
                return;
            }

            UpdatePathfinding();
            HandleMovement();
            CheckIfStuck();
        }

        // Zero horizontal velocity and play idle animation when within followDistance.
        private void HandleIdleState()
        {
            rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
            animationController.SetWalking(false);
            animationController.SetIdle(true);
        }

        // --- Pathfinding Update ---
        // Recalculates the path when the target has moved enough, or when the companion
        // has strayed too far from the current path (which implies it's no longer valid).
        private void UpdatePathfinding()
        {
            if (Vector3.Distance(target.position, lastTargetPosition) > repathDistance)
            {
                RecalculatePath();
                lastTargetPosition = target.position;
            }

            if (path == null || path.corners.Length == 0)
            {
                RecalculatePath();
                return;
            }

            int safeCorner = Mathf.Min(currentCorner, path.corners.Length - 1);
            float distToPath = Vector2.Distance(transform.position, path.corners[safeCorner]);
            if (distToPath > stuckCheckDistance)
                RecalculatePath();
        }

        // --- Movement Along Path ---
        // Drives horizontal velocity toward the next smoothed path corner.
        // Falls back to direct movement if no valid path exists.
        // Jumps when the next corner is above the companion's current position.
        private void HandleMovement()
        {
            if (path == null || path.corners.Length <= 1 || currentCorner >= path.corners.Length)
            {
                MoveDirectlyTowardTarget();
                return;
            }

            Vector3 nextCorner = GetSmoothedPosition();
            Vector2 direction = (nextCorner - transform.position).normalized;

            rb.linearVelocity = new Vector2(direction.x * moveSpeed, rb.linearVelocity.y);
            animationController.FlipSprite(direction.x);
            UpdateAnimationState(direction);

            // Jump if the next waypoint is above us and we're on the ground.
            if (canJump && IsGrounded() && nextCorner.y > transform.position.y + 0.2f)
                Jump();

            // Advance to the next corner once we're close enough.
            if (Vector2.Distance(transform.position, nextCorner) < 0.2f && currentCorner < path.corners.Length - 1)
                currentCorner++;
        }

        // Fallback when no NavMesh path is available — move straight toward the target.
        private void MoveDirectlyTowardTarget()
        {
            Vector2 direction = (target.position - transform.position).normalized;
            rb.linearVelocity = new Vector2(direction.x * moveSpeed, rb.linearVelocity.y);
            animationController.FlipSprite(direction.x);
            UpdateAnimationState(direction);

            if (canJump && IsGrounded() && direction.y > 0.3f)
                Jump();
        }

        // Returns the smoothed path position for the current corner index.
        private Vector3 GetSmoothedPosition()
        {
            if (currentCorner >= path.corners.Length) return transform.position;
            if (smoothedPath == null || smoothedPath.Length == 0) return path.corners[currentCorner];

            int smoothIndex = Mathf.Min(currentCorner, smoothedPath.Length - 1);
            return smoothedPath[smoothIndex];
        }

        // --- Stuck Detection ---
        // If position barely changed this frame, increment the stuck timer.
        // After stuckTimeout, force a repath to find an unblocked route.
        private void CheckIfStuck()
        {
            if (Vector3.Distance(transform.position, lastPosition) < 0.1f)
            {
                stuckTimer += Time.fixedDeltaTime;
                if (stuckTimer > stuckTimeout)
                {
                    RecalculatePath();
                    stuckTimer = 0f;
                }
            }
            else
            {
                stuckTimer = 0f;
            }
            lastPosition = transform.position;
        }

        // --- Path Calculation ---
        // Asks NavMesh for a path from the companion to the target, then smooths it.
        // Clears corners on failure so HandleMovement falls back to direct movement.
        private void RecalculatePath()
        {
            if (!IsTargetValid()) return;
            if (path == null) path = new NavMeshPath();

            if (NavMesh.CalculatePath(transform.position, target.position, NavMesh.AllAreas, path))
            {
                currentCorner = 1;
                SmoothPath();
            }
            else
            {
                path.ClearCorners();
            }
        }

        // --- Path Smoothing ---
        // Averages each corner with its neighbours using a sliding window of smoothingPoints.
        // Reduces sharp turns at corner waypoints for more natural movement.
        private void SmoothPath()
        {
            if (path.corners.Length < 2) return;

            smoothedPath = new Vector3[path.corners.Length];
            path.corners.CopyTo(smoothedPath, 0);

            for (int i = 1; i < smoothedPath.Length - 1; i++)
            {
                Vector3 sum = Vector3.zero;
                int count = 0;

                for (int j = Mathf.Max(0, i - smoothingPoints);
                     j < Mathf.Min(smoothedPath.Length, i + smoothingPoints + 1); j++)
                {
                    sum += path.corners[j];
                    count++;
                }

                smoothedPath[i] = sum / count;
            }
        }

        // private bool TryHandleOffMeshLink()
        // {
        //     if (NavMesh.SamplePosition(transform.position, out NavMeshHit hit, 0.5f, NavMesh.AllAreas))
        //     {
        //         if (NavMesh.FindClosestEdge(hit.position, out NavMeshHit edge, NavMesh.AllAreas))
        //         {
        //             if (edge.mask == 0 && !isOnOffMeshLink)
        //             {
        //                 // No valid edge found, might be on a jump link
        //                 OffMeshLinkData linkData = new OffMeshLinkData();
        //                 if (linkData.valid)
        //                 {
        //                     StartCoroutine(TraverseOffMeshLink(linkData));
        //                     return true;
        //                 }
        //             }
        //         }
        //     }
        //     return false;
        // }

        // --- OffMeshLink Traversal ---
        // Moves the companion along a parabolic arc between the link's start and end points.
        // Uses a sine curve for the height so the arc looks like a natural jump.
        // isOnOffMeshLink blocks FixedUpdate movement for the duration.
        private IEnumerator TraverseOffMeshLink(OffMeshLinkData data)
        {
            isOnOffMeshLink = true;
            animationController.TriggerJump();

            Vector3 startPos = transform.position;
            Vector3 endPos = data.endPos;
            float time = 0f;

            while (time < linkJumpDuration)
            {
                float t = time / linkJumpDuration;
                float height = Mathf.Sin(t * Mathf.PI) * linkJumpHeight;
                Vector3 newPos = Vector3.Lerp(startPos, endPos, t);
                newPos.y += height;
                transform.position = newPos;
                time += Time.deltaTime;
                yield return null;
            }

            transform.position = endPos;
            isOnOffMeshLink = false;
            RecalculatePath();
        }

        // --- Editor Gizmos ---
        // Visualises the raw path (red), smoothed path (green), current target corner (blue),
        // ground check radius (yellow), and the follow distance ring around the target.
        private void OnDrawGizmos()
        {
            if (path != null && path.corners.Length > 0)
            {
                Gizmos.color = Color.red;
                for (int i = 0; i < path.corners.Length - 1; i++)
                    Gizmos.DrawLine(path.corners[i], path.corners[i + 1]);

                if (smoothedPath != null && smoothedPath.Length > 1)
                {
                    Gizmos.color = Color.green;
                    for (int i = 0; i < smoothedPath.Length - 1; i++)
                        Gizmos.DrawLine(smoothedPath[i], smoothedPath[i + 1]);
                }

                if (currentCorner < path.corners.Length)
                {
                    Gizmos.color = Color.blue;
                    Gizmos.DrawWireSphere(path.corners[currentCorner], 0.3f);
                }
            }

            Gizmos.color = Color.yellow;
            if (groundCheck != null)
                Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
            if (target != null)
                Gizmos.DrawWireSphere(target.position, followDistance);
        }

        // --- Helpers ---

        // Caches component references and initialises path containers.
        private void InitializeComponents()
        {
            rb = GetComponent<Rigidbody2D>();
            animationController = GetComponent<AnimationController>();
            path = new NavMeshPath();
            smoothedPath = new Vector3[0];
        }

        // Looks for the player's TargetPoint child transform (offset follow point).
        // Falls back to the player's root transform if TargetPoint doesn't exist.
        private void FindTarget()
        {
            if (target != null) return;

            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                Transform found = player.transform.Find("TargetPoint");
                target = found ?? player.transform;
            }
        }

        private bool IsTargetValid() => target != null;

        private void UpdateAnimationState(Vector2 direction)
        {
            bool isMoving = Mathf.Abs(direction.x) > 0.1f;
            animationController.SetWalking(isMoving);
            animationController.SetIdle(!isMoving);
        }

        private void Jump()
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
            animationController.TriggerJump();
        }

        private bool IsGrounded()
        {
            if (groundCheck == null) return false;
            return Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayer);
        }

        // Instant position snap — used when the companion has fallen too far behind to catch up normally.
        private void TeleportToPlayer()
        {
            rb.linearVelocity = Vector2.zero;
            transform.position = target.position;
            RecalculatePath();
        }

        // --- Activation via Dialogue ---
        // Listens for the specific dialogue that unlocks this companion's following behaviour.
        // Change "MinerDialogue" to the name of whichever dialogue should trigger this companion.
        private void OnDialogueEndedHandler(DialogueObject dialogue)
        {
            if (dialogue == null) return;

            if (dialogue.name == "MinerDialogue")
                ActivateCompanion();
        }

        // Turns on following and unsubscribes from the event so it never fires again.
        private void ActivateCompanion()
        {
            companionActivated = true;
            Dialogue.OnDialogueEnded -= OnDialogueEndedHandler;
            if (IsTargetValid())
            {
                lastTargetPosition = target.position;
                RecalculatePath();
            }
        }

        private void OnDestroy()
        {
            Dialogue.OnDialogueEnded -= OnDialogueEndedHandler;
        }
    }
}
