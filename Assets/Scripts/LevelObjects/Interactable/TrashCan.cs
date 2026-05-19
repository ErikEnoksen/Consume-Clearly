using Save;
using UnityEngine;

namespace LevelObjects.Interactable
{
    public class TrashCan : Interactable
    {
        [Header("Trashcan Objects")]
        [SerializeField] private GameObject fullTrashcan;
        [SerializeField] private GameObject emptyTrashcan;

        [Header("Loot")]
        [SerializeField] private Item item;


        private InventoryManager _inventory;
        private bool isEmpty = false;

        private CircularSatisfactionMeter satisfactionMeter;

        protected override void Awake()
        {
            base.Awake();

            _inventory = GameObject.Find("InventorySelector").GetComponent<InventoryManager>();
            satisfactionMeter = GameObject.Find("CircularSatisfactionMeter").GetComponent<CircularSatisfactionMeter>();

            if (_inventory == null)

                Debug.LogError("No InventoryManagerTest found in scene!");

        }

        public void Start()
        {
            if (DayCycleManager.Instance != null)
                DayCycleManager.Instance.OnNewDay += ResetDaily;
        }

        private void OnDestroy()
        {
            if (DayCycleManager.Instance != null)
                DayCycleManager.Instance.OnNewDay -= ResetDaily;
        }

        public override void Interact()
        {
            if (isEmpty) return;

            EmptyTrash();
        }

        private void EmptyTrash()
        {
            isEmpty = true;
            UpdateVisuals();

            GiveItem();
            satisfactionMeter.IncreaseSatisfactionValue(2);

            Debug.Log("Trash can emptied!");
        }

        private void GiveItem()
        {
            if (_inventory == null)
            {
                _inventory = FindFirstObjectByType<InventoryManager>();
                if (_inventory == null) return;
            }

            int excessItems = _inventory.AddItem(item, 1);

            if (excessItems > 0)
            {
                Debug.Log($"Inventory full! {excessItems} items couldn't be added.");
            }
        }

        private void UpdateVisuals()
        {
            if (fullTrashcan != null) fullTrashcan.SetActive(!isEmpty);
            if (emptyTrashcan != null) emptyTrashcan.SetActive(isEmpty);
        }

        public override InteractableObjectState SaveState()
        {
            return new InteractableObjectState
            {
                uniqueId = GetUniqueId(),
                isActive = isEmpty
            };
        }

        public override void LoadState(InteractableObjectState state)
        {
            if (state == null || state.uniqueId != GetUniqueId()) return;

            isEmpty = state.isActive;

            // Add null checks to ensure objects are ready
            if (fullTrashcan != null && emptyTrashcan != null)
            {
                UpdateVisuals();
            }
            else
            {
                Debug.LogWarning($"Visual objects not assigned for TrashCan {gameObject.name}");
            }
        }

        private void ResetDaily(int day)
        {
            isEmpty = false;
            UpdateVisuals();
        }
    }
}