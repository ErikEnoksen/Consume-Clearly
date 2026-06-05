// =============================================================================
// WorkshopRecipe.cs — Recipe Data Asset (ScriptableObject)
// 
//
// PURPOSE:
//   Defines a single crafting/recycling recipe. Create one per operation via
//   Assets → Create → Scriptable Objects → WorkshopRecipe.
//   WorkshopStation reads these at runtime to know what to consume and produce.
//
// STRUCTURE:
//   • stationType  — which station this recipe belongs to (must match the station)
//   • inputs       — list of items and quantities the player must have to process
//   • outputPrefabs — list of items produced when the recipe succeeds
//
// STATION TYPES (StationType enum):
//   Each station type is its own category. Adding a new station type here makes
//   it available to assign to new WorkshopStation instances in the scene.
//   Current types: RecyclingBin, RepairShop, CraftingBench, TradingBench.
//
// HOW IT CONNECTS:
//   WorkshopStation.GetAvailableRecipes() filters the recipe list by stationType.
//   WorkshopStation.TryProcess() validates inputs against the player inventory,
//   removes them, and adds the outputs.
// =============================================================================

using System.Collections.Generic;
using UnityEngine;

// Identifies which kind of workshop station a recipe belongs to.
// Adding a new value here is all that's needed to support a new station type.
public enum StationType
{
    RecyclingBin,
    RepairShop,
    CraftingBench,
    TradingBench,
}

[CreateAssetMenu(fileName = "Workshop Recipe", menuName = "Scriptable Objects/WorkshopRecipe")]
public class WorkshopRecipe : ScriptableObject
{
    // --- Input Definition ---
    // One entry per required ingredient. The station checks the player's inventory
    // for itemPrefab's Item component name and the required amount.
    [System.Serializable]
    public class ItemStack
    {
        public GameObject itemPrefab;
        public int amount;
    }

    // Which station this recipe can be processed at.
    public StationType stationType;

    // All ingredients that must be present before processing can start.
    public List<ItemStack> inputs;

    // --- Output Definition ---
    // What the player receives after a successful process. Each entry spawns separately.
    [System.Serializable]
    public class ItemOutput
    {
        public GameObject itemPrefab;
        public int amount;
    }

    // Items added to the player's inventory when this recipe completes.
    public List<ItemOutput> outputPrefabs;
}