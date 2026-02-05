using UnityEngine;

namespace Player
{
    public class CameraController : MonoBehaviour
    {
        [Header("Target Settings")] public Transform target;
        public Vector3 offset = new Vector3(0, 0, -10);

        [Header("Background Settings")] public SpriteRenderer backgroundSprite;

        private Camera cam;
        private float halfHeight;
        private float halfWidth;
        private float minX, maxX, minY, maxY;

        private void Start()
        {
            if (target == null)
                target = GameObject.FindGameObjectWithTag("Player").transform;

            cam = GetComponent<Camera>();
            halfHeight = cam.orthographicSize;
            halfWidth = halfHeight * cam.aspect;

            UpdateBackgroundBounds();
        }

        public void SetBackgroundSprite(SpriteRenderer sprite)
        {
            backgroundSprite = sprite;
            UpdateBackgroundBounds();
        }

        private void UpdateBackgroundBounds()
        {
            if (backgroundSprite != null)
            {
                if (cam == null)
                    cam = GetComponent<Camera>();

                if (cam != null)
                {
                    halfHeight = cam.orthographicSize;
                    halfWidth = halfHeight * cam.aspect;
                }

                float spriteWidth = backgroundSprite.sprite.bounds.size.x * backgroundSprite.transform.localScale.x;
                float spriteHeight = backgroundSprite.sprite.bounds.size.y * backgroundSprite.transform.localScale.y;

                Vector3 spritePos = backgroundSprite.transform.position;

                minX = spritePos.x - spriteWidth / 2;
                maxX = spritePos.x + spriteWidth / 2;
                minY = spritePos.y - spriteHeight / 2;
                maxY = spritePos.y + spriteHeight / 2;
            }
        }

        private void LateUpdate()
        {
            if (target == null) return;

            Vector3 desiredPosition = target.position + offset;

            if (backgroundSprite != null)
            {
                float newX = Mathf.Clamp(desiredPosition.x, minX + halfWidth, maxX - halfWidth);

                float newY = Mathf.Clamp(desiredPosition.y, minY + halfHeight, maxY - halfHeight);


                transform.position = new Vector3(newX, newY, desiredPosition.z);
            }
            else
            {
                transform.position = desiredPosition;
            }
        }

        private void OnDrawGizmos()
        {
            if (backgroundSprite != null)
            {
                // Draw the camera bounds in the editor
                Gizmos.color = Color.green;
                Gizmos.DrawWireCube(backgroundSprite.transform.position,
                    new Vector3(
                        backgroundSprite.sprite.bounds.size.x * backgroundSprite.transform.localScale.x,
                        backgroundSprite.sprite.bounds.size.y * backgroundSprite.transform.localScale.y,
                        0));
            }
        }
    }
}