// =============================================================================
// CompanionManager.cs — Persistent Companion Spawn & Lifecycle Manager
// 
//
// PURPOSE:
//   Singleton that manages all companion instances across scene loads.
//   Reads CompanionSpawnPoint markers in each scene, decides whether to spawn
//   new companions or reposition existing ones, and wires the player as their
//   follow target once PlayerManager is ready.
//
// TWO DICTIONARIES:
//   • spawnedCompanions    — spawnID → live GameObject instance (the truth of what exists)
//   • scenePrefabOverrides — spawnID → prefab the current scene wants for that ID
//     (re-built fresh every scene load from the scene's CompanionSpawnPoints)
//
// ON SCENE LOAD (preserveBetweenScenes = true):
//   1. RegisterSceneOverrides()       — reads all spawn points into scenePrefabOverrides
//   2. ApplySceneOverridesToExisting()— for companions already alive, either replace
//                                       them (different prefab) or teleport them to
//                                       their spawn point (same prefab)
//   3. SpawnMissingFromScene()        — instantiates any spawn point that has no
//                                       live companion yet
//   4. TryAssignPlayerTargets()       — hands the player's TargetPoint to all followers;
//                                       retries for up to 2 seconds if the player isn't ready
//
// ON SCENE LOAD (preserveBetweenScenes = false):
//   All existing companions are destroyed and every scene spawn point re-spawns fresh.
//
// AUTO-CREATION:
//   [RuntimeInitializeOnLoadMethod] guarantees the manager exists even if not
//   placed in any scene — it creates itself before the first scene loads.
//
// COMPONENT SAFETY (autoAddMissingComponents):
//   EnsureComponents() checks for Rigidbody2D, AnimationController, and
//   CompanionFollow2D on spawn, adding them at runtime if missing and the
//   flag is enabled. Also wires groundCheck via reflection if it's null.
// =============================================================================

using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Companion
{
    public class CompanionManager : MonoBehaviour
    {
        public static CompanionManager Instance { get; private set; }

        [Header("Settings")]
        [Tooltip("If true, companions will persist across scene loads.")]
        [SerializeField] private bool preserveBetweenScenes = true;
        [Tooltip("If true, missing required components will be added at runtime.")]
        [SerializeField] private bool autoAddMissingComponents = true;
        [Tooltip("If true, existing companions will be teleported to matched spawn points on scene load.")]
        [SerializeField] private bool teleportExistingOnSceneLoad = true;

        // Which prefab the current scene wants for each spawnID — rebuilt every scene load.
        private Dictionary<string, GameObject> scenePrefabOverrides = new Dictionary<string, GameObject>();

        // All currently live companion instances, keyed by spawnID.
        private Dictionary<string, GameObject> spawnedCompanions = new Dictionary<string, GameObject>();

        // --- Singleton Setup ---
        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
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
            if (Instance == this)
            {
                SceneManager.sceneLoaded -= OnSceneLoaded;
                Instance = null;
            }
        }

        // --- Scene Load Handler ---
        // Runs the full spawn/reposition pipeline each time a scene finishes loading.
        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            RegisterSceneOverrides();
            if (preserveBetweenScenes)
            {
                ApplySceneOverridesToExisting();
                SpawnMissingFromScene();
            }
            else
            {
                // Not preserving — wipe and start fresh from the scene's spawn points.
                DespawnAll();
                SpawnMissingFromScene();
            }

            TryAssignPlayerTargets();
        }

        // --- Register Scene Overrides ---
        // Reads every CompanionSpawnPoint in the loaded scene into scenePrefabOverrides.
        // Duplicate IDs and missing prefabs are warned about but don't crash.
        private void RegisterSceneOverrides()
        {
            scenePrefabOverrides.Clear();
            var spawnPoints = FindObjectsByType<CompanionSpawnPoint>(FindObjectsSortMode.None);

            foreach (var sp in spawnPoints)
            {
                if (string.IsNullOrEmpty(sp.SpawnID))
                {
                    Debug.LogWarning($"CompanionSpawnPoint at {sp.transform.position} has empty SpawnID. Skipping.");
                    continue;
                }

                if (scenePrefabOverrides.ContainsKey(sp.SpawnID))
                {
                    Debug.LogWarning($"Duplicate CompanionSpawnPoint SpawnID '{sp.SpawnID}' found. Keeping first and ignoring others.");
                    continue;
                }

                if (sp.CompanionPrefab == null)
                    Debug.LogWarning($"CompanionSpawnPoint '{sp.SpawnID}' has no prefab assigned.");

                scenePrefabOverrides[sp.SpawnID] = sp.CompanionPrefab;
            }
        }

        // --- Apply Overrides to Existing Companions ---
        // For each already-alive companion that also has a scene spawn point:
        //   • Different prefab → despawn and respawn the new one
        //   • Same prefab → optionally teleport to the spawn point position
        private void ApplySceneOverridesToExisting()
        {
            var keys = spawnedCompanions.Keys.ToList();
            foreach (var spawnID in keys)
            {
                if (!scenePrefabOverrides.ContainsKey(spawnID)) continue;

                var desiredPrefab = scenePrefabOverrides[spawnID];
                var existingInstance = spawnedCompanions[spawnID];

                if (desiredPrefab == null) continue;

                var existingPrefab = GetOriginalPrefab(existingInstance);
                if (existingPrefab == null || existingPrefab.name != desiredPrefab.name)
                {
                    // Scene wants a different companion here — replace.
                    Despawn(spawnID);
                    SpawnByID(spawnID);
                }
                else
                {
                    // Same companion — just reposition them at the scene's spawn point.
                    if (teleportExistingOnSceneLoad)
                    {
                        var sp = FindObjectsByType<CompanionSpawnPoint>(FindObjectsSortMode.None).FirstOrDefault(x => x.SpawnID == spawnID);
                        if (sp != null)
                            existingInstance.transform.position = sp.transform.position;
                    }
                }
            }
        }

        // --- Spawn Missing ---
        // Instantiates companions for any spawn point that doesn't have a live instance yet.
        private void SpawnMissingFromScene()
        {
            var spawnPoints = FindObjectsByType<CompanionSpawnPoint>(FindObjectsSortMode.None);
            foreach (var sp in spawnPoints)
            {
                if (string.IsNullOrEmpty(sp.SpawnID)) continue;
                if (spawnedCompanions.ContainsKey(sp.SpawnID)) continue;

                if (sp.CompanionPrefab == null)
                {
                    Debug.LogWarning($"No prefab assigned for spawn '{sp.SpawnID}', skipping.");
                    continue;
                }

                SpawnAt(sp);
            }
        }

        // Editor-only: resolves a live instance back to its source prefab asset for comparison.
        // Returns null in builds (prefab comparison not needed at runtime).
        private GameObject GetOriginalPrefab(GameObject instance)
        {
            #if UNITY_EDITOR
            if (instance == null) return null;
            return UnityEditor.PrefabUtility.GetCorrespondingObjectFromSource(instance) as GameObject;
            #else
            return null;
            #endif
        }

        // --- Public API ---

        // Spawns all companions that have a spawn point in the scene and aren't alive yet.
        public void SpawnAll()
        {
            var spawnPoints = FindObjectsByType<CompanionSpawnPoint>(FindObjectsSortMode.None);
            foreach (var sp in spawnPoints)
            {
                if (!spawnedCompanions.ContainsKey(sp.SpawnID))
                    SpawnAt(sp);
            }
        }

        // Spawns the companion for a specific spawnID using the scene's matching spawn point.
        public GameObject SpawnByID(string spawnID)
        {
            var sp = FindObjectsByType<CompanionSpawnPoint>(FindObjectsSortMode.None).FirstOrDefault(x => x.SpawnID == spawnID);
            if (sp == null)
            {
                Debug.LogWarning($"No CompanionSpawnPoint found with SpawnID '{spawnID}' in the current scene.");
                return null;
            }
            return SpawnAt(sp);
        }

        // Destroys the live instance for this spawnID and removes it from the tracking dict.
        public void Despawn(string spawnID)
        {
            if (!spawnedCompanions.ContainsKey(spawnID)) return;
            var inst = spawnedCompanions[spawnID];
            if (inst != null) Destroy(inst);
            spawnedCompanions.Remove(spawnID);
        }

        public void DespawnAll()
        {
            var keys = spawnedCompanions.Keys.ToList();
            foreach (var k in keys) Despawn(k);
        }

        public void Respawn(string spawnID)
        {
            Despawn(spawnID);
            SpawnByID(spawnID);
        }

        // Overrides which prefab a spawnID uses and immediately replaces the live instance.
        public void SetPrefabForSpawnID(string spawnID, GameObject prefab)
        {
            scenePrefabOverrides[spawnID] = prefab;
            if (spawnedCompanions.ContainsKey(spawnID))
            {
                Despawn(spawnID);
                SpawnByID(spawnID);
            }
        }

        // Assigns the player's TargetPoint as the follow target for all live companions.
        public void AssignTargetsToAll(Transform playerTarget)
        {
            if (playerTarget == null)
            {
                Debug.LogWarning("AssignTargetsToAll called with null playerTarget.");
                return;
            }

            foreach (var kvp in spawnedCompanions)
            {
                var inst = kvp.Value;
                if (inst == null) continue;
                var follow = inst.GetComponent<CompanionFollow2D>();
                if (follow == null)
                {
                    if (autoAddMissingComponents)
                    {
                        follow = inst.AddComponent<CompanionFollow2D>();
                        Debug.Log($"Auto-added CompanionFollow2D to {inst.name}");
                    }
                    else
                    {
                        Debug.LogWarning($"Companion instance '{inst.name}' missing CompanionFollow2D. Skipping target assignment.");
                        continue;
                    }
                }
                follow.SetTarget(playerTarget);
            }
        }

        // --- Player Target Assignment ---
        // Tries to find the player immediately. If PlayerManager isn't ready yet
        // (common right after scene load), falls back to a 2-second polling coroutine.
        private void TryAssignPlayerTargets()
        {
            var playerMgr = Player.PlayerManager.Instance;
            if (playerMgr != null && playerMgr.GetPlayer() != null)
            {
                var target = playerMgr.GetPlayer().transform.Find("TargetPoint");
                if (target != null)
                {
                    AssignTargetsToAll(target);
                    return;
                }
            }

            StartCoroutine(DelayedAssignRoutine());
        }

        private System.Collections.IEnumerator DelayedAssignRoutine()
        {
            float start = Time.time;
            while (Time.time - start < 2f)
            {
                var playerMgr = Player.PlayerManager.Instance;
                if (playerMgr != null && playerMgr.GetPlayer() != null)
                {
                    var target = playerMgr.GetPlayer().transform.Find("TargetPoint");
                    if (target != null)
                    {
                        AssignTargetsToAll(target);
                        yield break;
                    }
                }
                yield return null;
            }

            Debug.LogWarning("CompanionManager: failed to find player TargetPoint within timeout.");
        }

        // --- Spawn At Point ---
        // Core instantiation method. Creates the companion, names it, ensures required
        // components, registers it, and assigns the player target if one is available.
        private GameObject SpawnAt(CompanionSpawnPoint sp)
        {
            if (sp == null) return null;
            if (string.IsNullOrEmpty(sp.SpawnID))
            {
                Debug.LogWarning("Cannot spawn companion with empty SpawnID.");
                return null;
            }
            if (sp.CompanionPrefab == null)
            {
                Debug.LogWarning($"Spawn point '{sp.SpawnID}' has no prefab assigned.");
                return null;
            }

            var instance = Instantiate(sp.CompanionPrefab, sp.transform.position, Quaternion.identity);
            instance.name = sp.SpawnID + "_" + instance.name;

            EnsureComponents(instance);
            spawnedCompanions[sp.SpawnID] = instance;

            // If the player is already in the scene, wire up the follow target immediately.
            var player = Player.PlayerManager.Instance?.GetPlayer();
            if (player != null)
            {
                var target = player.transform.Find("TargetPoint");
                if (target != null)
                {
                    var follow = instance.GetComponent<CompanionFollow2D>();
                    if (follow != null) follow.SetTarget(target);
                }
            }

            return instance;
        }

        // --- Component Safety Check ---
        // Adds missing required components at runtime if autoAddMissingComponents is enabled.
        // Also uses reflection to wire the groundCheck field if it exists as a child object
        // but wasn't assigned in the prefab.
        private void EnsureComponents(GameObject instance)
        {
            if (instance == null) return;

            var rb2d = instance.GetComponent<Rigidbody2D>();
            if (rb2d == null)
            {
                if (autoAddMissingComponents)
                {
                    rb2d = instance.AddComponent<Rigidbody2D>();
                    rb2d.gravityScale = 1f;
                    rb2d.collisionDetectionMode = CollisionDetectionMode2D.Discrete;
                    Debug.Log($"Auto-added Rigidbody2D to companion {instance.name}");
                }
                else
                {
                    Debug.LogWarning($"Companion '{instance.name}' missing Rigidbody2D");
                }
            }

            var animator = instance.GetComponent<Player.AnimationController>();
            if (animator == null)
            {
                if (autoAddMissingComponents)
                {
                    instance.AddComponent<Player.AnimationController>();
                    Debug.Log($"Auto-added AnimationController to companion {instance.name}");
                }
                else
                {
                    Debug.LogWarning($"Companion '{instance.name}' missing AnimationController");
                }
            }

            var follow = instance.GetComponent<CompanionFollow2D>();
            if (follow == null)
            {
                if (autoAddMissingComponents)
                {
                    follow = instance.AddComponent<CompanionFollow2D>();
                    Debug.Log($"Auto-added CompanionFollow2D to companion {instance.name}");
                }
                else
                {
                    Debug.LogWarning($"Companion '{instance.name}' missing CompanionFollow2D");
                }
            }

            // Reflection-based groundCheck wiring — finds a child named "GroundCheck" and
            // assigns it to the private field if it wasn't set in the prefab.
            if (follow != null)
            {
                var groundCheckField = typeof(CompanionFollow2D).GetField("groundCheck", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (groundCheckField != null)
                {
                    var current = groundCheckField.GetValue(follow) as Transform;
                    if (current == null)
                    {
                        var found = instance.transform.Find("GroundCheck");
                        if (found != null)
                            groundCheckField.SetValue(follow, found);
                    }
                }
            }
        }

        // --- Auto-Creation ---
        // Runs before any scene loads. If no CompanionManager exists in the scene,
        // one is created automatically so nothing needs to be manually placed.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void EnsureInstanceExists()
        {
            if (Instance != null) return;
            var existing = FindAnyObjectByType<CompanionManager>();
            if (existing != null) return;
            var go = new GameObject("CompanionManager");
            go.AddComponent<CompanionManager>();
            DontDestroyOnLoad(go);
            Debug.Log("CompanionManager: auto-created single persistent instance.");
        }
    }
}
