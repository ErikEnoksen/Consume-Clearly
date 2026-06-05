// =============================================================================
// PlayerSpawnPoint.cs — Player Spawn Position Marker
// 
//
// PURPOSE:
//   A scene marker that tells PlayerManager where to place the player.
//   Place one or more in each gameplay scene. PlayerManager.GetSpawnPosition()
//   reads all of them and picks the one marked IsDefaultSpawn.
//
// SPAWN TYPES:
//   • Default    — the primary spawn when the scene first loads
//   • Checkpoint — updated at runtime when the player reaches a save point
//   • Respawn    — used for fall / death respawn positions
//   Type can be changed at runtime via SetDefault / SetCheckpoint / SetRespawn
//   (e.g. called from a trigger when the player passes a checkpoint).
//
// GIZMOS:
//   Green sphere = default spawn, Yellow = non-default.
//   Vertical line helps locate the marker in a busy scene.
// =============================================================================

using UnityEngine;

namespace Player
{
    public class PlayerSpawnPoint : MonoBehaviour
    {
        public enum SpawnType
        {
            Default,
            Checkpoint,
            Respawn
        }

        // Mark one spawn point per scene as the default — PlayerManager picks this first.
        [SerializeField] private bool isDefaultSpawn = true;
        [SerializeField] private SpawnType spawnType = SpawnType.Default;

        // Optional ID for referencing specific spawn points by name (e.g. scene transitions).
        [SerializeField] private string spawnID;

        public bool IsDefaultSpawn => isDefaultSpawn;
        public SpawnType Type => spawnType;
        public string SpawnID => spawnID;

        // Green = default spawn, Yellow = non-default, so designers can tell them apart at a glance.
        private void OnDrawGizmos()
        {
            Gizmos.color = isDefaultSpawn ? Color.green : Color.yellow;
            Gizmos.DrawWireSphere(transform.position, 0.5f);
            Gizmos.DrawLine(transform.position, transform.position + Vector3.up * 2f);
        }

        // --- Runtime Type Setters ---
        // Called by checkpoint triggers or other events to reclassify this spawn point.
        public void SetDefault()
        {
            spawnType = SpawnType.Default;
            Debug.Log("set default");
        }

        public void SetCheckpoint()
        {
            spawnType = SpawnType.Checkpoint;
            Debug.Log("set checkpoint");
        }

        public void SetRespawn()
        {
            spawnType = SpawnType.Respawn;
            Debug.Log("set Respawn");
        }
    }
}
