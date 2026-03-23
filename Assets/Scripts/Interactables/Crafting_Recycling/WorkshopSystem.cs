using System;
using System.Collections.Generic;
using UnityEngine;

public class WorkshopStation : MonoBehaviour
{
    
    //SationType can expand into like BicylceRepair, Cloting Repair or multiple differen instances.
    //[SerializeField] private StationType stationType;
    [SerializeField] private StationType stationType;
    [SerializeField] private List<WorkshopRecipe> recipes;
    
    private InventoryManager inventoryManager;
    
   
    void Start()
    {
        inventoryManager = GameObject.Find("InventorySelector").GetComponent<InventoryManager>();
    }
    
    public bool TryProcess(WorkshopRecipe recipe)
    {
        if (recipe.stationType != stationType) {return false;}

        foreach (var input in recipe.inputs)
        {
            if (!HasItem(input.item.itemName, input.amount))
            {
                Debug.Log($"Missing: {input.item.itemName} x{input.amount}");
                return false;
            }
        }

        foreach (var input in recipe.inputs)
            inventoryManager.RemoveItem(input.item.itemName, input.amount);

        foreach (var output in recipe.outputs)
            // inventoryManager.AddItem(
            //     output.item.itemName,
            //     output.item.itemName,
            //     output.amount,
            //     output.item.itemImage,
            //     output.item.itemDescription,
            //     output.item.maxStackSize,
            //     "Untagged"
            //);
            Debug.Log($"Processed at {stationType}!"); 
        return true;
    }
    public bool CanProcess(WorkshopRecipe recipe)
    {
        foreach (var input in recipe.inputs)
            if (!HasItem(input.item.itemName, input.amount))
            {
                return false;
            } 
        return true;
        
    }
    
    public List<WorkshopRecipe> GetAvailableRecipes()
    {
        return recipes.FindAll(r => r.stationType == stationType);
    }
    private bool HasItem(string itemName, int amount)
    {
        int total = 0;
        foreach (var slot in inventoryManager.inventoryItems)
        {
            if (slot.itemName == itemName)
            {
                total += slot.quantity;
            }
        }
        return total >= amount;
    }
}
