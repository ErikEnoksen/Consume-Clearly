// =============================================================================
// SaveData.cs — Serialized Game State Snapshot
// 
//
// PURPOSE:
//   The single data container that gets written to disk as JSON. Every system
//   that needs to survive a save/load contributes its values here.
//   SaveSystem.cs handles the actual file I/O; GameManager fills and reads this.
//
// WHAT IS SAVED:
//   • Player        — position, money, hunger
//   • World         — current scene, game time, day/time of day
//   • Inventory     — all item slots (name, quantity, sprite name, etc.)
//   • Community     — community level and overflow XP points
//   • Satisfaction  — circular satisfaction meter value and passive flag
//   • Quests        — active quest objectives + list of completed quest IDs
//   • Interactables — per-object state for every ISaveable in the scene
//
// INVALID SCENE GUARD:
//   IsSceneValidForSaving() blocks saves from Credits and MainMenu scenes.
//   This prevents overwriting real progress with empty scene snapshots if
//   the game is saved or loaded while on those screens.
//
// SPRITE NOTE:
//   Sprite references can't be serialized to JSON, so only spriteName (a string)
//   is saved. The cachedSprite field is [NonSerialized] and gets re-resolved at
//   runtime from the sprite name after loading.
// =============================================================================

using System;
using System.Collections.Generic;
using UnityEngine;

namespace Save
{
    // --- Inventory Slot ---
    // A flat snapshot of one inventory slot. Sprites are stored by name
    // and resolved back to Sprite assets when the inventory is restored.
    [Serializable]
    public class InventorySlotData
    {
        public string itemName;
        public string itemID;
        public int quantity;
        public string itemDescription;
        public int maxStack;
        public string itemTag;
        public int sellPrice;
        public string spriteName;
        [NonSerialized] public UnityEngine.Sprite cachedSprite; // not serialized — rebuilt at load time
    }

    // --- Quest Objective State ---
    // Tracks how far along the player is on a single objective (e.g. collected 2/5 bottles).
    [Serializable]
    public class QuestObjectiveSaveState
    {
        public string objectiveID;
        public int currentAmount;
    }

    // --- Quest Save State ---
    // Groups all objectives for one active quest together.
    [Serializable]
    public class QuestSaveState
    {
        public string questID;
        public List<QuestObjectiveSaveState> objectives = new List<QuestObjectiveSaveState>();
    }

    // --- Save Data ---
    // The full game snapshot. Serialized to JSON by SaveSystem and deserialized on load.
    [Serializable]
    public class SaveData
    {
        // Core world state
        public float GameTime;
        public string CurrentScene;
        public Vector3 PlayerPosition;
        public int Money;
        public int CurrentDay;
        public float DayTimer;

        // Scene object states (one entry per ISaveable that returned a non-null SaveState)
        public List<InteractableObjectState> InteractableStates = new List<InteractableObjectState>();

        // Inventory
        public List<InventorySlotData> InventorySlots = new List<InventorySlotData>();

        // Community meter
        public int CommunityLevel;
        public int CommunityOverflowPoints;

        // Player systems
        public float HungerValue;
        public int SatisfactionValue;
        public bool IsPassivePointActive;

        // Quests
        public List<QuestSaveState> ActiveQuestStates = new List<QuestSaveState>();
        public List<string> CompletedQuestIDs = new List<string>();

        // Scenes excluded from saving — these are menus/credits, not gameplay scenes.
        private static readonly HashSet<string> InvalidScenes = new() { "Credits", "MainMenu" };

        // Returns false for non-gameplay scenes so SaveSystem refuses to write the file.
        public bool IsSceneValidForSaving()
        {
            return !string.IsNullOrEmpty(CurrentScene) && !InvalidScenes.Contains(CurrentScene);
        }
    }
}