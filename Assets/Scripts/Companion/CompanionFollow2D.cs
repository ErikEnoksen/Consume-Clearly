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
        [Header("References")]
        [Tooltip("Target transform to follow (usually the player)")]
        [SerializeField] private Transform target;
        [SerializeField] private NavMeshSurface surface2D;

        [Header("Movement Settings")]
        [SerializeField] private float moveSpeed = 5f;
        [SerializeField] private float jumpForce = 12f;
        [SerializeField] private bool canJump = true;
        [SerializeField] private float followDistance = 2f;
        [SerializeField] private float lookAheadDistance = 2f;

        [Header("Ground Settings")]
        [SerializeField] private float groundCheckRadius = 0.1f;
        [SerializeField] private Transform groundCheck;
        [SerializeField] private LayerMask groundLayer;

        [Header("Path Settings")]
        [SerializeField] private float repathDistance = 1.0f;
        [SerializeField] private float stuckCheckDistance = 2.0f;
        [SerializeField] private float stuckTimeout = 1.0f;
        [SerializeField] private int smoothingPoints = 3;

        [Header("OffMesh Link Settings")]
        [Tooltip("Height of the jump arc when traversing off-mesh links")]
        [SerializeField] private float linkJumpHeight = 2.5f;
        [Tooltip("Duration of the jump animation over an off-mesh link")]
        [SerializeField] private float linkJumpDuration = 0.5f;

        private Rigidbody2D rb;
        private NavMeshPath path;
        private AnimationController animationController;
        private int currentCorner = 1;
        private float coyoteTimeCounter;
        private Vector3 lastTargetPosition;
        private float stuckTimer;
        private Vector3 lastPosition;
        private Vector3[] smoothedPath;

        // Tracks if the companion is currently jumping via an OffMeshLink
        private bool isOnOffMeshLink = false;

        private void Start()
        {
            InitializeComponents();
            FindTarget();
        }

        public void SetTarget(Transform newTarget)
        {
            target = newTarget;
            lastTargetPosition = target.position;
            RecalculatePath();
        }

        private void FixedUpdate()
        {
            if (!IsTargetValid() || isOnOffMeshLink) return;

            float distanceToPlayer = Vector2.Distance(transform.position, target.position);

            if (distanceToPlayer <= followDistance)
            {
                HandleIdleState();
                return;
            }

            UpdatePathfinding();

            // Check if we're about to traverse an off-mesh link
            if (TryHandleOffMeshLink()) return;

            HandleMovement();
            CheckIfStuck();
        }

        private void HandleIdleState()
        {
            rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
            animationController.SetWalking(false);
            animationController.SetIdle(true);
        }

        private void UpdatePathfinding()
        {
            if (Vector3.Distance(target.position, lastTargetPosition) > repathDistance)
            {
                RecalculatePath();
                lastTargetPosition = target.position;
            }

            if (path != null && path.corners.Length > 0)
            {
                float distToPath = Vector2.Distance(transform.position,
                    path.corners[Mathf.Min(currentCorner, path.corners.Length - 1)]);

                if (distToPath > stuckCheckDistance)
                {
                    RecalculatePath();
                }
            }
        }

        private void HandleMovement()
        {
            if (path == null || path.corners.Length <= 1 || currentCorner >= path.corners.Length) return;

            Vector3 nextCorner = GetSmoothedPosition();
            Vector2 direction = (nextCorner - transform.position).normalized;

            // Apply movement
            rb.linearVelocity = new Vector2(direction.x * moveSpeed, rb.linearVelocity.y);
            animationController.FlipSprite(direction.x);
            UpdateAnimationState(direction);

            // Handle jumping
            if (canJump && IsGrounded() && nextCorner.y > transform.position.y + 0.2f)
            {
                Jump();
            }

            // Progress through path
            if (Vector2.Distance(transform.position, nextCorner) < 0.2f && currentCorner < path.corners.Length - 1)
            {
                currentCorner++;
            }
        }

        private Vector3 GetSmoothedPosition()
        {
            if (currentCorner >= path.corners.Length) return transform.position;

            Vector3 targetPos = path.corners[currentCorner];
            if (smoothedPath == null || smoothedPath.Length == 0) return targetPos;

            int smoothIndex = Mathf.Min(currentCorner, smoothedPath.Length - 1);
            return smoothedPath[smoothIndex];
        }

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

        private void RecalculatePath()
        {
            if (NavMesh.CalculatePath(transform.position, target.position, NavMesh.AllAreas, path))
            {
                currentCorner = 1;
                SmoothPath();
            }
        }

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

        private bool TryHandleOffMeshLink()
        {
            if (NavMesh.SamplePosition(transform.position, out NavMeshHit hit, 0.5f, NavMesh.AllAreas))
            {
                if (NavMesh.FindClosestEdge(hit.position, out NavMeshHit edge, NavMesh.AllAreas))
                {
                    if (edge.mask == 0 && !isOnOffMeshLink)
                    {
                        // No valid edge found, might be on a jump link
                        OffMeshLinkData linkData = new OffMeshLinkData();
                        if (linkData.valid)
                        {
                            StartCoroutine(TraverseOffMeshLink(linkData));
                            return true;
                        }
                    }
                }
            }
            return false;
        }

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
        private void InitializeComponents()
        {
            rb = GetComponent<Rigidbody2D>();
            animationController = GetComponent<AnimationController>();
            path = new NavMeshPath();
            smoothedPath = new Vector3[0];
        }

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
            return Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayer);
        }
    }
}
