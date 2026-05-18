using Save;
using UnityEngine;
using System;

namespace LevelObjects.Interactable
{
    public class TrashCan : Interactable
    {
        [Header("Trashcan Visuals")]
        [SerializeField] private GameObject fullTrashcan;
        [SerializeField] private GameObject emptyTrashcan;

        [Header("Loot Settings")]
        [SerializeField] private Item item;
        [SerializeField] private int satisfactionReward = 2;

        [Header("References")]
        [SerializeField] private InventoryManager inventoryManager;
        [SerializeField] private CircularSatisfactionMeter satisfactionMeter;

        // State
        private bool _isEmpty = false;
        private bool _isInitialized = false;

        // Events
        public event Action<TrashCan> OnTrashEmptied;
        public event Action<TrashCan> OnTrashReset;

        // Properties
        public bool IsEmpty => _isEmpty;
        public bool CanInteract => !_isEmpty && _isInitialized;

        protected override void Awake()
        {
            base.Awake();
            InitializeReferences();
        }

        private void InitializeReferences()
        {
            // Try to find inventory if not assigned
            if (inventoryManager == null)
            {
                inventoryManager = FindFirstObjectByType<InventoryManager>();
                if (inventoryManager == null)
                    Debug.LogError($"[TrashCan] No InventoryManager found in scene!");
            }

            // Try to find satisfaction meter if not assigned
            if (satisfactionMeter == null)
            {
                satisfactionMeter = FindFirstObjectByType<CircularSatisfactionMeter>();
                if (satisfactionMeter == null)
                    Debug.LogError($"[TrashCan] No CircularSatisfactionMeter found in scene!");
            }

            // Validate visual references
            if (fullTrashcan == null || emptyTrashcan == null)
            {
                Debug.LogWarning($"[TrashCan] Visual objects not fully assigned on {gameObject.name}");
            }

            _isInitialized = true;
            UpdateVisuals();
        }

        private void Start()
        {
            if (DayCycleManager.Instance != null)
            {
                DayCycleManager.Instance.OnNewDay += ResetDaily;
            }
            else
            {
                Debug.LogWarning($"[TrashCan] DayCycleManager not found, daily reset won't work");
            }
        }

        private void OnDestroy()
        {
            if (DayCycleManager.Instance != null)
            {
                DayCycleManager.Instance.OnNewDay -= ResetDaily;
            }
        }

        public override void Interact()
        {
            if (!ValidateInteraction()) return;

            EmptyTrash();
        }

        private bool ValidateInteraction()
        {
            if (!_isInitialized)
            {
                Debug.LogWarning($"[TrashCan] Not initialized yet");
                return false;
            }

            if (_isEmpty)
            {
                Debug.Log($"[TrashCan] Trash can is already empty");
                return false;
            }

            return true;
        }

        private void EmptyTrash()
        {
            _isEmpty = true;
            UpdateVisuals();

            bool itemGiven = GiveItemToPlayer();
            bool satisfactionGiven = GiveSatisfactionReward();

            // Trigger events
            OnTrashEmptied?.Invoke(this);

            Debug.Log($"[TrashCan] Trash emptied! Item given: {itemGiven}, Satisfaction given: {satisfactionGiven}");
        }

        private bool GiveItemToPlayer()
        {
            if (item == null)
            {
                Debug.LogWarning($"[TrashCan] No item assigned to give");
                return false;
            }

            if (inventoryManager == null)
            {
                Debug.LogError($"[TrashCan] Cannot give item - InventoryManager is null");
                return false;
            }

            int excessItems = inventoryManager.AddItem(item);

            if (excessItems > 0)
            {
                Debug.Log($"[TrashCan] Inventory full! {excessItems} {item}(s) couldn't be added.");
                return false;
            }

            Debug.Log($"[TrashCan] Gave item: {item}");
            return true;
        }

        private bool GiveSatisfactionReward()
        {
            if (satisfactionMeter == null)
            {
                Debug.LogError($"[TrashCan] Cannot give satisfaction - SatisfactionMeter is null");
                return false;
            }

            try
            {
                satisfactionMeter.IncreaseSatisfactionValue(satisfactionReward);
                Debug.Log($"[TrashCan] Added {satisfactionReward} satisfaction points");
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError($"[TrashCan] Failed to add satisfaction: {e.Message}");
                return false;
            }
        }
        private void UpdateVisuals()
        {
            if (fullTrashcan != null)
                fullTrashcan.SetActive(!_isEmpty);

            if (emptyTrashcan != null)
                emptyTrashcan.SetActive(_isEmpty);
        }

        private void ResetDaily(int day)
        {
            if (_isEmpty)
            {
                _isEmpty = false;
                UpdateVisuals();
                OnTrashReset?.Invoke(this);

                Debug.Log($"[TrashCan] Reset on day {day}");
            }
        }

        [Serializable]
        private class TrashCanSaveData
        {
            public string uniqueId;
            public bool isEmpty;
            public string lastEmptiedDate; // Optional for more advanced saving
        }

        public override InteractableObjectState SaveState()
        {
            return new InteractableObjectState
            {
                uniqueId = GetUniqueId(),
                isActive = _isEmpty
            };
        }

        public override void LoadState(InteractableObjectState state)
        {
            if (state == null)
            {
                Debug.LogWarning($"[TrashCan] Null state provided for {gameObject.name}");
                return;
            }

            if (state.uniqueId != GetUniqueId())
            {
                Debug.LogWarning($"[TrashCan] State ID mismatch: {state.uniqueId} vs {GetUniqueId()}");
                return;
            }

            _isEmpty = state.isActive;

            // Ensure visuals are updated after loading
            if (fullTrashcan != null && emptyTrashcan != null)
            {
                UpdateVisuals();
            }
            else
            {
                Debug.LogWarning($"[TrashCan] Cannot update visuals after load - references missing on {gameObject.name}");
            }

            Debug.Log($"[TrashCan] Loaded state - Empty: {_isEmpty}");
        }
    }
}