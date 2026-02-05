using UnityEngine;
using LevelObjects.Interactable;

namespace Player
{
    public class PlayerInteract : MonoBehaviour
    {
        public KeyCode interactKey = KeyCode.E;
        public LayerMask interactableLayer;

        public GameObject pressE;

        [Header("Raycast tuning")]
        public float originOffset = 0.5f;     // move the cast origin forward so it doesn't start inside the player
        public float circleRadius = 0.12f;    // thickness of the cast

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

            // Use CircleCast (thick ray) so the detection is more forgiving
            RaycastHit2D hit = Physics2D.CircleCast(origin, circleRadius, dir, 0.1f, interactableLayer);

            Debug.DrawRay(origin, dir * 0.1f, Color.yellow, 0.5f);

            if (hit.collider != null)
            {
                Interactable interactable = hit.collider.GetComponent<Interactable>();
                if (interactable != null && !interactable.RequiresLever)
                {
                    pressE.SetActive(true);
                    if (Input.GetKeyDown(interactKey))
                    {
                        interactable.Interact();
                    }
                }
            }
            else
            {
                pressE.SetActive(false);
            }
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
            Gizmos.DrawLine(origin, origin + dir * 0.1f);
            Gizmos.DrawWireSphere(origin + dir * 0.1f, circleRadius);
        }
    }
}