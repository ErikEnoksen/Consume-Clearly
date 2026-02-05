using System.Collections;
using Companion;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Player
{
    public class PlayerManager : MonoBehaviour
    {
        public static PlayerManager Instance { get; private set; }

        [SerializeField] private GameObject playerPrefab; // Reference to the player prefab

        private GameObject player;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);

                // Subscribe to the SceneManager.sceneLoaded event
                SceneManager.sceneLoaded += OnSceneLoaded;
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void OnDestroy()
        {
            // Unsubscribe from the SceneManager.sceneLoaded event to prevent duplicate calls
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            // Clear the player reference if the scene has changed
            if (player != null && player.scene != scene)
            {
                Destroy(player);
                player = null;
                Debug.Log("Player reference cleared due to scene change.");
            }

            // Spawn player only if it's not found in the scene and the scene isn't the MainMenu
            if (scene.name != "MainMenu" && player == null)
            {
                Debug.Log($"Spawning player in scene {scene.name}.");
                Vector3 spawnPosition = GetSpawnPosition();
                SpawnPlayer(spawnPosition);
            }
            else if (player != null)
            {
                SetupMovementScript(); // Ensure movement script is properly set up
            }
        }

        public void SetPlayerPosition(Vector3 position)
        {
            StartCoroutine(WaitAndSetPosition(position));
        }
        
        private IEnumerator WaitAndSetPosition(Vector3 position)
        {
            while (player == null)
                yield return null;

            var movement = player.GetComponent<MovementScript>();
            var controller = player.GetComponent<CharacterController>();
            var rb = player.GetComponent<Rigidbody>();

            if (movement != null) movement.enabled = false;
            if (controller != null) controller.enabled = false;
            if (rb != null)
            {
                rb.isKinematic = true;
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }

            // Wait a physics step so any gravity/physics settle
            yield return new WaitForFixedUpdate();

            // Teleport
            player.transform.position = position;

            // Clear residual physics state
            if (rb != null)
            {
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }

            // Let one frame pass so other systems react to the new position
            yield return null;

            if (controller != null) controller.enabled = true;
            if (rb != null) rb.isKinematic = false;
            if (movement != null) movement.enabled = true;

            Debug.Log($"Player position set to ({player.transform.position.x:F2}, {player.transform.position.y:F2}, {player.transform.position.z:F2}).");
        }

        
        private Vector3 GetSpawnPosition()
        {
            // Look for spawn points in the scene
            PlayerSpawnPoint[] spawnPoints = FindObjectsOfType<PlayerSpawnPoint>();
                
            if (spawnPoints.Length > 0)
            {
                // Find the default spawn point
                foreach (var spawnPoint in spawnPoints)
                {
                    if (spawnPoint.IsDefaultSpawn)
                    {
                        Debug.Log($"Using default spawn point at {spawnPoint.transform.position}");
                        return spawnPoint.transform.position;
                    }
                }
                    
                // If no default, use the first one found
                Debug.Log($"Using first available spawn point at {spawnPoints[0].transform.position}");
                return spawnPoints[0].transform.position;
            }
                
            Debug.LogWarning("No spawn point found in scene. Using Vector3.zero as fallback.");
            return Vector3.zero;
        }

        public GameObject GetPlayer()
        {
            return player;
        }

        public void SpawnPlayer(Vector3 spawnPosition)
        {
            if (playerPrefab == null)
            {
                Debug.LogError("Player Prefab is not assigned in PlayerManager!");
                return;
            }

            if (player == null)
            {
                player = Instantiate(playerPrefab, spawnPosition, Quaternion.identity);
                Debug.Log("Player successfully spawned.");

                // Validate spawn correctness
                Transform groundCheck = player.transform.Find("GroundCheck");
                if (groundCheck == null)
                {
                    Debug.LogError("Spawned player prefab is missing a GroundCheck object!");
                    return;
                }
            }
            else
            {
                Debug.LogWarning("Player already exists in the scene!");
            }

            SetupMovementScript();
            AssignTargetToCompanions();
        }

        private void SetupMovementScript()
        {
            if (player == null)
            {
                Debug.LogError("Player instance is null. Cannot setup MovementScript.");
                return;
            }

            MovementScript movementScript = player.GetComponent<MovementScript>();
            if (movementScript == null)
            {
                movementScript = player.AddComponent<MovementScript>();
                Debug.Log("MovementScript was missing and has been added to the player.");
            }

            // Assign the GroundCheck, if it exists
            Transform groundCheck = player.transform.Find("GroundCheck");
            if (groundCheck != null)
            {
                movementScript.SetupGroundCheck(groundCheck);
            }
            else
            {
                Debug.LogError(
                    "GroundCheck transform could not be found on the player prefab. Please ensure the prefab is properly configured.");
            }
        }
    
        private void AssignTargetToCompanions()
        {
            if (player == null)
            {
                Debug.LogWarning("No player found when assigning companion targets.");
                return;
            }

            Transform targetPoint = player.transform.Find("TargetPoint");
            if (targetPoint == null)
            {
                Debug.LogWarning("Player prefab does not contain a TargetPoint child!");
                return;
            }

            // Prefer using CompanionManager if available
            if (CompanionManager.Instance != null)
            {
                CompanionManager.Instance.AssignTargetsToAll(targetPoint);
                return;
            }

            // Fallback: assign directly to any CompanionFollow2D in the scene
            foreach (var companion in FindObjectsOfType<CompanionFollow2D>())
            {
                companion.SetTarget(targetPoint);
                Debug.Log($"Assigned TargetPoint to {companion.name}");
            }
        }

    }
}