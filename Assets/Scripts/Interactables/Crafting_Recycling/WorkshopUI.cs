// =============================================================================
// WorkshopUI.cs — Workshop Panel Display & Interaction
// 
//
// PURPOSE:
//   Handles everything the player sees and clicks inside the workshop panel.
//   WorkshopStation owns the interaction trigger; this class owns the UI.
//   When the station calls Open(), this panel populates itself from the
//   station's recipe list and stays in sync after each process attempt.
//
// FLOW:
//   WorkshopStation.Interact() → Open(station)
//     → PopulateRecipeList()   — builds one button per available recipe
//     → SelectRecipe(recipe)   — shows ingredients with green/red availability
//     → OnProcessClicked()     — tells the station to attempt processing,
//                                then refreshes the ingredient list in-place
//
// PLAYER MOVEMENT:
//   OnEnable/OnDisable freeze and unfreeze player movement so the player
//   can't walk away while the panel is open.
//
// INGREDIENT DISPLAY:
//   Each ingredient row shows: icon | name | playerHas/required (green if enough, red if not).
//   The Process button is greyed out (non-interactable) when any ingredient is short.
//
// NOTE:
//   The UI re-populates after a successful process so quantities update live
//   without closing and reopening the panel.
// =============================================================================

using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections.Generic;
using Player;

public class WorkshopUI : MonoBehaviour
{
    // --- Inspector References ---
    // Scroll list where recipe buttons are spawned.
    [SerializeField] private Transform recipeListParent;

    // Scroll list where ingredient rows are spawned for the selected recipe.
    [SerializeField] private Transform ingredientListParent;

    // Prefab instantiated once per recipe in the recipe list.
    [SerializeField] private GameObject recipeButtonPrefab;

    // Prefab instantiated once per ingredient when a recipe is selected.
    [SerializeField] private GameObject ingredientRowPrefab;

    // The "Process" button — enabled/disabled based on ingredient availability.
    [SerializeField] private Button processButton;

    // --- Runtime State ---
    private WorkshopStation station;        // the station that opened this panel
    private WorkshopRecipe selectedRecipe;  // the recipe the player has currently highlighted
    private InventoryManager inventoryManager;

    // --- Open ---
    // Called by WorkshopStation when the player interacts. Sets context and populates the list.
    public void Open(WorkshopStation callingStation)
    {
        station = callingStation;
        inventoryManager = GameObject.Find("InventorySelector").GetComponent<InventoryManager>();
        gameObject.SetActive(true);
        PopulateRecipeList();
    }

    // --- Enable / Disable ---
    // Freeze player movement while the panel is open so the player stays in place.
    void OnEnable()
    {
        if (station != null)
            PopulateRecipeList();

        var player = PlayerManager.Instance?.GetPlayer();
        player?.GetComponent<MovementScript>()?.FreezeMovement(true);
    }

    void OnDisable()
    {
        var player = PlayerManager.Instance?.GetPlayer();
        player?.GetComponent<MovementScript>()?.FreezeMovement(false);
    }

    // --- Recipe List ---
    // Clears and rebuilds the recipe button list from the station's available recipes.
    // Each button shows the recipe name and the icon of the first output item.
    void PopulateRecipeList()
    {
        foreach (Transform child in recipeListParent)
            Destroy(child.gameObject);

        foreach (var recipe in station.GetAvailableRecipes())
        {
            GameObject btn = Instantiate(recipeButtonPrefab, recipeListParent);
            btn.GetComponentInChildren<TMP_Text>().text = recipe.name;
            btn.GetComponentInChildren<Image>().sprite = recipe.outputPrefabs.Count > 0
                ? recipe.outputPrefabs[0].itemPrefab.GetComponent<Item>().Sprite : null;
            btn.GetComponent<Button>().onClick.AddListener(() => SelectRecipe(recipe));
        }
    }

    // --- Recipe Selection ---
    // Rebuilds the ingredient panel for the chosen recipe.
    // Each row shows the ingredient icon, name, and how many the player has vs. how many are needed.
    // Green = enough, Red = not enough. The Process button state is also updated here.
    void SelectRecipe(WorkshopRecipe recipe)
    {
        selectedRecipe = recipe;

        foreach (Transform child in ingredientListParent)
            Destroy(child.gameObject);

        foreach (var input in recipe.inputs)
        {
            Item item = input.itemPrefab.GetComponent<Item>();
            int playerHas = GetItemCount(item.ItemName);
            bool enough = playerHas >= input.amount;

            GameObject row = Instantiate(ingredientRowPrefab, ingredientListParent);
            TMP_Text[] texts = row.GetComponentsInChildren<TMP_Text>();
            Image icon = row.GetComponentInChildren<Image>();
            if (icon != null) { icon.sprite = item.Sprite; }

            texts[0].text = item.ItemName;
            texts[1].text = $"{Mathf.Min(playerHas, input.amount)}/{input.amount}";
            texts[1].color = enough ? Color.green : Color.red;
        }

        // Update the process button — greyed out if the player can't afford this recipe.
        processButton.interactable = station.CanProcess(recipe);
        processButton.onClick.RemoveAllListeners();
        processButton.onClick.AddListener(() => OnProcessClicked());
    }

    // --- Process Button ---
    // Asks the station to attempt the selected recipe. On success, refreshes the
    // ingredient display in-place so the player can see updated quantities immediately.
    void OnProcessClicked()
    {
        if (station.TryProcess(selectedRecipe))
            SelectRecipe(selectedRecipe);
    }

    // --- Inventory Helper ---
    // Counts how many of a named item the player has — mirrors the check in WorkshopStation
    // so the UI can display quantities without going through the station.
    private int GetItemCount(string itemName)
    {
        int total = 0;
        foreach (var slot in inventoryManager.inventoryItems)
            if (slot.itemName == itemName)
                total += slot.quantity;
        return total;
    }
}
