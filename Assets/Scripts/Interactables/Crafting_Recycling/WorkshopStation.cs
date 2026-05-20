using System;
using System.Collections.Generic;
using LevelObjects.Interactable;
using UnityEngine;
using Save;

public class WorkshopStation : Interactable
{
    
    //StationType can expand into like BicycleRepair, Clothing Repair or multiple different instances.
    //[SerializeField] private StationType stationType;
    [SerializeField] private StationType stationType;
    [SerializeField] private List<WorkshopRecipe> recipes;
    [SerializeField] private GameObject workshopUI;
    
    private InventoryManager inventoryManager;
    private CircularSatisfactionMeter satisfactionMeter;

    private int CSIncreaseAmount = 10;

    void Start()
    {
        inventoryManager = GameObject.Find("InventorySelector").GetComponent<InventoryManager>();
        satisfactionMeter = GameObject.Find("CircularSatisfactionMeter").GetComponent<CircularSatisfactionMeter>();
    }
    public override void Interact()
    {
        if (workshopUI.activeSelf)
        {
            workshopUI.SetActive(false);
        }
        else
        {
            workshopUI.GetComponent<WorkshopUI>().Open(this);
        }
    }
    public override InteractableObjectState SaveState()
    {
        return new InteractableObjectState { uniqueId = GetUniqueId() };
    }
    
    public override void LoadState(InteractableObjectState state) { }
    
    public bool TryProcess(WorkshopRecipe recipe)
    {
        if (recipe.stationType != stationType) { return false; }

        foreach (var input in recipe.inputs)
        {
            Item item = input.itemPrefab.GetComponent<Item>();
            if (!HasItem(item.ItemName, input.amount))
            {
                Debug.Log($"Missing: {item.ItemName} x{input.amount}");
                return false;
            }
        }

        foreach (var input in recipe.inputs)
            inventoryManager.RemoveItem(input.itemPrefab.GetComponent<Item>().ItemName, input.amount);

        foreach (var output in recipe.outputPrefabs)
        {
            Item item = output.itemPrefab.GetComponent<Item>();
            inventoryManager.AddItem(item, 1);
        }

        // Increase Circular Satisfaction Meter
        if (satisfactionMeter != null)
        {
            satisfactionMeter.IncreaseSatisfactionValue(CSIncreaseAmount);
        }

        Debug.Log($"Processed at {stationType}!");
        return true;
    }

    public bool CanProcess(WorkshopRecipe recipe)
    {
        foreach (var input in recipe.inputs)
            if (!HasItem(input.itemPrefab.GetComponent<Item>().ItemName, input.amount))
                return false;
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
