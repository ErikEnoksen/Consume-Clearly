using UnityEngine;
using System.Collections;
using Save;

namespace LevelObjects.Interactable
{
    public class LiftPlatform : Interactable
    {
        [Header("Lift Movement")] public Transform topPoint;
        public Transform bottomPoint;
        public float moveSpeed = 2f;
        public GameObject liftGround;

        [Header("Player Settings")] public float playerYOffset = 0.5f; // Height above lift ground where player stands

        private bool isAtTop = false;
        public bool isMoving = false;
        private GameObject currentPlayer;
        private Rigidbody2D playerRb;

        private void Start()
        {
            if (liftGround == null)
            {
                liftGround = transform.Find("LiftGround")?.gameObject;
                if (liftGround == null)
                {
                    Debug.LogError("LiftGround not found! Please assign it in the inspector.");
                }
            }

            CheckInitialPosition();
        }

        private void CheckInitialPosition()
        {
            if (topPoint == null || bottomPoint == null)
            {
                Debug.LogWarning("Top or Bottom point not assigned!");
                return;
            }

            // Calculate distances to top and bottom
            float distanceToTop = Vector3.Distance(transform.position, topPoint.position);
            float distanceToBottom = Vector3.Distance(transform.position, bottomPoint.position);

            // If closer to top, set isAtTop to true
            isAtTop = distanceToTop < distanceToBottom;

            Debug.Log($"Lift starting position: {(isAtTop ? "TOP" : "BOTTOM")}");
        }

        public override void Interact()
        {
            if (!isMoving)
                StartCoroutine(MoveLift());
        }

        public override InteractableObjectState SaveState()
        {
            return new InteractableObjectState
            {
                uniqueId = GetUniqueId(),
                isActive = isAtTop,
                position = transform.position,
                rotation = transform.rotation
            };
        }

        public override void LoadState(InteractableObjectState state)
        {
            if (state == null || state.uniqueId != GetUniqueId()) return;

            isAtTop = state.isActive;
            transform.position = state.position;
            
            StopAllCoroutines();
            isMoving = false;
            
            if (currentPlayer != null && playerRb != null)
            {
                currentPlayer.transform.SetParent(null);
                SetPlayerPhysics(false);
                currentPlayer = null;
                playerRb = null;
            }
        }

        private IEnumerator MoveLift()
        {
            isMoving = true;
            UpdatePlayerConstraints();

            Vector3 startPos = transform.position;
            Vector3 targetPos = isAtTop ? bottomPoint.position : topPoint.position;

            float t = 0f;
            while (t < 1f)
            {
                t += Time.deltaTime * moveSpeed;
                transform.position = Vector3.Lerp(startPos, targetPos, t);
                UpdatePlayerPosition();
                yield return null;
            }

            transform.position = targetPos;
            isAtTop = !isAtTop;
            isMoving = false;
            UpdatePlayerConstraints();
        }

        private void UpdatePlayerPosition()
        {
            if (currentPlayer != null && playerRb != null && liftGround != null)
            {
                Vector3 newPlayerPos = currentPlayer.transform.position;
                newPlayerPos.y = liftGround.transform.position.y + playerYOffset;
                currentPlayer.transform.position = newPlayerPos;
            }
        }

        private void UpdatePlayerConstraints()
        {
            if (playerRb == null) return;

            if (isMoving)
            {
                playerRb.constraints = RigidbodyConstraints2D.FreezePositionY | RigidbodyConstraints2D.FreezeRotation;
            }
            else
            {
                playerRb.constraints = RigidbodyConstraints2D.FreezeRotation;
            }
        }

        private void SetPlayerPhysics(bool onLift)
        {
            if (playerRb == null) return;

            if (onLift)
            {
                playerRb.gravityScale = 0;
                playerRb.linearVelocity = Vector2.zero;
                UpdatePlayerConstraints();
            }
            else
            {
                playerRb.gravityScale = 1;
                playerRb.constraints = RigidbodyConstraints2D.FreezeRotation;

                if (isMoving)
                {
                    Vector2 platformVelocity = (bottomPoint.position - topPoint.position).normalized * moveSpeed;
                    playerRb.linearVelocity = platformVelocity;
                }
            }
        }

        // Called when player enters the lift cabin collision box
        private void OnTriggerEnter2D(Collider2D collision)
        {
            if (collision.CompareTag("Player"))
            {
                currentPlayer = collision.gameObject;
                playerRb = collision.GetComponent<Rigidbody2D>();

                if (playerRb != null)
                {
                    currentPlayer.transform.SetParent(transform);
                    SetPlayerPhysics(true);
                    
                    // Position player on the lift ground immediately
                    if (liftGround != null)
                    {
                        Vector3 playerPos = currentPlayer.transform.position;
                        playerPos.y = liftGround.transform.position.y + playerYOffset;
                        currentPlayer.transform.position = playerPos;
                    }
                    
                    Debug.Log("Player entered lift cabin");
                }
            }
        }

        // Called when player exits the lift cabin collision box
        private void OnTriggerExit2D(Collider2D collision)
        {
            if (collision.gameObject == currentPlayer)
            {
                if (playerRb != null)
                {
                    currentPlayer.transform.SetParent(null);
                    SetPlayerPhysics(false);
                    Debug.Log("Player exited lift cabin");
                }

                currentPlayer = null;
                playerRb = null;
            }
        }

        private void OnDrawGizmos()
        {
            if (topPoint == null || bottomPoint == null) return;

            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(topPoint.position, bottomPoint.position);

            float pointSize = 0.3f;

            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(topPoint.position, pointSize);

            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(bottomPoint.position, pointSize);

            Gizmos.color = Color.yellow;
            Gizmos.DrawWireCube(transform.position, Vector3.one * pointSize);

            if (liftGround != null)
            {
                Gizmos.color = isMoving ? new Color(1f, 0.5f, 0f, 0.3f) : new Color(0f, 1f, 1f, 0.3f);
                BoxCollider2D collider = liftGround.GetComponent<BoxCollider2D>();
                if (collider != null)
                {
                    Vector3 pos = liftGround.transform.position + (Vector3)collider.offset;
                    Vector3 size = new Vector3(
                        collider.size.x * liftGround.transform.lossyScale.x,
                        collider.size.y * liftGround.transform.lossyScale.y,
                        0.1f
                    );
                    Gizmos.DrawCube(pos, size);
                }

                // Visualize player offset
                Gizmos.color = Color.magenta;
                Vector3 playerStandPos = liftGround.transform.position + Vector3.up * playerYOffset;
                Gizmos.DrawLine(
                    playerStandPos + Vector3.left * 1f,
                    playerStandPos + Vector3.right * 1f
                );
                Gizmos.DrawWireSphere(playerStandPos, 0.2f);
            }
        }
    }
}