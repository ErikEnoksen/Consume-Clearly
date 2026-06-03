// =============================================================================
// InstanceIdentifier.cs — Stable Scene Object GUID
// 
//
// PURPOSE:
//   Attaches a persistent GUID to any scene object that needs to be tracked
//   by the save system. Add this component alongside ISaveable when the object's
//   uniqueId should be stable across editor sessions, prefab placements, and builds.
//
// WHY THIS EXISTS:
//   ISaveable requires GetUniqueId() to return a stable string. If each script
//   generates its own GUID in OnValidate/Awake, two components on the same object
//   might disagree. InstanceIdentifier is a single source of truth — other
//   components read its Id property instead of managing their own.
//
// ID ASSIGNMENT:
//   • Editor (Reset/OnValidate) — GUID is written when the component is first added
//     or when the Inspector revalidates. SetDirty ensures it is saved to the scene/prefab.
//   • Runtime (Awake) — EnsureId() generates a GUID if somehow none was set,
//     so runtime-spawned objects always have an ID.
//   • DisallowMultipleComponent prevents two InstanceIdentifiers on the same object.
// =============================================================================

using System;
using UnityEngine;

namespace Save
{
    [DisallowMultipleComponent]
    public class InstanceIdentifier : MonoBehaviour
    {
        [SerializeField] private string id;
        public string Id => id;

#if UNITY_EDITOR
        // Assign a GUID when the component is first added to an object in the editor.
        private void Reset()
        {
            EnsureId();
            UnityEditor.EditorUtility.SetDirty(this);
        }

        // Re-check on every Inspector validation pass, but only outside Play mode so
        // the prefab asset itself stays empty (instances get their own unique IDs).
        private void OnValidate()
        {
            if (!Application.isPlaying)
            {
                EnsureId();
                UnityEditor.EditorUtility.SetDirty(this);
            }
        }
#endif

        // Safety fallback for runtime-spawned objects that didn't go through the editor path.
        private void Awake()
        {
            EnsureId();
        }

        private void EnsureId()
        {
            if (string.IsNullOrEmpty(id))
                id = Guid.NewGuid().ToString();
        }
    }
}