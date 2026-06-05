// =============================================================================
// GameManager.cs — Persistent Game State & Scene Coordinator
// 
//
// PURPOSE:
//   The central singleton that persists across all scenes (DontDestroyOnLoad).
//   It owns two distinct responsibilities:
//     1. Save / Load  — collects state from all scene systems, writes to disk,
//                       and restores everything after a scene is loaded.
//     2. Scene Transitions — safely swaps scenes while carrying player data
//                            (inventory, money, stats) between them.
//
// SINGLETON BEHAVIOUR:
//   Normal DontDestroyOnLoad pattern, with one exception: if the MainMenu scene
//   loads a new GameManager, the old persistent one is destroyed and replaced.
//   This lets the main menu always start with a clean GameManager.
//
// TWO KINDS OF SCENE CHANGE:
//   • LoadProgress()      — full save/load restore. Loads the scene stored in the
//                           save file and rebuilds all state from disk.
//   • TransitionToScene() — lightweight mid-session swap. Caches the leaving
//                           scene's live data (inventory, money, stats, player pos)
//                           in memory and restores it in the new scene without
//                           reading from disk. Also loads per-scene interactable
//                           states from that scene's own save file if one exists.
//
// SESSION POSITION CACHE (_sessionPositions):
//   When the player leaves a scene, their position is stored by scene name.
//   If they return to that scene in the same session, they spawn where they left,
//   not at the scene's default spawn point.
//
// QUICK SAVE / LOAD:
//   F5 = quick save, F9 = quick load (development shortcut, can be removed).
// =============================================================================

        using System.Collections;
        using System.Collections.Generic;
        using System.Linq;
        using Player;
        using Save;
        using LevelObjects.Interactable;
        using UnityEngine;
        using UnityEngine.SceneManagement;

        public class GameManager : MonoBehaviour
        {
            public static GameManager Instance { get; private set; }

            // --- Persistent State ---
            // Last loaded save data — kept so values can be re-applied after scene loads.
            SaveData data = new SaveData();

            // Mid-session caches — carry live data across scene transitions without a disk read.
            private List<InventorySlotData> _cachedInventory = new List<InventorySlotData>();
            private int _cachedMoney = 0;
            private int _cachedCommunityLevel = 0;
            private int _cachedCommunityOverflow = 0;
            private float _cachedHungerValue = 100f;
            private int _cachedSatisfactionValue = 50;
            private bool _cachedSatisfactionPassive = false;

            // Remembers where the player was in each scene this session so they return to the right spot.
            private readonly Dictionary<string, Vector3> _sessionPositions = new Dictionary<string, Vector3>();

            public float GameTime { get; private set; }
            public string CurrentScene { get; private set; }
            
            // --- Singleton Setup ---
            // Allows the MainMenu to replace the persistent GameManager so it always
            // starts fresh. All other scenes destroy duplicates as usual.
            private void Awake()
            {
                if (Instance == null)
                {
                    Instance = this;
                    transform.SetParent(null);
                    DontDestroyOnLoad(gameObject);
                    Initialize();
                }
                else if (Instance != this)
                {
                    // If the scene that just spawned this GameManager is the MainMenu,
                    // prefer the scene's GameManager: destroy the old persistent one and take over.
                    string activeScene = SceneManager.GetActiveScene().name;
                    if (activeScene == "MainMenu")
                    {
                        Destroy(Instance.gameObject);
                        Instance = this;
                        transform.SetParent(null);
                        DontDestroyOnLoad(gameObject);
                        Initialize();
                    }
                    else
                    {
                        Destroy(gameObject);
                    }
                }
            }

            // Resets runtime counters and subscribes to the scene-loaded event.
            private void Initialize()
            {
                GameTime = 0f;
                CurrentScene = SceneManager.GetActiveScene().name;
                SceneManager.sceneLoaded += OnSceneLoaded;
            }

            private void OnDestroy()
            {
                SceneManager.sceneLoaded -= OnSceneLoaded;
            }

            // Keeps CurrentScene up to date whenever Unity finishes loading a scene.
            private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
            {
                CurrentScene = scene.name;
            }

            // --- Update ---
            // Ticks game time and handles F5/F9 quick save/load shortcuts.
            private void Update()
            {
                GameTime += Time.deltaTime;

                if (Input.GetKeyDown(KeyCode.F5))
                {
                    SaveProgress();
                    Debug.Log("Quick Save!");
                }

                if (Input.GetKeyDown(KeyCode.F9))
                {
                    LoadProgress();
                    Debug.Log("Quick Load!");
                }
            }

            // --- Save Progress ---
            // Gathers state from every system in the current scene and writes it to disk.
            // customFileName lets InteractableScene save to a per-scene slot (e.g. "Shop.json").
            public void SaveProgress(string customFileName = null)
            {
                GameObject player = PlayerManager.Instance?.GetPlayer();
                SaveData saveData = new SaveData
                {
                    GameTime = GameTime,
                    CurrentScene = SceneManager.GetActiveScene().name,
                    PlayerPosition = player ? player.transform.position : data.PlayerPosition
                };

                // Poll every ISaveable in the scene and collect their snapshots.
                MonoBehaviour[] allMonoBehaviours = FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None);
                foreach (var mono in allMonoBehaviours)
                {
                    if (mono is ISaveable saveable)
                    {
                        InteractableObjectState state = saveable.SaveState();
                        if (state != null)
                            saveData.InteractableStates.Add(state);
                    }
                }

                InventoryManager inventoryManager = FindAnyObjectByType<InventoryManager>();
                if (inventoryManager != null)
                {
                    _cachedInventory = inventoryManager.SaveInventory();
                    saveData.InventorySlots = _cachedInventory;
                }

                MoneyManager moneyManager = FindAnyObjectByType<MoneyManager>();
                if (moneyManager != null)
                {
                    _cachedMoney = moneyManager.GetMoney();
                    saveData.Money = _cachedMoney;
                }

                if (DayCycleManager.Instance != null)
                {
                    saveData.CurrentDay = DayCycleManager.Instance.CurrentDay;
                    saveData.DayTimer = DayCycleManager.Instance.DayTimer;
                }

                CommunityMeter communityMeter = FindAnyObjectByType<CommunityMeter>();
                if (communityMeter != null)
                {
                    _cachedCommunityLevel = communityMeter.CommunityLevel;
                    _cachedCommunityOverflow = communityMeter.OverflowPoints;
                }
                saveData.CommunityLevel = _cachedCommunityLevel;
                saveData.CommunityOverflowPoints = _cachedCommunityOverflow;

                PlayerHunger playerHunger = FindAnyObjectByType<PlayerHunger>();
                if (playerHunger != null)
                    _cachedHungerValue = playerHunger.GetHungerValue();
                saveData.HungerValue = _cachedHungerValue;

                CircularSatisfactionMeter satisfactionMeter = FindAnyObjectByType<CircularSatisfactionMeter>();
                if (satisfactionMeter != null)
                {
                    _cachedSatisfactionValue = satisfactionMeter.currentSatisfactionValue;
                    _cachedSatisfactionPassive = satisfactionMeter.IsPassivePointActive;
                }
                saveData.SatisfactionValue = _cachedSatisfactionValue;
                saveData.IsPassivePointActive = _cachedSatisfactionPassive;

                if (Assets.Scripts.Quests.QuestController.Instance != null)
                {
                    saveData.ActiveQuestStates = Assets.Scripts.Quests.QuestController.Instance.GetSaveData();
                    saveData.CompletedQuestIDs = Assets.Scripts.Quests.QuestController.Instance.GetCompletedQuestIDs();
                }

                SaveSystem.Save(saveData, customFileName);
                Debug.Log($"Game saved! {saveData.InteractableStates.Count} interactable objects saved.");
            }

            // --- Load Progress ---
            // Reads a save file and kicks off a full scene + state restore.
            // Also resets pause state in case the game was paused when load was called.
            public void LoadProgress(string customFileName = null)
            {
                Time.timeScale = 1f;
                PauseMenu.isPaused = false;
                Debug.Log("Loading game progress...");
                data = SaveSystem.Load(customFileName);
                if (data != null)
                {
                    Debug.Log($"Save data loaded - Time: {data.GameTime}, Scene: {data.CurrentScene}, Position: {data.PlayerPosition}");
                    GameTime = data.GameTime;
                    CurrentScene = data.CurrentScene;
                    _cachedInventory = data.InventorySlots ?? new List<InventorySlotData>();
                    _cachedMoney = data.Money;
                    _cachedCommunityLevel = data.CommunityLevel;
                    _cachedCommunityOverflow = data.CommunityOverflowPoints;

                    // Guard against zero/default values in old or empty saves.
                    _cachedHungerValue = data.HungerValue > 0 ? data.HungerValue : 100f;
                    _cachedSatisfactionValue = data.SatisfactionValue > 0 ? data.SatisfactionValue : 50;
                    _cachedSatisfactionPassive = data.IsPassivePointActive;

                    if (DayCycleManager.Instance != null)
                        DayCycleManager.Instance.LoadState(data.CurrentDay > 0 ? data.CurrentDay : 1, data.DayTimer);

                    StartCoroutine(LoadSceneWithPlayerAndObjects(data.CurrentScene, data.PlayerPosition,
                        data.InteractableStates, data.ActiveQuestStates, data.CompletedQuestIDs));
                }
                else
                {
                    Debug.LogWarning("No save data found to load");
                }
            }

            // --- Full Load Coroutine ---
            // Loads the saved scene, waits for the player to initialize, places them at the
            // saved position, then restores all systems in order: interactables → quests →
            // inventory → money → community → hunger → satisfaction.
            private IEnumerator LoadSceneWithPlayerAndObjects(string sceneName, Vector3 playerPosition,
                List<InteractableObjectState> interactableStates,
                List<Save.QuestSaveState> activeQuestStates = null,
                List<string> completedQuestIDs = null)
            {
                SceneManager.LoadScene(sceneName);

                while (!SceneManager.GetSceneByName(sceneName).isLoaded)
                    yield return null;

                // Wait for PlayerManager to be ready — it spawns the player asynchronously.
                float timeout = 5f;
                float elapsed = 0f;
                while (PlayerManager.Instance == null || PlayerManager.Instance.GetPlayer() == null)
                {
                    yield return null;
                    elapsed += Time.deltaTime;
                    if (elapsed >= timeout)
                    {
                        Debug.LogError("Timeout waiting for PlayerManager to initialize player.");
                        yield break;
                    }
                }

                Debug.Log("Setting player position after reload...");
                PlayerManager.Instance.SetPlayerPosition(playerPosition);

                // One extra frame so all Awake/Start calls on scene objects finish before we load states.
                yield return null;

                LoadInteractableStates(interactableStates);

                if (activeQuestStates != null && Assets.Scripts.Quests.QuestController.Instance != null)
                    Assets.Scripts.Quests.QuestController.Instance.LoadSaveData(activeQuestStates, completedQuestIDs);

                InventoryManager inventoryManager = FindAnyObjectByType<InventoryManager>();
                if (inventoryManager != null)
                {
                    List<InventorySlotData> toRestore = _cachedInventory?.Count > 0
                        ? _cachedInventory
                        : data.InventorySlots;
                    if (toRestore != null)
                        inventoryManager.LoadInventory(toRestore);
                }

                MoneyManager moneyManager = FindAnyObjectByType<MoneyManager>();
                if (moneyManager != null)
                    moneyManager.SetMoney(_cachedMoney);

                CommunityMeter communityMeter = FindAnyObjectByType<CommunityMeter>();
                if (communityMeter != null)
                    communityMeter.LoadCommunityState(_cachedCommunityLevel, _cachedCommunityOverflow);

                PlayerHunger playerHunger = FindAnyObjectByType<PlayerHunger>();
                if (playerHunger != null)
                    playerHunger.SetHungerValue(_cachedHungerValue);

                CircularSatisfactionMeter satisfactionMeter = FindAnyObjectByType<CircularSatisfactionMeter>();
                if (satisfactionMeter != null)
                    satisfactionMeter.LoadSatisfactionState(_cachedSatisfactionValue, _cachedSatisfactionPassive);
            }

            // --- Load Interactable States ---
            // Builds a lookup of all ISaveables in the scene by ID, then matches saved
            // states to them and calls LoadState(). Warns on missing or duplicate IDs.
            private void LoadInteractableStates(List<InteractableObjectState> states)
            {
                if (states == null || states.Count == 0)
                {
                    Debug.Log("No interactable states to load.");
                    return;
                }

                // Build an ID → ISaveable map so we can look up objects in O(1).
                Dictionary<string, ISaveable> saveableObjects = new Dictionary<string, ISaveable>();
                MonoBehaviour[] allMonoBehaviours = FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None);
                foreach (var mono in allMonoBehaviours)
                {
                    if (mono is ISaveable saveable)
                    {
                        string id = saveable.GetUniqueId();
                        if (string.IsNullOrEmpty(id))
                        {
                            Debug.LogWarning($"Found ISaveable with null or empty ID on object {((MonoBehaviour)saveable).gameObject.name}. Skipping.");
                            continue;
                        }

                        if (!saveableObjects.ContainsKey(id))
                            saveableObjects.Add(id, saveable);
                        else
                            Debug.LogWarning($"Duplicate saveable ID found: {id} on object {((MonoBehaviour)saveable).gameObject.name}. Skipping duplicate.");
                    }
                }

                int loadedCount = 0;
                foreach (var savedState in states)
                {
                    if (savedState == null) continue;

                    string savedId = savedState.uniqueId;
                    if (string.IsNullOrEmpty(savedId))
                    {
                        Debug.LogWarning("Saved state with null or empty uniqueId encountered. Skipping.");
                        continue;
                    }

                    if (saveableObjects.TryGetValue(savedId, out var saveable))
                    {
                        saveable.LoadState(savedState);
                        loadedCount++;
                    }
                    else
                    {
                        Debug.LogWarning($"Could not find object with ID: {savedId}");
                    }
                }

                Debug.Log($"Loaded {loadedCount} of {states.Count} interactable object states.");
            }

            // --- Menu / Utility ---
            // Saves progress, destroys the player (it's DontDestroyOnLoad), then loads the main menu.
            public void GoToMainMenu()
            {
                SaveProgress();

                if (PlayerManager.Instance != null)
                    Destroy(PlayerManager.Instance.gameObject);

                LoadMainMenu();
            }

            // Simple synchronous scene load — use for non-gameplay scenes (menus, credits).
            public void LoadScene(string sceneName)
            {
                SceneManager.LoadScene(sceneName);
            }

            // --- Mid-Session Scene Transition ---
            // Caches all live game state from the leaving scene, async-loads the destination,
            // waits for the player, restores position, then pushes cached data into the new scene.
            public bool IsTransitioning { get; private set; }

            public void TransitionToScene(string sceneName)
            {
                if (IsTransitioning) return;
                IsTransitioning = true;
                StartCoroutine(TransitionCoroutine(sceneName));
            }

            private IEnumerator TransitionCoroutine(string sceneName)
            {
                if (string.IsNullOrEmpty(sceneName) || !Application.CanStreamedLevelBeLoaded(sceneName))
                {
                    Debug.LogError($"Cannot transition to scene '{sceneName}': not found in build settings.");
                    IsTransitioning = false;
                    yield break;
                }

                // --- Cache leaving scene state ---
                // Player position stored so returning to this scene feels seamless.
                string leavingScene = SceneManager.GetActiveScene().name;
                GameObject leavingPlayer = PlayerManager.Instance?.GetPlayer();
                if (leavingPlayer != null)
                    _sessionPositions[leavingScene] = leavingPlayer.transform.position;

                CommunityMeter leavingCommunityMeter = FindAnyObjectByType<CommunityMeter>();
                if (leavingCommunityMeter != null)
                {
                    _cachedCommunityLevel = leavingCommunityMeter.CommunityLevel;
                    _cachedCommunityOverflow = leavingCommunityMeter.OverflowPoints;
                }

                PlayerHunger leavingHunger = FindAnyObjectByType<PlayerHunger>();
                if (leavingHunger != null)
                    _cachedHungerValue = leavingHunger.GetHungerValue();

                CircularSatisfactionMeter leavingSatisfaction = FindAnyObjectByType<CircularSatisfactionMeter>();
                if (leavingSatisfaction != null)
                {
                    _cachedSatisfactionValue = leavingSatisfaction.currentSatisfactionValue;
                    _cachedSatisfactionPassive = leavingSatisfaction.IsPassivePointActive;
                }

                // Items picked up since the last disk save must also survive the scene swap.
                InventoryManager leavingInventory = FindAnyObjectByType<InventoryManager>();
                if (leavingInventory != null)
                    _cachedInventory = leavingInventory.SaveInventory();

                MoneyManager leavingMoney = FindAnyObjectByType<MoneyManager>();
                if (leavingMoney != null)
                    _cachedMoney = leavingMoney.GetMoney();

                // --- Load destination scene ---
                AsyncOperation load = SceneManager.LoadSceneAsync(sceneName);
                if (load == null)
                {
                    Debug.LogError($"LoadSceneAsync returned null for scene '{sceneName}'.");
                    yield break;
                }

                float timeout = 5f, elapsed = 0f;
                while (!load.isDone)
                {
                    yield return null;
                    elapsed += Time.deltaTime;
                    if (elapsed >= timeout)
                    {
                        Debug.LogError($"Timed out loading scene '{sceneName}'.");
                        yield break;
                    }
                }

                // Wait for the player to be available in the new scene.
                elapsed = 0f;
                while (PlayerManager.Instance == null || PlayerManager.Instance.GetPlayer() == null)
                {
                    yield return null;
                    elapsed += Time.deltaTime;
                    if (elapsed >= timeout) yield break;
                }

                // Restore position first — before the camera renders — to avoid a visible pop.
                if (_sessionPositions.TryGetValue(sceneName, out Vector3 returnPos))
                    PlayerManager.Instance.SetPlayerPosition(returnPos);

                yield return null;

                // --- Restore destination scene state ---
                // Load per-scene interactable states from that scene's own save file if it exists.
                SaveData sceneData = SaveSystem.Load(sceneName);
                if (sceneData != null)
                    LoadInteractableStates(sceneData.InteractableStates);

                // Inventory and money always follow the player — read from cache, not disk.
                InventoryManager inventoryManager = FindAnyObjectByType<InventoryManager>();
                if (inventoryManager != null && _cachedInventory != null && _cachedInventory.Count > 0)
                    inventoryManager.LoadInventory(_cachedInventory);

                MoneyManager moneyManager = FindAnyObjectByType<MoneyManager>();
                if (moneyManager != null)
                    moneyManager.SetMoney(_cachedMoney);

                CommunityMeter communityMeter = FindAnyObjectByType<CommunityMeter>();
                if (communityMeter != null)
                    communityMeter.LoadCommunityState(_cachedCommunityLevel, _cachedCommunityOverflow);

                PlayerHunger playerHunger = FindAnyObjectByType<PlayerHunger>();
                if (playerHunger != null)
                    playerHunger.SetHungerValue(_cachedHungerValue);

                CircularSatisfactionMeter satisfactionMeter = FindAnyObjectByType<CircularSatisfactionMeter>();
                if (satisfactionMeter != null)
                    satisfactionMeter.LoadSatisfactionState(_cachedSatisfactionValue, _cachedSatisfactionPassive);

                IsTransitioning = false;
            }

            public void LoadMainMenu()
            {
                SceneManager.LoadScene("MainMenu");
            }

            // Auto-saves then quits. In the editor, stops play mode instead of closing the app.
            public void QuitGame()
            {
                SaveProgress();

            #if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
            #else
                Application.Quit();
            #endif
            }
        }