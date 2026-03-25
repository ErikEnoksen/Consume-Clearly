using UnityEngine;
using Save;

namespace LevelObjects.Interactable
{
    public abstract class Interactable : MonoBehaviour, ISaveable
    {
        [SerializeField] protected bool requiresLever = false;
        
        [Header("Interactable Settings")]
        [SerializeField] protected float interactionRange = 0.6f;
        
        [Header("Save System")] [SerializeField]
        private string uniqueId;

        public bool RequiresLever => requiresLever;
        public float InteractionRange => interactionRange;

        private void OnValidate()
        {
            if (string.IsNullOrEmpty(uniqueId))
            {
                uniqueId = System.Guid.NewGuid().ToString();
            }
            
            interactionRange = Mathf.Max(0.1f, interactionRange);
            
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