using UnityEngine;

namespace Inventory
{

    public class ItemActionPanel : MonoBehaviour
    {
        [SerializeField]
        private GameObject quantityButton;
        [SerializeField]
        private GameObject removeButton;
        [SerializeField]
        private GameObject button;

        public void Enable()
        {
            if (!removeButton.activeSelf && !quantityButton.activeSelf)
            {
                removeButton.SetActive(true);
                quantityButton.SetActive(true);
            }
            else return;
        }

        public void Disable()
        {
            if (removeButton.activeSelf && quantityButton.activeSelf)
            {
                removeButton.SetActive(false);
                quantityButton.SetActive(false);
            }
            else return;
        }

        public void Remove()
        {
            var itemSlot = GetComponentInParent<InventoryItemLogic>();
            if (itemSlot == null)
            {
                Debug.LogWarning("Buttons.remove: no InventoryItemLogic found in parent chain.");
                return;
            }

            int index = itemSlot.SlotIndex;
            var controller = FindObjectOfType<InventoryController>(true);
            if (controller == null)
            {
                Debug.LogWarning("Buttons.remove: InventoryController not found in scene.");
                return;
            }

            controller.InventoryObject.RemoveItem(index);
            Debug.Log($"Remove clicked for slot index: {index}");
        }

        public void IncreaseQuantity()
        {
            var itemSlot = GetComponentInParent<InventoryItemLogic>();
            if (itemSlot == null)
            {
                Debug.LogWarning("Buttons.remove: no InventoryItemLogic found in parent chain.");
                return;
            }

            int index = itemSlot.SlotIndex;
            var controller = FindObjectOfType<InventoryController>(true);
            if (controller == null)
            {
                Debug.LogWarning("Buttons.remove: InventoryController not found in scene.");
                return;
            }
            int itemQ = controller.InventoryObject.GetItemAt(index).Quantity;
            controller.InventoryObject.ManipulateItem(index, itemQ+1, itemQ);
            Debug.Log($"Remove clicked for slot index: {index}");
        }
    }
}
