using System.Collections;
using Save;
using UnityEngine;

namespace LevelObjects.Interactable
{
    public class Trapdoor : Interactable
    {
        [Header("Trapdoor Parts")]
        public Transform door;
        public float openAngle = -90;  
        public float rotationSpeed = 90f; 

        private bool isOpen = false;
        private bool isRotating = false;

        private Quaternion closedRotation;
        private Quaternion openRotation;

        void Start()
        {
            if (door == null)
            {
                Debug.LogWarning("Trapdoor door not assigned — defaulting to this object.");
                door = transform;
            }

            // The door rotates around its local axis, so we use its own local rotation
            closedRotation = door.localRotation;
            openRotation = Quaternion.Euler(door.localEulerAngles + new Vector3(0, 0, openAngle));
        }

        public override void Interact()
        {
            if (!isRotating)
                StartCoroutine(RotateTrapdoor(isOpen ? closedRotation : openRotation));
        }

        public override InteractableObjectState SaveState()
        {
            return new InteractableObjectState
            {
                uniqueId = GetUniqueId(),
                isActive = isOpen,
                position = transform.position,
                rotation = door.localRotation
            };
        }

        public override void LoadState(InteractableObjectState state)
        {
            if (state == null || state.uniqueId != GetUniqueId()) return;

            isOpen = state.isActive;
            
            if (door != null)
            {
                door.localRotation = state.rotation;
            }
            
            StopAllCoroutines();
            isRotating = false;
        }

        private IEnumerator RotateTrapdoor(Quaternion target)
        {
            isRotating = true;

            while (Quaternion.Angle(door.localRotation, target) > 0.1f)
            {
                door.localRotation = Quaternion.RotateTowards(door.localRotation, target, rotationSpeed * Time.deltaTime);
                yield return null;
            }

            door.localRotation = target;
            isOpen = !isOpen;
            isRotating = false;
        }
    }
}