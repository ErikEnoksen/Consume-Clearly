using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections.Generic;

public class WorkshopUI : MonoBehaviour
{
    
    [SerializeField] private WorkshopStation station;
    [SerializeField] private Transform recipeListParent;
    [SerializeField] private Transform ingredientListParent;
    [SerializeField] private GameObject recipeButtonPrefab;
    [SerializeField] private GameObject ingredientRowPrefab;
    [SerializeField] private Button processButton;
    
    private WorkshopRecipe selectedRecipe;
    private InventoryManager inventoryManager;
    
    void OnEnable()
    {
        inventoryManager = GameObject.Find("InventorySelector").GetComponent<InventoryManager>();
        PopulateRecipeList();
    }

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
    
    void SelectRecipe(WorkshopRecipe recipe)
    {
        selectedRecipe = recipe;

        foreach (Transform child in ingredientListParent)
            Destroy(child.gameObject);

        foreach (var input in recipe.inputs)
        {
            int playerHas = GetItemCount(input.itemName);
            bool enough = playerHas >= input.amount;

            GameObject row = Instantiate(ingredientRowPrefab, ingredientListParent);
            TMP_Text[] texts = row.GetComponentsInChildren<TMP_Text>();
            Image icon = row.GetComponentInChildren<Image>();
            if (icon != null) {icon.sprite = input.icon;}
            
            texts[0].text = input.itemName;
            texts[1].text = $"{Mathf.Min(playerHas, input.amount)}/{input.amount}";
            texts[1].color = enough ? Color.green : Color.red;
        }

        processButton.interactable = station.CanProcess(recipe);
        processButton.onClick.RemoveAllListeners();
        processButton.onClick.AddListener(() => OnProcessClicked());
    }
    
    void OnProcessClicked()
    {
        if (station.TryProcess(selectedRecipe))
            SelectRecipe(selectedRecipe);
    }

    private int GetItemCount(string itemName)
    {
        int total = 0;
        foreach (var slot in inventoryManager.inventoryItems)
            if (slot.itemName == itemName)
                total += slot.quantity;
        return total;
    }
}
