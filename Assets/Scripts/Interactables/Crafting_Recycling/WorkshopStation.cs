// =============================================================================
// WorkshopStation.cs — Interactable Workshop Scene Object
// 
//
// PURPOSE:
//   The scene-side counterpart to WorkshopRecipe. Place this on a workshop
//   object in the world (RecyclingBin, CraftingBench, etc.). When the player
//   interacts with it, it opens the WorkshopUI and passes itself as context
//   so the UI knows which recipes to show.
//
// FLOW:
//   Player interacts → Interact() → WorkshopUI.Open(this)
//     → player selects a recipe → WorkshopUI calls TryProcess()
//       → ingredients removed, outputs added, satisfaction meter ticked
//
// UI LOCKING:
//   Registers with UIManager as an IUILockable. While the workshop panel is
//   open, UIManager holds a Workshop lock that prevents other UI panels
//   (inventory, dialogue, etc.) from opening at the same time.
//
// CIRCULAR SATISFACTION METER:
//   Every successful process increases the CS meter by CSIncreaseAmount (10).
//   This rewards the player for using the recycling/crafting system.
//
// SAVE / LOAD:
//   The station itself has no meaningful state to save — only its uniqueId is
//   stored. If per-station state is needed later, extend SaveState/LoadState.
// =============================================================================

using System;
using System.Collections.Generic;
using LevelObjects.Interactable;
using UnityEngine;
using Save;

public class WorkshopStation : Interactable, IUILockable
{
    // --- Inspector Fields ---
    // StationType filters which recipes this station can use.
    // Can be extended to BicycleRepair, ClothingRepair, etc. without changing any logic.
    [SerializeField] private StationType stationType;

    // All recipes assigned to this station — filtered at runtime by stationType.
    [SerializeField] private List<WorkshopRecipe> recipes;

    // The UI panel GameObject that gets shown/hidden on interact.
    [SerializeField] private GameObject workshopUI;

    // --- Scene References (resolved at Start) ---
    private InventoryManager inventoryManager;
    private CircularSatisfactionMeter satisfactionMeter;

    // --- Configuration ---
    private int CSIncreaseAmount = 10; // points added to the satisfaction meter per successful process
    private bool _isLocked = false;

    // --- Initialization ---
    // Find scene dependencies and register with UIManager for lock management.
    void Start()
    {
        inventoryManager = GameObject.Find("InventorySelector").GetComponent<InventoryManager>();
        satisfactionMeter = GameObject.Find("Sliders").GetComponent<CircularSatisfactionMeter>();
        UIManager.Instance?.RegisterUI(this);
    }

    private void OnDestroy()
    {
        UIManager.Instance?.UnregisterUI(this);
    }

    // --- UI Lock ---
    // Called by UIManager when another panel takes priority (e.g. dialogue opens).
    // Force-closes the workshop so two panels don't overlap.
    public void SetLocked(bool locked)
    {
        _isLocked = locked;
        if (locked && workshopUI.activeSelf)
        {
            workshopUI.SetActive(false);
            UIManager.Instance?.RemoveLock(UIManager.UILockType.Workshop);
        }
    }

    // --- Interact ---
    // Toggles the workshop panel open or closed. Respects the UIManager lock.
    public override void Interact()
    {
        if (_isLocked) return;

        if (workshopUI.activeSelf)
        {
            CloseWorkshop();
            UIManager.Instance?.RemoveLock(UIManager.UILockType.Workshop);
        }
        else
        {
            workshopUI.GetComponent<WorkshopUI>().Open(this);
            UIManager.Instance?.AddLock(UIManager.UILockType.Workshop);
        }
    }

    // Called externally (e.g. a close button in the UI) to shut the panel.
    public void CloseWorkshop()
    {
        workshopUI.SetActive(false);
        UIManager.Instance?.RemoveLock(UIManager.UILockType.Workshop);
    }

    // --- Save / Load ---
    // No per-station runtime state to persist — only the ID is needed for the save system.
    public override InteractableObjectState SaveState()
    {
        return new InteractableObjectState { uniqueId = GetUniqueId() };
    }

    public override void LoadState(InteractableObjectState state) { }

    // --- Recipe Processing ---
    // Validates that the player has all required ingredients, then removes them
    // and adds the outputs. Returns false early if anything is missing.
    public bool TryProcess(WorkshopRecipe recipe)
    {
        // Guard: recipe must belong to this station type.
        if (recipe.stationType != stationType) { return false; }

        // Validate all inputs before removing anything.
        foreach (var input in recipe.inputs)
        {
            Item item = input.itemPrefab.GetComponent<Item>();
            if (!HasItem(item.ItemName, input.amount))
            {
                Debug.Log($"Missing: {item.ItemName} x{input.amount}");
                return false;
            }
        }

        // All inputs confirmed — consume them.
        foreach (var input in recipe.inputs)
            inventoryManager.RemoveItem(input.itemPrefab.GetComponent<Item>().ItemName, input.amount);

        // Add each output item to the player's inventory.
        foreach (var output in recipe.outputPrefabs)
        {
            Item item = output.itemPrefab.GetComponent<Item>();
            inventoryManager.AddItem(item, 1);
        }

        // Reward the player with a satisfaction bump for using the system.
        if (satisfactionMeter != null)
        {
            satisfactionMeter.IncreaseSatisfactionValue(CSIncreaseAmount);
        }

        Debug.Log($"Processed at {stationType}!");
        return true;
    }

    // Quick read-only check — used by WorkshopUI to grey out the process button
    // when the player doesn't have enough materials.
    public bool CanProcess(WorkshopRecipe recipe)
    {
        foreach (var input in recipe.inputs)
            if (!HasItem(input.itemPrefab.GetComponent<Item>().ItemName, input.amount))
                return false;
        return true;
    }

    // Returns only the recipes that match this station's type.
    public List<WorkshopRecipe> GetAvailableRecipes()
    {
        return recipes.FindAll(r => r.stationType == stationType);
    }

    // --- Inventory Helper ---
    // Counts how many of a named item the player currently holds across all inventory slots.
    private bool HasItem(string itemName, int amount)
    {
        int total = 0;
        foreach (var slot in inventoryManager.inventoryItems)
        {
            if (slot.itemName == itemName)
                total += slot.quantity;
        }
        return total >= amount;
    }
}
