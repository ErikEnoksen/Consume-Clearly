using UnityEngine;
using Save;

namespace LevelObjects.Interactable
{ 
    public class Bridge : Interactable
{
    [Header("Bridge Objects")]
    [SerializeField] private GameObject brokenBridge;
    [SerializeField] private GameObject fixedBridge;

    [Header("Required Items")]
    [SerializeField] private string plankItemId = "Plank";
    [SerializeField] private string screwItemId = "Screw";
    
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
        
        InventoryItem plank = FindItemByName(plankItemId);
        InventoryItem screw = FindItemByName(screwItemId);

        if (plank == null) {Debug.LogError("No Plank item found in inventory!"); return;}
        if (screw == null) {Debug.LogError("No screws left in inventory!"); return;}
        
        
        if (plank.quantity > 0) plank.RemoveItem(1);
        
        if (screw.quantity > 0) screw.RemoveItem(1);
        
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
    }
    
    private void UpdateVisuals()
    {
        if (brokenBridge != null) brokenBridge.SetActive(!isRepaired);
        if (fixedBridge  != null) fixedBridge.SetActive(isRepaired);
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
