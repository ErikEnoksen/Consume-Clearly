using System;
using System.Collections.Generic;
using UnityEngine;


namespace Save
{
    [Serializable]
    public class InventorySlotData
    {
        public string itemName;
        public string itemID;
        public int quantity;
        public string itemDescription;
        public int maxStack;
        public string itemTag;
        public string spriteName;
    }

    [Serializable]
    public class SaveData
    {
        public float GameTime;
        public string CurrentScene;
        public Vector3 PlayerPosition;
        public int Money;
        public List<InteractableObjectState> InteractableStates = new List<InteractableObjectState>();
        public List<InventorySlotData> InventorySlots = new List<InventorySlotData>();

        private static readonly HashSet<string> InvalidScenes = new() { "Credits", "MainMenu" };

        public bool IsSceneValidForSaving()
        {
            return !string.IsNullOrEmpty(CurrentScene) && !InvalidScenes.Contains(CurrentScene);
        }
    }
}