using UnityEngine;
using Save;
using Inventory;

namespace LevelObjects.Interactable
{ 
    public class Bridge : Interactable
{
    [Header("Bridge Objects")]
    [SerializeField] private GameObject brokenBridge;
    [SerializeField] private GameObject fixedBridge;

    [Header("Required Items")]
    [SerializeField] private string woodItemId = "Wood";
    [SerializeField] private string screwItemId = "Screw";
    
    private InventoryObject _inventory;
    private bool isRepaired = false;

    protected override void Awake()
    {
        base.Awake();
        
        var controller = FindFirstObjectByType<InventoryController>(FindObjectsInactive.Include);
        if (controller != null)
        {
            _inventory = controller.InventoryObject;
        }
        else
        {
            Debug.LogError("No InventoryController found in scene!");
        }
        UpdateVisuals();
    }
    
    //Place Holder to test if Bridge can be built
// #if UNITY_EDITOR
//     private void Update()
//     {
//         if (Input.GetKeyDown(KeyCode.T))
//         {
//             isRepaired = false; // allow re-testing
//             Repair();
//         }
//     }
// #endif
    public override void Interact()
    {
        if (isRepaired) return;
        if (_inventory == null) return;
        
        int woodIndex = _inventory.FindItemIndexWithName(woodItemId);
        int screwIndex = _inventory.FindItemIndexWithName(screwItemId);

        if (woodIndex == -1)
        {
            Debug.LogWarning("No wood item in inventory!");
            return;
        }
        if (screwIndex == -1)
        {
            Debug.LogWarning("No screws in your inventory");
            return;
        }
        
        //Consumes one of each
        int woodQty = _inventory.GetItemAt(woodIndex).Quantity;
        int screwQty = _inventory.GetItemAt(screwIndex).Quantity;
        
        if (woodQty > 1)
        {
            _inventory.ChangeQuantityAt(woodIndex, woodQty - 1);
        }
        else 
        {
            _inventory.RemoveItem(woodIndex);
        }
        if (screwQty > 1)
        {
            _inventory.ChangeQuantityAt(screwIndex, screwQty -1);
        }
        else
        {
            _inventory.RemoveItem(screwIndex);
        }
        Repair();
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
