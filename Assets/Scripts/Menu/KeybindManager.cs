using UnityEngine;
using System.Collections.Generic;

public class KeybindManager : MonoBehaviour
{
    public static KeybindManager Instance;

    private Dictionary<string, KeyCode> keybinds = new Dictionary<string, KeyCode>();

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            DestroyImmediate(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        LoadDefaults();
        LoadKeybinds();
    }

    void LoadDefaults()
    {
        keybinds["Jump"] = KeyCode.Space;
        keybinds["Interact"] = KeyCode.E;
        keybinds["Inventory"] = KeyCode.Q;
        keybinds["Pause"] = KeyCode.P;
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
            if (pair.Value == newKey)
            {
                Debug.LogWarning($"Key {newKey} already in use!");
                return;
            }
        }

        keybinds[action] = newKey;
        PlayerPrefs.SetString(action, newKey.ToString());
    }

    public string GetKeyAsString(string action)
    {
        return GetKey(action).ToString();
    }
}