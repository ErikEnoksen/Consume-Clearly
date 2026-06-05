// =============================================================================
// PlayerManager.cs — Persistent Player Spawner & Lifecycle Manager
// 
//
// PURPOSE:
//   Singleton (DontDestroyOnLoad) that owns the player GameObject across all
//   scenes. Handles spawning on scene load, fall-off respawning, position
//   teleportation, and wiring companion follow targets after spawn.
//
// SPAWN FLOW:
//   OnSceneLoaded → GetSpawnPosition() (finds PlayerSpawnPoint marked IsDefault)
//   → SpawnPlayer() → SetupMovementScript() → AssignTargetToCompanions()
//
// DELAY SPAWN MODE (delaySpawnUntilSignal):
//   When true, the player is NOT spawned automatically on scene load.
//   Use SpawnPlayerFromTimeline() to trigger spawning from a Timeline signal
//   (e.g. intro cutscene). This wires the Cinemachine camera follow after spawn.
//
// FALL RESPAWN:
//   Update() monitors Y position. If the player falls below fallThreshold (-20),
//   WaitAndSetPosition() teleports them back to lastRespawnPosition.
//   SetRespawnPoint() should be called by checkpoints to update this.
//
// POSITION TELEPORT (WaitAndSetPosition):
//   Disables movement and physics, waits a FixedUpdate step for any residual
//   forces to settle, then moves the player and re-enables everything.
//   This prevents physics from snapping the player back after the teleport.
//
// COMPANION WIRING:
//   After each spawn, AssignTargetToCompanions() hands the player's "TargetPoint"
//   child transform to CompanionManager (or directly to CompanionFollow2D as fallback).
// =============================================================================

using Companion;
using System.Collections;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Player
{
    public class PlayerManager : MonoBehaviour
    {
        public static PlayerManager Instance { get; private set; }

        // --- Inspector Fields ---
        [SerializeField] private GameObject playerPrefab;
        // When true, skips auto-spawn on scene load — use SpawnPlayerFromTimeline() instead.
        [SerializeField] private bool delaySpawnUntilSignal;
        // Y position below which the player is considered to have fallen off the world.
        [SerializeField] private float fallThreshold = -20f;

        // --- Runtime State ---
        private GameObject player;
        private Vector3 lastRespawnPosition;
        private bool isRespawning;

        // --- Singleton Setup ---
        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                transform.SetParent(null);
                DontDestroyOnLoad(gameObject);
                SceneManager.sceneLoaded += OnSceneLoaded;
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void OnDestroy()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        // --- Fall Detection ---
        // Checks every frame whether the player has fallen below the world floor.
        // isRespawning prevents the coroutine stacking if Update fires multiple frames before the teleport completes.
        private void Update()
        {
            if (player == null || isRespawning) return;
            if (player.transform.position.y < fallThreshold)
                StartCoroutine(Respawn());
        }

        private IEnumerator Respawn()
        {
            isRespawning = true;
            yield return StartCoroutine(WaitAndSetPosition(lastRespawnPosition));
            isRespawning = false;
        }

        // Call this from checkpoint triggers to update where the player respawns on fall.
        public void SetRespawnPoint(Vector3 position)
        {
            lastRespawnPosition = position;
        }

        // --- Scene Load Handler ---
        // Clears the old player reference if we've moved to a new scene, then spawns
        // a fresh one — unless delaySpawnUntilSignal is set or we're on the MainMenu.
        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (player != null && player.scene != scene)
            {
                Destroy(player);
                player = null;
                Debug.Log("Player reference cleared due to scene change.");
            }

            if (scene.name != "MainMenu" && player == null && !delaySpawnUntilSignal)
            {
                Debug.Log($"Spawning player in scene {scene.name}.");
                SpawnPlayer(GetSpawnPosition());
            }
            else if (player != null)
            {
                // Player already exists (e.g. DontDestroyOnLoad carried it over) — just re-wire movement.
                SetupMovementScript();
            }
        }

        // Public entry point — GameManager calls this to restore position after a load.
        public void SetPlayerPosition(Vector3 position)
        {
            StartCoroutine(WaitAndSetPosition(position));
        }

        // --- Teleport Coroutine ---
        // Disables movement and physics before moving so forces don't fight the teleport,
        // then re-enables everything cleanly one frame after the position is set.
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

            // Let physics settle before moving.
            yield return new WaitForFixedUpdate();

            player.transform.position = position;

            // Clear any residual velocity after the move.
            if (rb != null)
            {
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }

            // One extra frame so other systems (camera, companions) react to the new position.
            yield return null;

            if (controller != null) controller.enabled = true;
            if (rb != null) rb.isKinematic = false;
            if (movement != null) movement.enabled = true;

            Debug.Log($"Player position set to ({player.transform.position.x:F2}, {player.transform.position.y:F2}, {player.transform.position.z:F2}).");
        }

        // --- Spawn Position ---
        // Looks for a PlayerSpawnPoint marked IsDefaultSpawn. Falls back to the
        // first available spawn point, then Vector3.zero if none exist.
        private Vector3 GetSpawnPosition()
        {
            PlayerSpawnPoint[] spawnPoints = FindObjectsByType<PlayerSpawnPoint>(FindObjectsSortMode.None);

            if (spawnPoints.Length > 0)
            {
                foreach (var spawnPoint in spawnPoints)
                {
                    if (spawnPoint.IsDefaultSpawn)
                    {
                        Debug.Log($"Using default spawn point at {spawnPoint.transform.position}");
                        return spawnPoint.transform.position;
                    }
                }

                Debug.Log($"Using first available spawn point at {spawnPoints[0].transform.position}");
                return spawnPoints[0].transform.position;
            }

            Debug.LogWarning("No spawn point found in scene. Using Vector3.zero as fallback.");
            return Vector3.zero;
        }

        public GameObject GetPlayer() => player;

        // --- Spawn Player ---
        // Instantiates the player prefab, validates the GroundCheck child exists,
        // wires MovementScript, and assigns the TargetPoint to companions.
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
                lastRespawnPosition = spawnPosition;
                Debug.Log("Player successfully spawned.");

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

        // --- Movement Setup ---
        // Ensures the player has a MovementScript and that its GroundCheck is wired.
        // Adds MovementScript at runtime if the prefab is missing it (safety net).
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

            Transform groundCheck = player.transform.Find("GroundCheck");
            if (groundCheck != null)
                movementScript.SetupGroundCheck(groundCheck);
            else
                Debug.LogError("GroundCheck transform could not be found on the player prefab. Please ensure the prefab is properly configured.");
        }

        // --- Companion Target Assignment ---
        // Passes the player's TargetPoint to CompanionManager (preferred) or directly
        // to any CompanionFollow2D in the scene as a fallback.
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

            if (CompanionManager.Instance != null)
            {
                CompanionManager.Instance.AssignTargetsToAll(targetPoint);
                return;
            }

            // Fallback — no CompanionManager in scene.
            foreach (var companion in FindObjectsByType<CompanionFollow2D>(FindObjectsSortMode.None))
            {
                companion.SetTarget(targetPoint);
                Debug.Log($"Assigned TargetPoint to {companion.name}");
            }
        }

        // --- Timeline Spawn ---
        // Called by a Timeline signal during intro cutscenes.
        // Also wires the Cinemachine camera to follow the newly spawned player.
        public void SpawnPlayerFromTimeline()
        {
            if (player != null) return;

            SpawnPlayer(GetSpawnPosition());

            CinemachineCamera vcam = FindAnyObjectByType<CinemachineCamera>();
            if (vcam != null)
            {
                vcam.Follow = player.transform;
                vcam.LookAt = player.transform;
            }

            Debug.Log("Player spawned from Timeline signal.");
        }
    }
}