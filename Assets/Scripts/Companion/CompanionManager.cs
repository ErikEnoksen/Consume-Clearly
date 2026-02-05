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

        // scene-specific overrides: spawnID -> prefab
        private Dictionary<string, GameObject> scenePrefabOverrides = new Dictionary<string, GameObject>();

        // active spawned companions: spawnID -> instance
        private Dictionary<string, GameObject> spawnedCompanions = new Dictionary<string, GameObject>();

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
                // If not preserving, clear and respawn according to scene
                DespawnAll();
                SpawnMissingFromScene();
            }

            // Try to assign player as target (wait briefly if player not yet created)
            TryAssignPlayerTargets();
        }

        private void RegisterSceneOverrides()
        {
            scenePrefabOverrides.Clear();
            var spawnPoints = FindObjectsOfType<CompanionSpawnPoint>();

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
                {
                    Debug.LogWarning($"CompanionSpawnPoint '{sp.SpawnID}' has no prefab assigned.");
                }

                scenePrefabOverrides[sp.SpawnID] = sp.CompanionPrefab;
            }
        }

        private void ApplySceneOverridesToExisting()
        {
            var keys = spawnedCompanions.Keys.ToList();
            foreach (var spawnID in keys)
            {
                if (!scenePrefabOverrides.ContainsKey(spawnID)) continue;

                var desiredPrefab = scenePrefabOverrides[spawnID];
                var existingInstance = spawnedCompanions[spawnID];

                if (desiredPrefab == null)
                {
                    // If scene says no prefab, remove existing
                    continue;
                }

                var existingPrefab = GetOriginalPrefab(existingInstance);
                if (existingPrefab == null || existingPrefab.name != desiredPrefab.name)
                {
                    // Different prefab: replace
                    Despawn(spawnID);
                    SpawnByID(spawnID);
                }
                else
                {
                    // Same prefab: teleport to spawn point if possible
                    if (teleportExistingOnSceneLoad)
                    {
                        var sp = FindObjectsOfType<CompanionSpawnPoint>().FirstOrDefault(x => x.SpawnID == spawnID);
                        if (sp != null)
                        {
                            existingInstance.transform.position = sp.transform.position;
                        }
                    }
                }
            }
        }

        private void SpawnMissingFromScene()
        {
            var spawnPoints = FindObjectsOfType<CompanionSpawnPoint>();
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

        private GameObject GetOriginalPrefab(GameObject instance)
        {
            #if UNITY_EDITOR
            if (instance == null) return null;
            var prefab = UnityEditor.PrefabUtility.GetCorrespondingObjectFromSource(instance) as GameObject;
            return prefab;
            #else
            return null;
            #endif
        }

        public void SpawnAll()
        {
            var spawnPoints = FindObjectsOfType<CompanionSpawnPoint>();
            foreach (var sp in spawnPoints)
            {
                if (!spawnedCompanions.ContainsKey(sp.SpawnID))
                    SpawnAt(sp);
            }
        }

        public GameObject SpawnByID(string spawnID)
        {
            var sp = FindObjectsOfType<CompanionSpawnPoint>().FirstOrDefault(x => x.SpawnID == spawnID);
            if (sp == null)
            {
                Debug.LogWarning($"No CompanionSpawnPoint found with SpawnID '{spawnID}' in the current scene.");
                return null;
            }

            return SpawnAt(sp);
        }

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

        public void SetPrefabForSpawnID(string spawnID, GameObject prefab)
        {
            scenePrefabOverrides[spawnID] = prefab;

            if (spawnedCompanions.ContainsKey(spawnID))
            {
                Despawn(spawnID);
                SpawnByID(spawnID);
            }
        }

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

        private void TryAssignPlayerTargets()
        {
            // Try to find player; PlayerManager is expected to be persistent like this manager
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

            // If player not ready, schedule a delayed attempt
            StartCoroutine(DelayedAssignRoutine());
        }

        private System.Collections.IEnumerator DelayedAssignRoutine()
        {
            float start = Time.time;
            while (Time.time - start < 2f) // try for up to 2 seconds
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

            // Ensure components
            EnsureComponents(instance);

            spawnedCompanions[sp.SpawnID] = instance;

            // If we already have a player, assign target now
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

            // Try to wire groundCheck if missing
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
                        {
                            groundCheckField.SetValue(follow, found);
                        }
                    }
                }
            }
        }

        // Ensure a CompanionManager exists even if not placed in the scene manually.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void EnsureInstanceExists()
        {
            if (Instance != null) return;
            var existing = FindObjectOfType<CompanionManager>();
            if (existing != null) return;
            var go = new GameObject("CompanionManager");
            go.AddComponent<CompanionManager>();
            DontDestroyOnLoad(go);
            Debug.Log("CompanionManager: auto-created single persistent instance.");
        }
    }
}
