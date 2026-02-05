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
        
        [SerializeField] private bool isDefaultSpawn = true;
        [SerializeField] private SpawnType spawnType = SpawnType.Default;
        [SerializeField] private string spawnID; 
        
        public bool IsDefaultSpawn => isDefaultSpawn;
        public SpawnType Type => spawnType;
        public string SpawnID => spawnID;

        private void OnDrawGizmos()
        {
            // Visualize spawn point in editor
            Gizmos.color = isDefaultSpawn ? Color.green : Color.yellow;
            Gizmos.DrawWireSphere(transform.position, 0.5f);
            Gizmos.DrawLine(transform.position, transform.position + Vector3.up * 2f);
        }
        public void SetDefault()
        {
            spawnType=SpawnType.Default;
            Debug.Log("set default");
        }
        public void SetCheckpoint()
        {
            spawnType=SpawnType.Checkpoint;
            Debug.Log("set checkpoint");
        }
        public void SetRespawn()
        {
            spawnType=SpawnType.Respawn;
            Debug.Log("set Respawn");
        }
    }
}
