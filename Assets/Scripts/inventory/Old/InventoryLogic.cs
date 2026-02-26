using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Inventory
{
    public class InventoryLogic : MonoBehaviour
    {
        [SerializeField]
        private InventoryItemLogic itemPrefab;
        [SerializeField]
        private RectTransform contentPanel;
        [SerializeField]
        private InventoryDescription descriptionPanel;

        private List<InventoryItemLogic> inventoryItems = new List<InventoryItemLogic>();

        public event Action<int> DescriptionRequested;

        private void Awake()
        {
            if (descriptionPanel != null)
            {
                descriptionPanel.ResetDescription();
            }
        }

        public void InitializeInventory(int size)
        {
            inventoryItems = new List<InventoryItemLogic>();
            contentPanel.DetachChildren();
            for (int i = 0; i < size; i++)
            {
                InventoryItemLogic newItem = Instantiate(itemPrefab, Vector3.zero, Quaternion.identity);
                newItem.transform.SetParent(contentPanel);
                inventoryItems.Add(newItem);
                newItem.OnItemClicked += HandleItemSelected;
                newItem.OnRightMouseBtnClick += HandleShowItemAction; 
                newItem.SetSlotIndex(i);
            }
        }

        public void UpdateData(int index, Sprite sprite, int quantity, bool isUsable)
        {
            if (index < 0 || index >= inventoryItems.Count) return;
            inventoryItems[index].SetData(sprite, quantity,isUsable);
        }

        public void ManipulateItemFunction(int index, bool isUsable)
        {
            Debug.Log("Manipulating item function called for index: " + index + " with emptying: " + isUsable);
            inventoryItems[index].SetEventTriggerEnabled(isUsable);
            ResetDescription();
        }

        private void HandleShowItemAction(InventoryItemLogic logic)
        {
            HideButtons();
            int index = inventoryItems.IndexOf(logic);
            if (index < 0) return;
            if (inventoryItems[index].HasItem) 
            {
                inventoryItems[index].EnableActionPanel();
            }
        }

        private void HandleItemSelected(InventoryItemLogic logic)
        {
            Debug.Log("Item selected");
            ResetDescription();
            int index = inventoryItems.IndexOf(logic);
            if (index < 0) return;
            DescriptionRequested?.Invoke(index);
        }

        public void Show() 
        {
            gameObject.SetActive(true);
            ResetDescription();
        }

        public void Hide() 
        {
            gameObject.SetActive(false);
            ResetDescription();
        }

        public void UpdateDescription(int itemIndex, Sprite itemSprite, string name, string description)
        {
            descriptionPanel.SetDescription(itemSprite, name, description);
            DeselectAllSelections();
            inventoryItems[itemIndex].Select();
        }

        public void ResetDescription()
        {
            descriptionPanel.ResetDescription();
            DeselectAllSelections();
            HideButtons();
        }

        private void DeselectAllSelections()
        {
            foreach (InventoryItemLogic item in inventoryItems)
            {
                item.Deselect();
            }

        }

        private void HideButtons()
        {
            foreach (InventoryItemLogic item in inventoryItems)
            {
                item.DisableActionPanel();
            }
        }

        public void InventorySizeTest()
        {
            Debug.Log("Inventory size is: " + inventoryItems.Count);
        }
    }
}
