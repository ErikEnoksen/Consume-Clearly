// =============================================================================
// CompanionSpawnPoint.cs — Companion Spawn Marker
// 
//
// PURPOSE:
//   A scene marker that tells CompanionManager where to place a companion and
//   which prefab to use. Place one in each scene where a companion should appear.
//   CompanionManager reads all spawn points on scene load and either spawns
//   new companions or teleports existing ones to their matched position.
//
// SPAWN ID:
//   The SpawnID string links this marker to a companion instance across scenes.
//   CompanionManager uses it as the key in its spawnedCompanions dictionary.
//   Must be unique per scene — duplicate IDs are skipped with a warning.
//
// SPAWN TYPES:
//   • Default    — standard placement, used in most cases
//   • Checkpoint — reserved for checkpoint-based respawn logic (future use)
//   • Respawn    — reserved for explicit respawn points (future use)
//
// GIZMOS:
//   Cyan sphere = default spawn, Yellow = non-default.
//   The companion name / SpawnID is labelled in the editor for quick identification.
// =============================================================================

using UnityEngine;

namespace Companion
{
    public class CompanionSpawnPoint : MonoBehaviour
    {
        // Categorises the intent of this spawn point — currently informational,
        // can be used by CompanionManager to filter spawn behaviour in the future.
        public enum SpawnType
        {
            Default,
            Checkpoint,
            Respawn
        }

        // The companion prefab to instantiate at this position.
        [SerializeField] private GameObject companionPrefab;

        // If true, this is the primary spawn for this companion in the scene.
        [SerializeField] private bool isDefaultSpawn = true;

        [SerializeField] private SpawnType spawnType = SpawnType.Default;

        // Unique key used by CompanionManager to track and match this companion across scenes.
        [SerializeField] private string spawnID;

        // --- Public Accessors ---
        public GameObject CompanionPrefab => companionPrefab;
        public bool IsDefaultSpawn => isDefaultSpawn;
        public SpawnType Type => spawnType;
        public string SpawnID => spawnID;

        // --- Editor Gizmos ---
        // Draws a sphere and vertical line at the spawn position so designers can
        // see companion placements in the Scene view without entering Play mode.
        private void OnDrawGizmos()
        {
            Gizmos.color = isDefaultSpawn ? Color.cyan : Color.yellow;
            Gizmos.DrawWireSphere(transform.position, 0.4f);
            Gizmos.DrawLine(transform.position, transform.position + Vector3.up * 1.5f);

            if (companionPrefab != null)
            {
                Vector3 labelPos = transform.position + Vector3.up * 0.6f;
                #if UNITY_EDITOR
                UnityEditor.Handles.Label(labelPos, string.IsNullOrEmpty(spawnID) ? companionPrefab.name : spawnID);
                #endif
            }
        }
    }
}

