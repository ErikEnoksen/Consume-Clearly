using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class KeybindDisplay : MonoBehaviour
{
    [SerializeField] private string actionName;
    [SerializeField] private bool showLabel = true;

    private TMP_Text textComponent;
    private string labelText;

    private void Start()
    {
        textComponent = GetComponent<TMP_Text>();

        if (textComponent == null)
        {
            Debug.LogError("KeybindDisplay requires a TMP_Text component", gameObject);
            return;
        }

        labelText = textComponent.text;
        UpdateDisplay();

        // Subscribe to keybind changes
        KeybindManager.OnKeybindChanged += OnKeybindChanged;
    }

    private void OnDestroy()
    {
        // Unsubscribe when destroyed
        KeybindManager.OnKeybindChanged -= OnKeybindChanged;
    }

    private void UpdateDisplay()
    {
        if (KeybindManager.Instance == null)
            return;

        string keyAsString = KeybindManager.Instance.GetKeyAsString(actionName);

        if (showLabel)
        {
            textComponent.text = $"{labelText}: {keyAsString}";
        }
        else
        {
            textComponent.text = keyAsString;
        }
    }

    private void OnKeybindChanged(string changedAction, KeyCode newKey)
    {
        if (changedAction == actionName)
        {
            UpdateDisplay();
        }
    }
}