// =============================================================================
// InteractableObjectState.cs — Per-Object Save Snapshot
//
//
// PURPOSE:
//   The data bag that each ISaveable object fills out when SaveState() is called.
//   GameManager collects one of these per saveable object and stores them all
//   in SaveData.InteractableStates. On load, each is matched to its object by
//   uniqueId and passed back via LoadState().
//
// FIELDS:
//   Most fields are optional — each ISaveable only fills what it needs.
//   Unused fields serialize as default values (0, false, null) and are ignored
//   by LoadState() on the receiving object.
//
//   uniqueId            — must match the object's GetUniqueId() exactly
//   isActive            — general on/off toggle (e.g. auto-trigger fired, item collected)
//   position / rotation — world transform snapshot for objects that can be moved
//   ropeVariantName     — which rope prefab is attached to a Hook (null = no rope)
//   dialogueStageIndex  — which conversation stage an NPC is currently on
//   conversationsOnStage— how many times the current stage has been played
//   friendshipLevel     — companion's current friendship XP level
//   friendshipMood      — companion's mood as an int (cast from CompanionMood enum)
//   dailyConversationGiven — whether the friendship bonus was already given today
// =============================================================================

using System;
using UnityEngine;

namespace Save
{
    [Serializable]
    public class InteractableObjectState
    {
        // Required — used to match this snapshot back to its scene object on load.
        public string uniqueId;

        // General state toggle — meaning depends on the object (e.g. has auto-trigger fired).
        public bool isActive;

        // Transform snapshot — used by objects that can be physically moved.
        public Vector3 position;
        public Quaternion rotation;

        // Hook-specific: which rope variant is currently attached (null = empty hook).
        public string ropeVariantName;

        // Dialogue / NPC conversation progress.
        public int dialogueStageIndex;
        public int conversationsOnStage;

        // Companion friendship state — serialised as plain ints so no enum dependency.
        public int friendshipLevel;
        public int friendshipMood;
        public bool dailyConversationGiven;
    }
}
