using UnityEngine;
using LevelObjects.Interactable;

namespace Player
{
    public class PlayerInteract : MonoBehaviour
    {
        public LayerMask interactableLayer;

        public GameObject pressE;

        [Header("Raycast tuning")]
        public float originOffset = 0.5f;     // move the cast origin forward so it doesn't start inside the player
        public float circleRadius = 0.12f;    // thickness of the cast
        public float maxSearchRadius = 5f;    // Biggest interaction Range that is supported
        
        private SpriteRenderer spriteRenderer;


        void Awake()
        {
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();
            pressE.SetActive(false);
        }

        void Update()
        {
            Vector2 dir = GetFacingDirection();

            // Compute an origin slightly in front of the player to avoid starting inside the player's collider
            Vector2 origin = (Vector2)transform.position + dir * originOffset;

            Interactable interactable = FindBestInteractable(origin, dir);

            Debug.DrawRay(origin, dir * maxSearchRadius, Color.yellow, 0.5f);

            if (interactable != null && !interactable.RequiresLever)
            {
                pressE.SetActive(true);

                if (Input.GetKeyDown(KeybindManager.Instance.GetKey("Interact")))
                {
                    interactable.Interact();
                }
            }
            else
            {
                pressE.SetActive(false);
            }
        }
        
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

                if (toTarget.sqrMagnitude > 0.001f)
                {
                    float facingDot = Vector2.Dot(dir, toTarget.normalized);
                    if (facingDot <= 0.1f) continue;
                }

                float distance = Vector2.Distance(origin, closestPoint);
                if (distance > interactable.InteractionRange) continue;

                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    bestInteractable = interactable;
                }
            }

            return bestInteractable;
        }

        private Vector2 GetFacingDirection()
        {
            if (spriteRenderer != null)
            {
                return spriteRenderer.flipX ? Vector2.left : Vector2.right;
            }

            if (transform.localScale.x < 0f) return Vector2.left;
            if (transform.localScale.x > 0f) return Vector2.right;

            float h = Input.GetAxisRaw("Horizontal");
            if (Mathf.Abs(h) > 0.1f) return h > 0f ? Vector2.right : Vector2.left;

            return (Vector2)transform.right;
        }

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