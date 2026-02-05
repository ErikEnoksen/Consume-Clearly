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
        
            public float GameTime { get; private set; }
            public string CurrentScene { get; private set; }
        
// csharp
            private void Awake()
            {
                if (Instance == null)
                {
                    Instance = this;
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
        
                // Setup camera background if it's a valid gameplay scene
                if (IsSceneValidForCamera())
                {
                    StartCoroutine(SetupCameraBackground());
                }
            }
        
            private bool IsSceneValidForCamera()
            {
                return !string.IsNullOrEmpty(CurrentScene) && CurrentScene != "MainMenu";
            }
        
            private IEnumerator SetupCameraBackground()
            {
                // Wait for player to be spawned
                yield return new WaitForSeconds(0.1f);
        
                // Find the background sprite in the scene
                SpriteRenderer backgroundSprite = FindSceneBackground();
        
                if (backgroundSprite != null)
                {
                    // Find the camera controller
                    CameraController cameraController = FindObjectOfType<CameraController>();
        
                    if (cameraController != null)
                    {
                        cameraController.SetBackgroundSprite(backgroundSprite);
                        Debug.Log($"Background sprite assigned to camera in scene {CurrentScene}");
                    }
                    else
                    {
                        Debug.LogWarning("CameraController not found in scene");
                    }
                }
                else
                {
                    Debug.LogWarning($"No background sprite found in scene {CurrentScene}");
                }
            }
        
            private SpriteRenderer FindSceneBackground()
            {
                // Try to find by tag first
                GameObject backgroundObject = GameObject.FindGameObjectWithTag("Background");
                if (backgroundObject != null)
                {
                    return backgroundObject.GetComponent<SpriteRenderer>();
                }
        
                // Try to find by name
                backgroundObject = GameObject.Find("Background");
                if (backgroundObject != null)
                {
                    return backgroundObject.GetComponent<SpriteRenderer>();
                }
        
                // As a fallback, find any sprite renderer that looks like a background
                SpriteRenderer[] allSprites = FindObjectsOfType<SpriteRenderer>();
                foreach (var sprite in allSprites)
                {
                    if (sprite.gameObject.name.ToLower().Contains("background") ||
                        sprite.gameObject.layer == LayerMask.NameToLayer("Background"))
                    {
                        return sprite;
                    }
                }
        
                return null;
            }
        
            private void Update()
            {
                GameTime += Time.deltaTime;
        
                if (Input.GetKeyDown(KeyCode.Escape))
                {
                    GoToMainMenu();
                }
        
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
        
                SaveSystem.Save(saveData,customFileName);
                Debug.Log($"Game saved! {saveData.InteractableStates.Count} interactable objects saved.");
            }
        
            public void LoadProgress(string customFileName = null)
            {
                Debug.Log("Loading game progress...");
                data = SaveSystem.Load(customFileName);
                if (data != null)
                {
                    Debug.Log(
                        $"Save data loaded - Time: {data.GameTime}, Scene: {data.CurrentScene}, Position: {data.PlayerPosition}");
                    GameTime = data.GameTime;
                    CurrentScene = data.CurrentScene;
        
                    StartCoroutine(LoadSceneWithPlayerAndObjects(data.CurrentScene, data.PlayerPosition,
                        data.InteractableStates));
                }
                else
                {
                    Debug.LogWarning("No save data found to load");
                }
            }
        
            private IEnumerator LoadSceneWithPlayerAndObjects(string sceneName, Vector3 playerPosition,
                List<InteractableObjectState> interactableStates)
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