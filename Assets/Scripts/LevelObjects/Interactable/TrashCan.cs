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
        [SerializeField] private string itemId = "Trash";
        [SerializeField] private string itemName = "Trash";
        [SerializeField] private int amount = 1;
        [SerializeField] private Sprite itemSprite;
        [SerializeField] private string itemDescription = "Some trash";
        [SerializeField] private int maxStack = 10;
        [SerializeField] private string itemTag = "Trash";


        private InventoryManager _inventory;
        private bool isEmpty = false;

        protected override void Awake()
        {
            base.Awake();

            _inventory = FindFirstObjectByType<InventoryManager>();

            if (_inventory == null)

                Debug.LogError("No InventoryManagerTest found in scene!");

        }

        public void Start()
        {
            if (DayCycleManager.Instance != null)
                DayCycleManager.Instance.OnNewDay += ResetDaily;
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

            Debug.Log("Trash can emptied!");
        }

        private void GiveItem()
        {
            if (_inventory == null)
            {
                _inventory = FindFirstObjectByType<InventoryManager>();
                if (_inventory == null) return;
            }

            int excessItems = _inventory.AddItem(
                itemId,
                itemName,
                amount,
                itemSprite,
                itemDescription,
                maxStack,
                itemTag
            );

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