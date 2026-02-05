using UnityEngine;

namespace Companion
{
    public class CompanionSpawnPoint : MonoBehaviour
    {
        public enum SpawnType
        {
            Default,
            Checkpoint,
            Respawn
        }

        [SerializeField] private GameObject companionPrefab;
        [SerializeField] private bool isDefaultSpawn = true;
        [SerializeField] private SpawnType spawnType = SpawnType.Default;
        [SerializeField] private string spawnID;

        public GameObject CompanionPrefab => companionPrefab;
        public bool IsDefaultSpawn => isDefaultSpawn;
        public SpawnType Type => spawnType;
        public string SpawnID => spawnID;

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

