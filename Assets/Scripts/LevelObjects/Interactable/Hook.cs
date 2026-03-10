using System.Collections;
using System.Runtime.InteropServices.WindowsRuntime;
using UnityEngine;
using LevelObjects.Interactable;
using Save;

namespace LevelObjects.Interactable
{
    public class Hook : Interactable
    {
        public enum HookType { Swinging, Climbing }
        
        [Header("Hook Settings")] 
        [SerializeField] private HookType hookType;
        [SerializeField] private bool requireRope = true;
        
        [Header("Rope Visual")]
        [SerializeField] private GameObject ropePrefab;
        [SerializeField] private Transform ropeParent;
        
        private bool _ropeAttached;
        private GameObject _ropeInstance;
        private InventoryManager _inventoryManager;
        
        public bool RopeAttached => _ropeAttached;

        private void Start()
        {
            _inventoryManager = GameObject.Find("InventorySelector").GetComponent<InventoryManager>();

            if (ropeParent != null) return;
        }

        public override void Interact()
        {
            if (_ropeAttached)
            {
                ActivateHook();
                return;
            }

            if (requireRope)
            {
                if (!TakeRopeFromInventory())
                {
                    Debug.Log("No rope in inventory!");
                    return;
                }
            }

            AttachRope();
            ActivateHook();
        }
        
        private void AttachRope()
        {
            _ropeAttached = true;

            if (ropePrefab != null)
            {
                var parent = ropeParent != null ? ropeParent : transform;
                _ropeInstance = Instantiate(ropePrefab, parent);
                _ropeInstance.transform.localPosition = Vector3.zero;
                _ropeInstance.transform.localRotation = Quaternion.identity;
            }
        }
        
        private void ActivateHook()
        {
            if (hookType == HookType.Swinging)
            {
                var swingController = FindFirstObjectByType<SwingingController>();
                if (swingController != null)
                {
                    swingController.AttachToHook(this);
                }
            }
            else if (hookType == HookType.Climbing)
            {
                Debug.Log("Climbing hook is attached: Go Climb it (or ye idk)");
            }
        }

        private bool TakeRopeFromInventory()
        {
            if(_inventoryManager == null) return false;

            foreach (var slot in _inventoryManager.inventoryItems)
            {
                if (slot.itemName == "Rope" && slot.quantity > 0)
                {
                    slot.RemoveItem(1);
                    return true;
                }
            }
            return false;
        }
        
        public override InteractableObjectState SaveState()
        {
            return new InteractableObjectState
            {
                uniqueId = GetUniqueId(),
                isActive = _ropeAttached,

            };
        }

        public override void LoadState(InteractableObjectState state)
        {
            if (state == null || state.uniqueId != GetUniqueId()) return;
            _ropeAttached = state.isActive;
            if (_ropeAttached) AttachRope();
        }
    }
}
