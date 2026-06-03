// =============================================================================
// PlayerInteract.cs — Player Interaction Detection & Input
// 
//
// PURPOSE:
//   Detects interactable objects in front of the player each frame and fires
//   their Interact() method when the player presses the interact key.
//   Also controls the "Press E" prompt UI so it shows only when an interactable
//   is in range and facing the right way.
//
// DETECTION:
//   Uses OverlapCircleAll to find all Interactable colliders within maxSearchRadius,
//   then filters by:
//     1. Facing direction — dot product with the vector to the object must be > 0.1
//        (objects directly behind the player are ignored)
//     2. InteractionRange — each Interactable has its own range; only objects within
//        that range qualify
//   The closest qualifying interactable wins (bestDistance).
//
// FACING DIRECTION:
//   Reads from the SpriteRenderer's flipX flag (most reliable for 2D sprites).
//   Falls back to localScale.x, then to raw horizontal input if neither is available.
//
// REQUIRES LEVER:
//   Interactables with RequiresLever = true are skipped — those are activated by
//   a lever/switch, not by the player pressing E directly.
//
// GIZMOS (editor):
//   Yellow line = cast direction, yellow sphere = detection radius at the origin.
// =============================================================================

using UnityEngine;
using LevelObjects.Interactable;

namespace Player
{
    public class PlayerInteract : MonoBehaviour
    {
        // --- Inspector Fields ---
        public LayerMask interactableLayer;

        // "Press E" UI prompt shown when a valid interactable is in range.
        public GameObject pressE;

        [Header("Raycast tuning")]
        public float originOffset = 0.5f;   // shifts the cast start forward so it doesn't begin inside the player
        public float circleRadius = 0.12f;  // thickness of the sweep (unused in current OverlapCircle implementation)
        public float maxSearchRadius = 5f;  // maximum detection radius — individual interactables may restrict further

        private SpriteRenderer spriteRenderer;

        void Awake()
        {
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();
            pressE.SetActive(false);
        }

        // --- Update: Detect & Prompt ---
        // Each frame, finds the best interactable in front of the player and
        // shows the prompt. Triggers Interact() on key press.
        void Update()
        {
            Vector2 dir = GetFacingDirection();

            // Offset the origin forward so the cast doesn't immediately hit the player's own collider.
            Vector2 origin = (Vector2)transform.position + dir * originOffset;

            Interactable interactable = FindBestInteractable(origin, dir);

            Debug.DrawRay(origin, dir * maxSearchRadius, Color.yellow, 0.5f);

            if (interactable != null && !interactable.RequiresLever)
            {
                pressE.SetActive(true);

                if (Input.GetKeyDown(KeybindManager.Instance.GetKey("Interact")))
                    interactable.Interact();
            }
            else
            {
                pressE.SetActive(false);
            }
        }

        // --- Find Best Interactable ---
        // Scans all interactable colliders in range, filters by facing direction and
        // the object's own InteractionRange, and returns the closest valid one.
        private Interactable FindBestInteractable(Vector2 origin, Vector2 dir)
        {
            Collider2D[] hits = Physics2D.OverlapCircleAll(origin, maxSearchRadius, interactableLayer);

            Interactable bestInteractable = null;
            float bestDistance = float.MaxValue;

            foreach (var hit in hits)
            {
                Interactable interactable = hit.GetComponentInParent<Interactable>();
                if (interactable == null) continue;

                Vector2 closestPoint = hit.ClosestPoint(origin);
                Vector2 toTarget = closestPoint - origin;

                // Skip objects that aren't generally in front of the player (dot product filter).
                if (toTarget.sqrMagnitude > 0.001f)
                {
                    float facingDot = Vector2.Dot(dir, toTarget.normalized);
                    if (facingDot <= 0.1f) continue;
                }

                float distance = Vector2.Distance(origin, closestPoint);

                // Each interactable defines its own maximum range.
                if (distance > interactable.InteractionRange) continue;

                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    bestInteractable = interactable;
                }
            }

            return bestInteractable;
        }

        // --- Facing Direction ---
        // Priority: SpriteRenderer.flipX → localScale.x → raw horizontal input → transform.right
        private Vector2 GetFacingDirection()
        {
            if (spriteRenderer != null)
                return spriteRenderer.flipX ? Vector2.left : Vector2.right;

            if (transform.localScale.x < 0f) return Vector2.left;
            if (transform.localScale.x > 0f) return Vector2.right;

            float h = Input.GetAxisRaw("Horizontal");
            if (Mathf.Abs(h) > 0.1f) return h > 0f ? Vector2.right : Vector2.left;

            return (Vector2)transform.right;
        }

        // Draws the detection origin and search radius in the Scene view.
        void OnDrawGizmos()
        {
            Gizmos.color = Color.yellow;
            Vector2 dir = Application.isPlaying ? GetFacingDirection() : (Vector2)transform.right;
            Vector2 origin = (Vector2)transform.position + dir * originOffset;

            Gizmos.DrawLine(origin, origin + dir * maxSearchRadius);
            Gizmos.DrawWireSphere(origin, maxSearchRadius);
        }
    }
}