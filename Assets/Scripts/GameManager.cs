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
        
            SaveData data = new SaveData();
            private List<InventorySlotData> _cachedInventory = new List<InventorySlotData>();
            private int _cachedMoney = 0;
            private int _cachedCommunityLevel = 0;
            private int _cachedCommunityOverflow = 0;
            private float _cachedHungerValue = 100f;
            private int _cachedSatisfactionValue = 50;
            private bool _cachedSatisfactionPassive = false;
            private readonly Dictionary<string, Vector3> _sessionPositions = new Dictionary<string, Vector3>();

            public float GameTime { get; private set; }
            public string CurrentScene { get; private set; }
            
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
        
            private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
            {
                CurrentScene = scene.name;
            }
        
            private void Update()
            {
                GameTime += Time.deltaTime;
        
                // Quick save/load shortcuts (optional - remove if not needed)
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
        
            public void SaveProgress(string customFileName = null)
            {
                GameObject player = PlayerManager.Instance?.GetPlayer();
                SaveData saveData = new SaveData
                {
                    GameTime = GameTime,
                    CurrentScene = SceneManager.GetActiveScene().name,
                    PlayerPosition = player ? player.transform.position : data.PlayerPosition
                };
        
                // Collect all saveable objects in scene
                MonoBehaviour[] allMonoBehaviours = FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None);
        
                foreach (var mono in allMonoBehaviours)
                {
                    if (mono is ISaveable saveable)
                    {
                        InteractableObjectState state = saveable.SaveState();
                        if (state != null)
                        {
                            saveData.InteractableStates.Add(state);
                        }
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

                SaveSystem.Save(saveData,customFileName);
                Debug.Log($"Game saved! {saveData.InteractableStates.Count} interactable objects saved.");
            }
        
            public void LoadProgress(string customFileName = null)
            {
                Time.timeScale = 1f;
                PauseMenu.isPaused = false;
                Debug.Log("Loading game progress...");
                data = SaveSystem.Load(customFileName);
                if (data != null)
                {
                    Debug.Log(
                        $"Save data loaded - Time: {data.GameTime}, Scene: {data.CurrentScene}, Position: {data.PlayerPosition}");
                    GameTime = data.GameTime;
                    CurrentScene = data.CurrentScene;
                    _cachedInventory = data.InventorySlots ?? new List<InventorySlotData>();
                    _cachedMoney = data.Money;
                    _cachedCommunityLevel = data.CommunityLevel;
                    _cachedCommunityOverflow = data.CommunityOverflowPoints;
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
        
            private IEnumerator LoadSceneWithPlayerAndObjects(string sceneName, Vector3 playerPosition,
                List<InteractableObjectState> interactableStates,
                List<Save.QuestSaveState> activeQuestStates = null,
                List<string> completedQuestIDs = null)
            {
                SceneManager.LoadScene(sceneName);
        
                // Wait for scene to load
                while (!SceneManager.GetSceneByName(sceneName).isLoaded)
                {
                    yield return null;
                }
        
                // Wait for PlayerManager to initialize player
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
        
                // Set player position
                Debug.Log("Setting player position after reload...");
                PlayerManager.Instance.SetPlayerPosition(playerPosition);
        
                // Wait one more frame to ensure all objects are initialized
                yield return null;
        
                // Load interactable object states
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

            private void LoadInteractableStates(List<InteractableObjectState> states)
            {
                if (states == null || states.Count == 0)
                {
                    Debug.Log("No interactable states to load.");
                    return;
                }
        
                // Find all saveable objects in the scene
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
                        {
                            saveableObjects.Add(id, saveable);
                        }
                        else
                        {
                            Debug.LogWarning($"Duplicate saveable ID found: {id} on object {((MonoBehaviour)saveable).gameObject.name}. Skipping duplicate.");
                        }
                    }
                }
        
                int loadedCount = 0;
                foreach (var savedState in states)
                {
                    if (savedState == null)
                        continue;
        
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
        
            public void GoToMainMenu()
            {
                SaveProgress();
        
                if (PlayerManager.Instance != null)
                {
                    Destroy(PlayerManager.Instance.gameObject);
                }
        
                LoadMainMenu();
            }
        
            public void LoadScene(string sceneName)
            {
                SceneManager.LoadScene(sceneName);
            }

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

                // Remember where the player was in the current scene before leaving
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

                // Cache inventory and money so items picked up since the last save survive the scene swap
                InventoryManager leavingInventory = FindAnyObjectByType<InventoryManager>();
                if (leavingInventory != null)
                    _cachedInventory = leavingInventory.SaveInventory();

                MoneyManager leavingMoney = FindAnyObjectByType<MoneyManager>();
                if (leavingMoney != null)
                    _cachedMoney = leavingMoney.GetMoney();

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

                elapsed = 0f;
                while (PlayerManager.Instance == null || PlayerManager.Instance.GetPlayer() == null)
                {
                    yield return null;
                    elapsed += Time.deltaTime;
                    if (elapsed >= timeout) yield break;
                }

                // Restore position before rendering the next frame to avoid spawn point pop
                if (_sessionPositions.TryGetValue(sceneName, out Vector3 returnPos))
                    PlayerManager.Instance.SetPlayerPosition(returnPos);

                yield return null;

                // Restore scene-specific interactable states from this scene's save file if one exists
                SaveData sceneData = SaveSystem.Load(sceneName);
                if (sceneData != null)
                    LoadInteractableStates(sceneData.InteractableStates);

                // Always restore inventory and money from cache so they follow the player
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
        
            public void QuitGame()
            {
                SaveProgress(); // Auto-save before quitting
        
            #if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
            #else
                Application.Quit();
            #endif
            }
        }