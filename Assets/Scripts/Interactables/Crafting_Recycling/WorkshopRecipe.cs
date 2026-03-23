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
        public ItemSO item;
        public int amount;
    }

    public StationType stationType;
    public List<ItemStack> inputs;
    public List<ItemStack> outputs;
}
