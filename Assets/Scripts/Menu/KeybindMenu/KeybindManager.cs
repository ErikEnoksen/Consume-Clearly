using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class KeybindManager : MonoBehaviour
{
    public static KeybindManager Instance;

    public GameObject settingsMenu;
    public GameObject keybindsMenu;

    public GameObject keybindsButton;
    public GameObject backButton;

    private GameObject _contextKeybindsMenu;
    private GameObject _contextSettingsMenu;

    // Event triggered when a keybind is changed
    public static event Action<string, KeyCode> OnKeybindChanged;

    private Dictionary<string, KeyCode> keybinds = new Dictionary<string, KeyCode>();

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            DestroyImmediate(gameObject);
            return;
        }

        Instance = this;
        transform.SetParent(null);
        DontDestroyOnLoad(gameObject);

        LoadDefaults();
        LoadKeybinds();
    }

    void LoadDefaults()
    {
        keybinds["Jump"] = KeyCode.Space;
        keybinds["Interact"] = KeyCode.E;
        keybinds["Inventory"] = KeyCode.Q;
        keybinds["Pause"] = KeyCode.Escape;
        keybinds["MoveLeft"] = KeyCode.A;
        keybinds["MoveRight"] = KeyCode.D;
    }

    void LoadKeybinds()
    {
        foreach (var action in new List<string>(keybinds.Keys))
        {
            if (PlayerPrefs.HasKey(action))
            {
                keybinds[action] = (KeyCode)System.Enum.Parse(
                    typeof(KeyCode),
                    PlayerPrefs.GetString(action)
                );
            }
        }
    }

    public KeyCode GetKey(string action)
    {
        if (!keybinds.ContainsKey(action))
        {
            Debug.LogWarning($"No keybind found for {action}");
            return KeyCode.None;
        }

        return keybinds[action];
    }

    public void SetKey(string action, KeyCode newKey)
    {
        // Prevent duplicate bindings
        foreach (var pair in keybinds)
        {
            if (pair.Value == newKey && pair.Key != action)
            {
                Debug.LogWarning($"Key {newKey} already in use!");
                return;
            }
        }

        keybinds[action] = newKey;
        PlayerPrefs.SetString(action, newKey.ToString());
        
        // Notify listeners that the keybind changed
        OnKeybindChanged?.Invoke(action, newKey);
    }

    public string GetKeyAsString(string action)
    {
        return GetKey(action).ToString();
    }

    public Dictionary<string, KeyCode> GetAllKeybinds()
    {
        return new Dictionary<string, KeyCode>(keybinds);
    }

    public void SetBackContext(GameObject contextKeybindsMenu, GameObject contextSettingsMenu)
    {
        _contextKeybindsMenu = contextKeybindsMenu;
        _contextSettingsMenu = contextSettingsMenu;
    }

    public void Back()
    {
        if (SceneManager.GetActiveScene().name == "MainMenu")
        {
            keybindsMenu.SetActive(false);
            backButton.SetActive(true);
            keybindsButton.SetActive(true);
            settingsMenu.SetActive(true);
            return;
        }

        if (_contextKeybindsMenu != null) _contextKeybindsMenu.SetActive(false);
        if (_contextSettingsMenu != null) _contextSettingsMenu.SetActive(true);
    }

    public void Reset()
    {
        LoadDefaults();
        
        foreach (var action in keybinds.Keys)
        {
            PlayerPrefs.DeleteKey(action);
        }
        
        PlayerPrefs.Save();
        
        foreach (var pair in keybinds)
        {
            OnKeybindChanged?.Invoke(pair.Key, pair.Value);
        }
    }
}