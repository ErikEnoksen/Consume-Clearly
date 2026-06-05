// =============================================================================
// BrokenStructure.cs - Allows the repairing of structures
//
// PURPOSE:
//   Through this script structures can be repaired with the necessary materials.
//   If the player has the materials then these will be removed to repair the structure.
// =============================================================================
using UnityEngine;
using Save;
using System.Collections.Generic;
using Assets.Scripts.Quests;

namespace LevelObjects.Interactable
{ 
    public class Bridge : Interactable
    {
    [Header("Quest related ID")]
    [SerializeField] private string repairObjectiveID;

    [Header("Strucure Objects")]
    [SerializeField] private GameObject brokenStrucutre;
    [SerializeField] private GameObject fixedStructure;

    [Header("Required Items")]
    [SerializeField] private Item[] requiredItems;


    List<InventoryItem> items = new List<InventoryItem>();
    private InventoryManager _inventory;
    private bool isRepaired = false;

    protected override void Awake()
    {
        base.Awake();
        
        _inventory = FindFirstObjectByType<InventoryManager>();
        
        if (_inventory == null) 
            
            Debug.LogError("No InventoryManagerTest found in scene!");
        
        UpdateVisuals();
    }

    public override void Interact()
    {
        if (isRepaired || _inventory == null) return;
        
            foreach(var item in requiredItems)
            {
                InventoryItem inventoryItem = FindItemByName(item.ItemName);
                
                if (inventoryItem != null && inventoryItem.quantity > 0)
                {
                    items.Add(inventoryItem);
                }
            }

            if(!(requiredItems.Length == items.Count))
            {
                return;
            }

            foreach (var item in items)
            {
                item.RemoveItem(1);
            }
            
            Repair();
        
    }
    private InventoryItem FindItemByName(string itemName)
    {
        foreach (var slot in _inventory.inventoryItems)
        {
            Debug.Log("Checking slot: " + slot.itemName + " qty: " + slot.quantity);
            if (slot.itemName == itemName && slot.quantity > 0)
                return slot;
        }
        return null;
    }

    private void Repair()
    {
        isRepaired = true;
        UpdateVisuals();
        Debug.Log("Bridge repaired!");

        if (repairObjectiveID != null)
        {
            QuestController.Instance?.UpdateObjectiveProgress(
                repairObjectiveID,
                objectiveType.RepairObject,
                1
                );
        }
    }
    
    private void UpdateVisuals()
    {
        if (brokenStrucutre != null) brokenStrucutre.SetActive(!isRepaired);
        if (fixedStructure  != null) fixedStructure.SetActive(isRepaired);
    }
    public override InteractableObjectState SaveState()
    {
        return new InteractableObjectState
        {
            uniqueId    = GetUniqueId(),
            isActive = isRepaired
        };
    }

    public override void LoadState(InteractableObjectState state)
    {
        isRepaired = state.isActive;
        UpdateVisuals();
    }

}
}
