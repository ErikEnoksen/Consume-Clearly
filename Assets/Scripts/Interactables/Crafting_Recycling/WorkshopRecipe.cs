using System.Collections.Generic;
using UnityEngine;

//This opens the door for other Resycling Stations in the Future hopefully it will be easy to implement
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
    [System.Serializable]
    public class ItemStack
    {
        public GameObject itemPrefab;
        public int amount;
    }

    public StationType stationType;
    public List<ItemStack> inputs;

    [System.Serializable]
    public class ItemOutput
    {
        public GameObject itemPrefab;
        public int amount;
    }

    public List<ItemOutput> outputPrefabs;
}