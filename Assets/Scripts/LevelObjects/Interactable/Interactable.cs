using UnityEngine;
using Save;

namespace LevelObjects.Interactable
{
    public abstract class Interactable : MonoBehaviour, ISaveable
    {
        [SerializeField] protected bool requiresLever = false;

        [Header("Save System")] [SerializeField]
        private string uniqueId;

        public bool RequiresLever => requiresLever;

        private void OnValidate()
        {
            if (string.IsNullOrEmpty(uniqueId))
            {
                uniqueId = System.Guid.NewGuid().ToString();
            }

            // Ensure objects that require a lever are on the Default layer (0)
            if (requiresLever)
            {
                gameObject.layer = 0; // Default layer
            }
        }

        protected virtual void Awake()
        {
            if (string.IsNullOrEmpty(uniqueId))
            {
                uniqueId = System.Guid.NewGuid().ToString();
            }

            if (requiresLever)
            {
                gameObject.layer = 0; // Default layer at runtime
            }
        }

        public string GetUniqueId()
        {
            return uniqueId;
        }

        public abstract void Interact();
        public abstract InteractableObjectState SaveState();
        public abstract void LoadState(InteractableObjectState state);
    }
}