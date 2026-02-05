using UnityEngine;

namespace LevelObjects.Interactable
{
    [RequireComponent(typeof(BoxCollider2D))]
    public class LiftGround : MonoBehaviour
    {
        // This component now only serves as a reference point for height calculations
        // The collision detection is handled by the LiftPlatform's trigger collider

        private void Start()
        {
            // Verify it's a child of LiftPlatform
            LiftPlatform parentLift = GetComponentInParent<LiftPlatform>();
            if (parentLift == null)
            {
                Debug.LogWarning(
                    "LiftGround should be a child of an object with LiftPlatform script for proper functionality.");
            }

            // This collider can be used for visual debugging or as a platform surface
            BoxCollider2D collider = GetComponent<BoxCollider2D>();
            if (collider != null)
            {
                Debug.Log($"LiftGround initialized at position: {transform.position}");
            }
        }
    }
}