using UnityEngine;

namespace LevelObjects
{
    public class Climbable : MonoBehaviour
    {
        public enum ClimbType { Rope, Ladder }

        [Header("Climb Settings")] public ClimbType climbType = ClimbType.Ladder;
        public Transform topPoint;
        public Transform bottomPoint;
        [Tooltip("When true the player X will snap to the climbable's X while climbing.")]
        public bool snapToX = true;

        void OnDrawGizmosSelected()
        {
            if (topPoint != null && bottomPoint != null)
            {
                Gizmos.color = Color.cyan;
                Gizmos.DrawLine(topPoint.position, bottomPoint.position);
                Gizmos.DrawSphere(topPoint.position, 0.05f);
                Gizmos.DrawSphere(bottomPoint.position, 0.05f);
            }
        }
    }
}

