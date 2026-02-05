using UnityEngine;

namespace LevelObjects.Interactable
{
    public class CogwheelRotation : MonoBehaviour
    {
        public float rotationSpeed = 180f;
        public LiftPlatform lift;

        void Update()
        {
            if (lift != null && lift.isMoving)
            {
                transform.Rotate(0f, 0f, rotationSpeed * Time.deltaTime);
            }
        }
    }
}